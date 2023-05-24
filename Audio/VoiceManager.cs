using System;
using System.Collections.Generic;
using IPA.Utilities;
using MultiplayerChat.Config;
using MultiplayerChat.Core;
using MultiplayerChat.Network;
using MultiplayerCore.Networking;
using SiraUtil.Logging;
using UnityEngine;
using UnityOpus;
using Zenject;
using Object = UnityEngine.Object;

namespace MultiplayerChat.Audio;

// ReSharper disable once ClassNeverInstantiated.Global
public class VoiceManager : MonoBehaviour, IInitializable, IDisposable
{
    [Inject] private readonly SiraLog _logger = null!;
    [Inject] private readonly PluginConfig _pluginConfig = null!;
    [Inject] private readonly ChatManager _chatManager = null!;
    [Inject] private readonly MicrophoneManager _microphoneManager = null!;
    [Inject] private readonly IMultiplayerSessionManager _multiplayerSession = null!;
    [Inject] private readonly MpPacketSerializer _packetSerializer = null!;

    private Encoder? _opusEncoder;
    private readonly Decoder _opusDecoder;

    private int _captureFrequency;
    private SamplingFrequency _encodeFrequency;

    public int EncodeFrameLength { get; private set; } = GetFrameLength(SamplingFrequency.Frequency_48000);
    
    private float[] _resampleBuffer;
    private readonly float[] _encodeSampleBuffer;
    private readonly byte[] _encodeOutputBuffer;
    private int _encodeSampleIndex;

    private readonly float[] _decodeSampleBuffer;

    public const NumChannels OpusChannels = NumChannels.Mono;
    public static readonly SamplingFrequency DecodeFrequency = SamplingFrequency.Frequency_48000;
    public const int OpusComplexity = 10;
    public const int Bitrate = 96000;
    public const int MsPerFrame = 20;
    
    /// <summary>
    /// Max frame length, in samples, supported by Opus.
    /// This is 20ms @ 48 kHz.
    /// </summary>
    public const int MaxFrameLength = 960;

    public bool IsTransmitting { get; private set; }

    public bool IsLoopbackTesting { get; private set; }
    private AudioSource? _loopbackTester;

    private readonly PlayerVoicePlayer _loopbackVoicePlayer;
    private readonly Dictionary<string, PlayerVoicePlayer> _voicePlayers;

    public event Action? StartedTransmittingEvent;
    public event Action? StoppedTransmittingEvent;

    public VoiceManager()
    {
        _opusEncoder = null;
        _opusDecoder = new(DecodeFrequency, OpusChannels);

        _captureFrequency = 0;
        _encodeFrequency = SamplingFrequency.Frequency_48000;

        _resampleBuffer = new float[MaxFrameLength]; // resized automatically (EnsureResampleBufferSize)
        _encodeSampleBuffer = new float[MaxFrameLength]; 
        _encodeOutputBuffer = new byte[MaxFrameLength * sizeof(float)]; 
        _encodeSampleIndex = 0;

        _decodeSampleBuffer = new float[Decoder.maximumPacketDuration * (int) OpusChannels];

        IsLoopbackTesting = false;

        _loopbackVoicePlayer = new PlayerVoicePlayer("loopback", jitterBufferMs: 250, spatialBlend: 0);
        _voicePlayers = new();
    }

    public void Initialize()
    {
        _multiplayerSession.disconnectedEvent += HandleSessionDisconnected;

        _microphoneManager.FragmentReadyEvent += HandleMicrophoneFragment;
        _microphoneManager.CaptureEndEvent += HandleMicrophoneEnd;

        _packetSerializer.RegisterCallback<MpcVoicePacket>(HandleVoicePacket);
    }

    public void Update()
    {
        _loopbackVoicePlayer.Update();
        
        foreach (var vp in _voicePlayers.Values)
            vp.Update();
    }

    public void Dispose()
    {
        _multiplayerSession.disconnectedEvent -= HandleSessionDisconnected;

        if (_microphoneManager.IsCapturing)
            _microphoneManager.StopCapture();

        _microphoneManager.FragmentReadyEvent -= HandleMicrophoneFragment;
        _microphoneManager.CaptureEndEvent -= HandleMicrophoneEnd;

        _opusEncoder?.Dispose();
        _opusDecoder.Dispose();

        IsLoopbackTesting = false;
        if (_loopbackTester != null)
            Object.Destroy(_loopbackTester);
    }

    private void HandleSessionDisconnected(DisconnectedReason reason)
    {
        StopVoiceTransmission();
        StopLoopbackTest();
    }

    #region Encode / Send
    
