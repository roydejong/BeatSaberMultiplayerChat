using System;
using IPA.Utilities;
using MultiplayerChat.Audio.VoIP;
using MultiplayerChat.Network;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MultiplayerChat.Audio;

public class PlayerVoicePlayer : IDisposable
{
    private const int PlaybackClipLength = 1024 * 6;

    private readonly JitterBuffer _jitterBuffer;
    private readonly float _spatialBland;
    private readonly AudioClip _audioClip;

    private AudioSource? _audioSource;

    public bool IsReceiving { get; private set; }
    public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

    public PlayerVoicePlayer(OpusDecodeThread decodeThread, float spatialBland)
    {
        _jitterBuffer = new JitterBuffer(decodeThread);
        _spatialBland = spatialBland;
        _audioClip = AudioClip.Create("VoicePlayback", PlaybackClipLength, (int) OpusConstants.Channels,
            (int) OpusConstants.Frequency, true, pcmreadercallback: HandlePcmRead);
    }

    public void Dispose()
    {
        Object.Destroy(_audioClip);
        _jitterBuffer.Dispose();
    }

    public void SetMultiplayerAvatarAudioController(MultiplayerAvatarAudioController avatarAudio) =>
        ConfigureAudioSource(avatarAudio.GetField<AudioSource, MultiplayerAvatarAudioController>("_audioSource"));

    public void ConfigureAudioSource(AudioSource audioSource)
    {
        _audioSource = audioSource;
        _audioSource.clip = _audioClip;
        _audioSource.timeSamples = 0;
        _audioSource.volume = 0f;

        if (_spatialBland <= 0)
        {
            _audioSource.spatialize = false;
            _audioSource.spatialBlend = 0;
        }
        else
        {
            _audioSource.spatialize = true;
            _audioSource.spatialBlend = _spatialBland;
        }
    }

    public void HandleVoicePacket(MpcVoicePacket voicePacket)
    {
        _jitterBuffer.Feed(voicePacket);

        // Reset & start playback if not yet playing
        if (voicePacket.IsEndOfTransmission || _audioSource is null || _audioSource.isPlaying)
            return;
        
        _audioSource.timeSamples = 0;
        _audioSource.loop = true;
        _audioSource.volume = 0f;
        _audioSource.Play();
    }

    public void HandleTransmissionEnd()
    {
        if (_audioSource != null)
        {
            _audioSource.loop = false;
            _audioSource.Stop();
            _audioSource.timeSamples = 0;
            _audioSource.volume = 0f;
        }

        IsReceiving = false;
    }

    private void HandlePcmRead(float[] data)
    {
        bool isEndOfTransmission;
        bool isSilence;
        
        _jitterBuffer.ReadNext(data, out isEndOfTransmission, out isSilence, out var logText);

        Console.WriteLine($"HandlePcmRead [data={data.Length}, logText={logText}]");
        
        if (_audioSource != null)
        {
            if (isSilence)
            {
                // Reset volume if frame was fully silent (buffer delay or buffer empty)
                _audioSource.volume = 0f;
            }
            else if (_audioSource.volume < 1f)
            {
                // Ramp up volume (helps prevents bad noise at start of a transmission)
                if (_audioSource.volume >= .99f || Mathf.Approximately(_audioSource.volume, 1f))
                    _audioSource.volume = 1f;
                else
                    _audioSource.volume = Mathf.Lerp(_audioSource.volume, 1f, .035f);
            }
        }

        if (isEndOfTransmission)
            HandleTransmissionEnd();
    }
}