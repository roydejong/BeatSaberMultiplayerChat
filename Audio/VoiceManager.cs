using System;
using System.Collections.Generic;
using IPA.Utilities;
using MultiplayerChat.Audio.VoIP;
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
public class VoiceManager : IInitializable, IDisposable
{
    [Inject] private readonly SiraLog _log = null!;
    [Inject] private readonly PluginConfig _pluginConfig = null!;
    [Inject] private readonly ChatManager _chatManager = null!;
    [Inject] private readonly MicrophoneManager _microphoneManager = null!;
    [Inject] private readonly IMultiplayerSessionManager _multiplayerSession = null!;
    [Inject] private readonly MpPacketSerializer _packetSerializer = null!;
    [Inject] private readonly OpusDecodeThread _opusDecodeThread = null!;

    private readonly Encoder _opusEncoder;

    private readonly float[] _encodeSampleBuffer;
    private readonly byte[] _encodeOutputBuffer;
    private int _encodeSampleIndex;

    public bool IsTransmitting { get; private set; }

    public bool IsLoopbackTesting { get; private set; }
    private AudioSource? _loopbackTester;

    private PlayerVoicePlayer _loopbackVoicePlayer = null!;
    private Dictionary<string, PlayerVoicePlayer> _voicePlayers;

    public VoiceManager()
    {
        _opusEncoder = new(OpusConstants.Frequency, OpusConstants.Channels, OpusApplication.VoIP)
        {
            Bitrate = OpusConstants.Bitrate,
            Complexity = OpusConstants.Complexity,
            Signal = OpusConstants.Signal
        };

        _encodeSampleBuffer = new float[OpusConstants.FrameSampleLength];
        _encodeOutputBuffer = new byte[OpusConstants.FrameByteLength];
        _encodeSampleIndex = 0;

        IsLoopbackTesting = false;

        _voicePlayers = new();
    }

    public void Initialize()
    {
        _loopbackVoicePlayer = new PlayerVoicePlayer(_opusDecodeThread, 0);
        
        _multiplayerSession.disconnectedEvent += HandleSessionDisconnected;
        
        _microphoneManager.OnFragmentReady += HandleMicrophoneFragment;
        _microphoneManager.OnCaptureEnd += HandleMicrophoneEnd;

        _packetSerializer.RegisterCallback<MpcVoicePacket>(HandleVoicePacket);
    }

    public void Dispose()
    {
        _multiplayerSession.disconnectedEvent -= HandleSessionDisconnected;
        
        if (_microphoneManager.IsCapturing)
            _microphoneManager.StopCapture();
        
        _microphoneManager.OnFragmentReady -= HandleMicrophoneFragment;
        _microphoneManager.OnCaptureEnd -= HandleMicrophoneEnd;

        _opusEncoder.Dispose();
        
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

    private void HandleMicrophoneFragment(float[] samples)
    {
        foreach (var sample in samples)
        {
            _encodeSampleBuffer[_encodeSampleIndex++] = sample;

            if (_encodeSampleIndex != OpusConstants.FrameSampleLength)
                // Collect samples until we have a full frame
                continue;

            try
            {
                HandleEncodedFrame
                (
                    _opusEncoder.Encode(_encodeSampleBuffer, OpusConstants.FrameSampleLength, _encodeOutputBuffer)
                );
            }
            finally
            {
                _encodeSampleIndex = 0;   
            }
        }
    }
    
    private void HandleMicrophoneEnd()
    {
        _loopbackVoicePlayer.HandleTransmissionEnd();
        
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

        var dataLength = packet.DataLength;
        if (dataLength > OpusConstants.FrameByteLength)
        {
            _log.Warn($"Dropping oversized voice frame (dataLength={dataLength}, FrameByteSize={OpusConstants.FrameByteLength})");
            return;
        }

        if (source == null)
        {
            // Loopback test
            _loopbackVoicePlayer.HandleVoicePacket(packet);
            return;
        }
        
        if (_chatManager.GetIsPlayerMuted(source.userId))
        {
            // Player is muted
            _chatManager.SetPlayerIsSpeaking(source, false);
            return;
        }

        // Get or initialize player entry
        if (!_voicePlayers.TryGetValue(source.userId, out var voicePlayer))
        {
            if (packet.IsEndOfTransmission)
                return;
        
            voicePlayer = new PlayerVoicePlayer(_opusDecodeThread, _pluginConfig.SpatialBlend);
            _voicePlayers.Add(source.userId, voicePlayer);
        }
        
        // Push it
        voicePlayer.HandleVoicePacket(packet);
    }

    #endregion

    #region Talk API

    public bool StartVoiceTransmission()
    {
        if (!_pluginConfig.EnableVoiceChat)
            return false;
        
        if (IsTransmitting)
            return true;

        if (!_multiplayerSession.isConnected || !_multiplayerSession.isSyncTimeInitialized)
            return false;
        
        IsTransmitting = true;
        _microphoneManager.StartCapture();

        _chatManager.SetLocalPlayerIsSpeaking(true);
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
        return true;
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
        
        _loopbackVoicePlayer.HandleTransmissionEnd();

        if (!IsLoopbackTesting)
            return;
        
        IsLoopbackTesting = false;
    }

    #endregion

    #region Avatar integration

    public void ProvideAvatarAudio(MultiplayerAvatarAudioController avatarAudio)
    {
        var player = avatarAudio.GetField<IConnectedPlayer, MultiplayerAvatarAudioController>("_connectedPlayer");
        
        if (!_voicePlayers.TryGetValue(player.userId, out var voicePlayer))
        {
            voicePlayer = new PlayerVoicePlayer(_opusDecodeThread, _pluginConfig.SpatialBlend);
            _voicePlayers.Add(player.userId, voicePlayer);
        }
        
        voicePlayer.SetMultiplayerAvatarAudioController(avatarAudio);
    }

    #endregion
}