    public static int GetFrameLength(int bitrate) => bitrate / (1000 / MsPerFrame);
    public static int GetFrameLength(SamplingFrequency frequency) => GetFrameLength((int)frequency);

    private static SamplingFrequency GetEncodeFrequency(int inputFrequency)
    {
        return inputFrequency switch
        {
            > 24000 => SamplingFrequency.Frequency_48000,
            > 16000 => SamplingFrequency.Frequency_24000,
            > 12000 => SamplingFrequency.Frequency_16000,
            > 8000 => SamplingFrequency.Frequency_12000,
            _ => SamplingFrequency.Frequency_8000
        };
    }
    
    private void EnsureResampleBufferSize(int minimumSize)
    {
        if (_resampleBuffer.Length < minimumSize)
        {
            _resampleBuffer = new float[minimumSize];
        }
    }

    private void HandleMicrophoneFragment(float[] samples, int captureFrequency)
    {
        // Apply gain
        AudioGain.Apply(samples, _pluginConfig.MicrophoneGain);
        
        // (Re)initialize encoder as needed
        if (_opusEncoder == null || _captureFrequency != captureFrequency)
        {
            _captureFrequency = captureFrequency;
            _encodeFrequency = GetEncodeFrequency(captureFrequency);

            EncodeFrameLength = GetFrameLength(_encodeFrequency);
            
            _opusEncoder?.Dispose();
            _opusEncoder = new Encoder(_encodeFrequency, OpusChannels, OpusApplication.VoIP)
            {
                Bitrate = Bitrate,
                Complexity = OpusComplexity,
                Signal = OpusSignal.Voice
            };

            _logger.Info($"Initialized Opus encoder (captureFrequency={captureFrequency}, " +
                         $"encodeFrequency={_encodeFrequency}, " +
                         $"encodeFrameLength={EncodeFrameLength})");
        }
        
        // If encode frequency is not exact, resample audio
        var encodeFrequencyInt = (int)_encodeFrequency;
        
        float[] copySourceBuffer;
        int copySourceLength;

        if (captureFrequency == encodeFrequencyInt)
        {
            copySourceBuffer = samples;
            copySourceLength = samples.Length;
        }
        else
        {
            EnsureResampleBufferSize(AudioResample.ResampledSampleCount(samples.Length, captureFrequency, encodeFrequencyInt));

            copySourceBuffer = _resampleBuffer;
            copySourceLength = AudioResample.Resample(samples, _resampleBuffer, captureFrequency, encodeFrequencyInt);
        }

        // Continuously write to encode buffer until it reaches the target frame length, then encode
        for (var i = 0; i < copySourceLength; i++)
        {
            _encodeSampleBuffer[_encodeSampleIndex++] = copySourceBuffer[i];

            if (_encodeSampleIndex != EncodeFrameLength)
                continue;

            HandleEncodedFrame
            (
                _opusEncoder.Encode(_encodeSampleBuffer, EncodeFrameLength, _encodeOutputBuffer)
            );
            _encodeSampleIndex = 0;
        }
    }

    private void HandleMicrophoneEnd()
    {
        _loopbackVoicePlayer.StopImmediate();

        Array.Clear(_encodeSampleBuffer, 0, _encodeSampleBuffer.Length);
        Array.Clear(_encodeOutputBuffer, 0, _encodeOutputBuffer.Length);
    }

    private void HandleEncodedFrame(int encodedLength)
    {
        if (encodedLength <= 0)
            return;

        var voicePacket = MpcVoicePacket.Obtain();

        try
        {
            // Broadcast unreliable voice frame
            voicePacket.AllocatePooledBuffer(encodedLength);
            Buffer.BlockCopy(_encodeOutputBuffer, 0, voicePacket.Data!, 0, encodedLength);

            if (IsLoopbackTesting)
            {
                // Loopback test only - do not actually send, instead loop as if it was just received
                HandleVoicePacket(voicePacket, null);
                return;
            }

            // Normal mode: network send if transmitting
            if (!_multiplayerSession.isConnected || !_multiplayerSession.isSyncTimeInitialized || !IsTransmitting)
                return;

            _multiplayerSession.SendUnreliable(voicePacket);
        }
        finally
        {
            voicePacket.Release();
        }
    }

    #endregion

    #region Decode / Receive

    private void HandleVoicePacket(MpcVoicePacket packet, IConnectedPlayer? source)
    {
        if (!_pluginConfig.EnableVoiceChat)
            return;

        try
        {
            var dataLength = packet.DataLength;
            if (dataLength > 0)
                HandleVoiceFragment(_opusDecoder.Decode(packet.Data, dataLength, _decodeSampleBuffer), source);
            else
                HandleVoiceFragment(0, source);
        }
        finally
        {
            packet.Release();
        }
    }

