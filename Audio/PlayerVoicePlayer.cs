using System;
using IPA.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MultiplayerChat.Audio;

public class PlayerVoicePlayer : IDisposable
{
    private const int PlaybackClipLength = 1024 * 6;

    private readonly float _spatialBland;
    private readonly AudioClip _audioClip;
    private float[]? _localBuffer;
    private int _bufferPos;

    private AudioSource? _audioSource;

    public bool IsReceiving { get; private set; }
    public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

    public PlayerVoicePlayer(float spatialBland)
    {
        _spatialBland = spatialBland;
        _audioClip = AudioClip.Create("VoicePlayback", PlaybackClipLength, (int) VoiceManager.OpusChannels,
            (int) VoiceManager.OpusFrequency, false);
        _localBuffer = null;
        _bufferPos = 0;
    }

    public void Dispose()
    {
        Object.Destroy(_audioClip);
        _localBuffer = null;
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

    public void HandleDecodedFragment(float[] decodeBuffer, int decodedLength)
    {
        if (_audioSource == null || decodedLength <= 0)
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

        if (_audioSource != null)
        {
            if (_audioSource.isPlaying)
            {
                if (_audioSource.volume < 1f)
                {
                    if (_audioSource.volume >= .99f || Mathf.Approximately(_audioSource.volume, 1f))
                        _audioSource.volume = 1f;
                    else
                        _audioSource.volume = Mathf.Lerp(_audioSource.volume, 1f, .035f);
                }
            }
            else if (_bufferPos > (PlaybackClipLength / 2))
            {
                _audioSource.timeSamples = 0;
                _audioSource.loop = true;
                _audioSource.Play();
            }
        }

        _bufferPos %= PlaybackClipLength;
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

        _bufferPos = 0;
        _audioClip.SetData(EmptyClipSamples, 0);

        IsReceiving = false;
    }

    private static readonly float[] EmptyClipSamples = new float[PlaybackClipLength];
}