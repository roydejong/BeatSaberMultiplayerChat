using System;
using IPA.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MultiplayerChat.Audio;

public class PlayerVoicePlayer : IDisposable
{
    private const int PlaybackClipLength = 1024 * 6;

    private readonly AudioClip _audioClip;
    private float[]? _localBuffer;
    private int _bufferPos;
    
    private AudioSource? _audioSource;
    
    public bool IsReceiving { get; private set; }
    public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

    public PlayerVoicePlayer()
    {
        _audioClip = AudioClip.Create("VoicePlayback", PlaybackClipLength, (int)VoiceManager.OpusChannels,
            (int)VoiceManager.OpusFrequency, false);
        _localBuffer = null;
        _bufferPos = 0;
    }

    public void Dispose()
    {
        Object.Destroy(_audioClip);
        _localBuffer = null;
    }

    public void SetMultiplayerAvatarAudioController(MultiplayerAvatarAudioController avatarAudio)
    {
        _audioSource = avatarAudio.GetField<AudioSource, MultiplayerAvatarAudioController>("_audioSource");
        _audioSource.clip = _audioClip;
        _audioSource.loop = true;
    }

    public void SetCustomAudioSource(AudioSource audioSource)
    {
        _audioSource = audioSource;
        _audioSource.clip = _audioClip;
        _audioSource.loop = true;
    }

    public void HandleDecodedFragment(float[] decodeBuffer, int decodedLength)
    {
        if (_audioSource == null)
        {
            HandleTransmissionEnd();
            return;
        }
        
        if (_localBuffer == null || _localBuffer.Length != decodedLength)
            _localBuffer = new float[decodedLength];

        IsReceiving = true;    

        Array.Copy(decodeBuffer, _localBuffer, decodedLength);

        _audioClip.SetData(_localBuffer, _bufferPos);
        _bufferPos += decodedLength;

        if (_audioSource != null && !_audioSource.isPlaying)
        {
            if (_bufferPos > (PlaybackClipLength / 2))
            {
                _audioSource.time = 0f;
                _audioSource.Play();
            }
        }

        _bufferPos %= PlaybackClipLength;
    }

    public void HandleTransmissionEnd()
    {
        if (_audioSource != null)
            _audioSource.Stop();

        _bufferPos = 0;

        IsReceiving = false;
    }
}