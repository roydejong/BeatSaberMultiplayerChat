using System;
using System.Collections.Generic;
using IPA.Utilities;
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
public class VoiceManager : IInitializable, IDisposable
{
    [Inject] private readonly SiraLog _log = null!;
    [Inject] private readonly PluginConfig _pluginConfig = null!;
    [Inject] private readonly ChatManager _chatManager = null!;
    [Inject] private readonly MicrophoneManager _microphoneManager = null!;
    [Inject] private readonly IMultiplayerSessionManager _multiplayerSession = null!;
    [Inject] private readonly MpPacketSerializer _packetSerializer = null!;

    private readonly Encoder _opusEncoder;
    private readonly Decoder _opusDecoder;

    private readonly float[] _encodeSampleBuffer;
    private readonly byte[] _encodeOutputBuffer;
    private int _encodeSampleIndex;

    private readonly float[] _decodeSampleBuffer;

    public const NumChannels OpusChannels = NumChannels.Mono;
    public static readonly SamplingFrequency OpusFrequency = SamplingFrequency.Frequency_48000;
    public const int OpusComplexity = 10;

    public const int Bitrate = 96000;
    public const int FrameLength = 120;
    public const int FrameByteSize = FrameLength * sizeof(float);
    
    public bool IsTransmitting { get; private set; }

    public bool IsLoopbackTesting { get; private set; }
    private AudioSource? _loopbackTester;

    private PlayerVoicePlayer _loopbackVoicePlayer;
    private Dictionary<string, PlayerVoicePlayer> _voicePlayers;

    public VoiceManager()
    {
        _opusEncoder = new(OpusFrequency, OpusChannels, OpusApplication.VoIP)
        {
            Bitrate = Bitrate,
            Complexity = OpusComplexity,
            Signal = OpusSignal.Voice
        };
        _opusDecoder = new(OpusFrequency, OpusChannels);

        _encodeSampleBuffer = new float[FrameLength];
        _encodeOutputBuffer = new byte[FrameByteSize];
        _encodeSampleIndex = 0;

        _decodeSampleBuffer = new float[Decoder.maximumPacketDuration * (int)OpusChannels];

        IsLoopbackTesting = false;

        _loopbackVoicePlayer = new PlayerVoicePlayer();
        _voicePlayers = new();
    }

    public void Initialize()
    {
        _microphoneManager.OnFragmentReady += HandleMicrophoneFragment;

        _packetSerializer.RegisterCallback<MpcVoicePacket>(HandleVoicePacket);
    }

    public void Dispose()
    {
        if (_microphoneManager.IsCapturing)
            _microphoneManager.StopCapture();
        
        _microphoneManager.OnFragmentReady -= HandleMicrophoneFragment;

        _opusEncoder.Dispose();
        _opusDecoder.Dispose();
        
        IsLoopbackTesting = false;
        if (_loopbackTester != null)
            Object.Destroy(_loopbackTester);
    }

    #region Encode / Send

    private void HandleMicrophoneFragment(float[] samples)
    {
        foreach (var sample in samples)
        {
            _encodeSampleBuffer[_encodeSampleIndex++] = sample;

            if (_encodeSampleIndex != FrameLength)
                continue;
            
            HandleEncodedFrame
            (
                _opusEncoder.Encode(_encodeSampleBuffer, FrameLength, _encodeOutputBuffer)
            );
            _encodeSampleIndex = 0;
        }
    }

    private void HandleEncodedFrame(int encodedLength)
    {
        if (encodedLength <= 0)
            return;
        
        // Broadcast unreliable voice frame
        var sendBuffer = new byte[encodedLength];
        Array.Copy(_encodeOutputBuffer, sendBuffer, encodedLength);
        
        var voicePacket = new MpcVoicePacket()
        {
            Data = sendBuffer
        };

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

    #endregion

    #region Decode / Receive

    private void HandleVoicePacket(MpcVoicePacket packet, IConnectedPlayer? source)
    {
        var dataLength = packet.Data?.Length ?? 0;
        if (dataLength > 0)
            HandleVoiceFragment(_opusDecoder.Decode(packet.Data, dataLength, _decodeSampleBuffer), source);
        else
            HandleVoiceFragment(0, source);
    }

    private void HandleVoiceFragment(int decodedLength, IConnectedPlayer? source)
    {
        if (source == null)
        {
            // This should only happen in loopback situations
            _loopbackVoicePlayer.HandleDecodedFragment(_decodeSampleBuffer, decodedLength);
            return;
        }

        if (_chatManager.GetIsPlayerMuted(source.userId))
            // Player is muted
            return;

        var isEndOfTransmission = decodedLength <= 0;

        if (!_voicePlayers.TryGetValue(source.userId, out var voicePlayer))
        {
            if (isEndOfTransmission)
                return;
        
            voicePlayer = new PlayerVoicePlayer();
            _voicePlayers.Add(source.userId, voicePlayer);
        }

        if (!isEndOfTransmission)
        {
            voicePlayer.HandleDecodedFragment(_decodeSampleBuffer, decodedLength);
            _chatManager.SetPlayerIsSpeaking(source, true);
        }
        else
        {
            voicePlayer.HandleTransmissionEnd();
            _chatManager.SetPlayerIsSpeaking(source, false);
        }
    }

    #endregion

    #region Talk API

    public void StartVoiceTransmission()
    {
        if (IsTransmitting)
            return;
        
        IsTransmitting = true;
        _microphoneManager.StartCapture();

        _log.Info("Voice: start transmit");
    }

    public void StopVoiceTransmission()
    {
        if (!IsTransmitting)
            return;
        
        _microphoneManager.StopCapture();
        IsTransmitting = false;

        if (_multiplayerSession.isConnected && _multiplayerSession.isSyncTimeInitialized)
        {
            // Empty packet to signal end of transmission 
            _multiplayerSession.SendUnreliable(new MpcVoicePacket()
            {
                Data = null
            });
        }
        
        _log.Info("Voice: stop transmit");
    }

    public void HandlePlayerMuted(string userId)
    {
        if (_voicePlayers.TryGetValue(userId, out var voicePlayer))
            voicePlayer.HandleTransmissionEnd();
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

        _loopbackVoicePlayer.SetCustomAudioSource(_loopbackTester);
        
        _loopbackTester.gameObject.SetActive(true);
        return _loopbackTester;
    }

    public void StartLoopbackTest()
    {
        StopLoopbackTest();
        
        SetupLoopback();

        IsLoopbackTesting = true;
        _microphoneManager.StartCapture();
        
        _log.Info("Started loopback test");
    }

    public void StopLoopbackTest()
    {
        _microphoneManager.StopCapture();
        
        _loopbackVoicePlayer.HandleTransmissionEnd();

        if (!IsLoopbackTesting)
            return;
        
        IsLoopbackTesting = false;
        _log.Info("Stopped loopback test");
    }

    #endregion

    #region Avatar integration

    public void ProvideAvatarAudio(MultiplayerAvatarAudioController avatarAudio)
    {
        var player = avatarAudio.GetField<IConnectedPlayer, MultiplayerAvatarAudioController>("_connectedPlayer");
        
        if (!_voicePlayers.TryGetValue(player.userId, out var voicePlayer))
        {
            voicePlayer = new PlayerVoicePlayer();
            _voicePlayers.Add(player.userId, voicePlayer);
        }
        
        voicePlayer.SetMultiplayerAvatarAudioController(avatarAudio);
    }

    #endregion
}