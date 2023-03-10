using System;
using IPA.Utilities;
using UnityEngine;

namespace MultiplayerChat.Audio;

public class PlayerVoicePlayer : IDisposable
{
    private const int JitterBufferMs = 120;

    private readonly float _spatialBlend;
    private readonly JitterBufferClip _bufferedClip;

    private AudioSource? _audioSource;

    public bool IsPlaying { get; private set; }

    public PlayerVoicePlayer(float spatialBlend)
    {
        _spatialBlend = spatialBlend;
        _bufferedClip = new JitterBufferClip(JitterBufferMs);

        IsPlaying = false;
    }

    public void Dispose()
    {
        _bufferedClip.Dispose();
        IsPlaying = false;
    }

    public void SetMultiplayerAvatarAudioController(MultiplayerAvatarAudioController avatarAudio) =>
        ConfigureAudioSource(avatarAudio.GetField<AudioSource, MultiplayerAvatarAudioController>("_audioSource"));

    public void ConfigureAudioSource(AudioSource audioSource)
    {
        _audioSource = audioSource;
        _audioSource.clip = _bufferedClip.AudioClip;
        _audioSource.timeSamples = 0;
        _audioSource.volume = 0f;

        if (_spatialBlend <= 0)
        {
            _audioSource.spatialize = false;
            _audioSource.spatialBlend = 0;
        }
        else
        {
            _audioSource.spatialize = true;
            _audioSource.spatialBlend = _spatialBlend;
        }
    }

    #region Audio Stream

    public void Update()
    {
        _bufferedClip.Update();
            
        if (_audioSource == null)
            return;

        if (!IsPlaying && _bufferedClip.IsActive)
        {
            _audioSource.timeSamples = 0;
            _audioSource.loop = true;
            _audioSource.volume = 1f;
            _audioSource.Play();
            IsPlaying = true;
        }
        else if (IsPlaying && !_bufferedClip.IsActive)
        {
            StopImmediate();
            IsPlaying = false;
        }
    }
    
    public void HandleDecodedFragment(float[] decodeBuffer, int decodedLength) =>
        _bufferedClip.FeedFragment(decodeBuffer, decodedLength);

    public void StopImmediate()
    {
        if (_audioSource != null)
        {
            _audioSource.loop = false;
            _audioSource.Stop();
            _audioSource.timeSamples = 0;
            _audioSource.volume = 0f;
        }

        _bufferedClip.Reset();
        IsPlaying = false;
    }
    
    #endregion
}