    private void HandleVoiceFragment(int decodedLength, IConnectedPlayer? source)
    {
        if (source == null)
        {
            // This should only happen in loopback situations
            _loopbackVoicePlayer.FeedFragment(_decodeSampleBuffer, decodedLength);
            return;
        }

        if (_chatManager.GetIsPlayerMuted(source.userId))
        {
            // Player is muted, ignore
            _chatManager.SetPlayerIsSpeaking(source.userId, false);
            return;
        }

        var voicePlayer = EnsurePlayerVoicePlayer(source.userId);
        voicePlayer.FeedFragment(_decodeSampleBuffer, decodedLength);
    }

    #endregion

    #region Talk API

    public bool CanTransmit => _pluginConfig.EnableVoiceChat &&
                               _multiplayerSession is {isConnected: true, isSyncTimeInitialized: true};

    public bool StartVoiceTransmission()
    {
        if (!CanTransmit)
            return false;
        
        if (IsTransmitting)
            return true;

        IsTransmitting = true;
        _microphoneManager.StartCapture();

        _chatManager.SetLocalPlayerIsSpeaking(true);
        StartedTransmittingEvent?.Invoke();
        return true;
    }

    public bool StopVoiceTransmission()
    {
        if (!IsTransmitting)
            return true;

        _microphoneManager.StopCapture();
        IsTransmitting = false;

        if (_multiplayerSession.isConnected)
        {
            // Empty packet to signal end of transmission 
            var endPacket = MpcVoicePacket.Obtain();

            try
            {
                endPacket.Data = null;
                _multiplayerSession.SendUnreliable(endPacket);
            }
            finally
            {
                endPacket.Release();
            }
        }

        _chatManager.SetLocalPlayerIsSpeaking(false);
        StoppedTransmittingEvent?.Invoke();
        return true;
    }

    public void HandlePlayerMuted(string userId)
    {
        if (_voicePlayers.TryGetValue(userId, out var voicePlayer))
            voicePlayer.StopImmediate();
    }

    #endregion

    #region Loopback test

    private AudioSource SetupLoopback()
    {
        if (_loopbackTester == null)
        {
            var obj = new GameObject("VoiceLoopbackTester");
            _loopbackTester = obj.AddComponent<AudioSource>();
        }

        _loopbackVoicePlayer.ConfigureAudioSource(_loopbackTester);

        _loopbackTester.gameObject.SetActive(true);
        return _loopbackTester;
    }

    public void StartLoopbackTest()
    {
        StopLoopbackTest();

        SetupLoopback();

        IsLoopbackTesting = true;
        _microphoneManager.StartCapture();
    }

    public void StopLoopbackTest()
    {
        _microphoneManager.StopCapture();

        _loopbackVoicePlayer.StopImmediate();

        if (!IsLoopbackTesting)
            return;

        IsLoopbackTesting = false;
    }

    #endregion

    #region VoicePlayers & Avatars

    private PlayerVoicePlayer EnsurePlayerVoicePlayer(string playerUserId)
    {
        if (!_voicePlayers.TryGetValue(playerUserId, out var voicePlayer))
        {
            voicePlayer = new PlayerVoicePlayer(
                playerUserId,
                _pluginConfig.JitterBufferMs, 
                _pluginConfig.SpatialBlend
            );
            
            voicePlayer.StartPlaybackEvent += HandleVoicePlaybackStart;
            voicePlayer.StopPlaybackEvent += HandleVoicePlaybackStop;
            
            _voicePlayers.Add(playerUserId, voicePlayer);
        }

        return voicePlayer;
    }

    private void HandleVoicePlaybackStart(object sender, EventArgs e)
    {
        _chatManager.SetPlayerIsSpeaking(((PlayerVoicePlayer)sender).PlayerUserId, true);
    }

    private void HandleVoicePlaybackStop(object sender, EventArgs e)
    {
        _chatManager.SetPlayerIsSpeaking(((PlayerVoicePlayer)sender).PlayerUserId, false);
    }

    public void ProvideAvatarAudio(MultiplayerAvatarAudioController avatarAudio)
    {
        var player = avatarAudio.GetField<IConnectedPlayer, MultiplayerAvatarAudioController>("_connectedPlayer");
        
        var voicePlayer = EnsurePlayerVoicePlayer(player.userId);
        voicePlayer.SetMultiplayerAvatarAudioController(avatarAudio);
    }

    #endregion
}