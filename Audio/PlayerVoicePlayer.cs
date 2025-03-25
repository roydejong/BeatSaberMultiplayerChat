using System;
using System.Runtime.CompilerServices;
using BeatSaber.AvatarCore;
using IPA.Utilities;
using UnityEngine;

namespace MultiplayerChat.Audio;

public class PlayerVoicePlayer : IDisposable
{
    #region Base
    
    private static readonly int ClipSampleSize = (int)VoiceManager.DecodeFrequency; // 1 full second worth of samples
    private static readonly int ClipFeedSize = VoiceManager.MaxFrameLength; // max and decode are the same (20ms@48khz)
    private static readonly float[] EmptyClipSamples = new float[ClipSampleSize];
    
    public string PlayerUserId { get; private set; }
    public bool IsPlaying { get; private set; }
    public int JitterBufferMs { get; private set; }
    
    private readonly float _spatialBlend;
    private AudioSource? _audioSource;
    private readonly FifoFloatStream _streamBuffer;
    private readonly AudioClip _audioClip;
    private readonly float[] _playbackBuffer;
    
    private bool _havePendingFragments;
    private bool _isJitterBuffering;
    private float _jitterStartTime;
    private bool _isWritingBuffer;
    private int _lastPlaybackPos;
    private int _playbackIterations;
    private int _bufferPos;
    private int _bufferIterations;
    private bool _transmissionEnding;
    private bool _transmissionEnded;
    private int _deadFrames;

    /// <summary>
    /// This event is raised when buffering begins, upon receiving a first voice fragment.
    /// </summary>
    public event EventHandler? StartBufferingEvent;
    /// <summary>
    /// This event is raised when buffering is complete and playback begins. 
    /// </summary>
    public event EventHandler? StartPlaybackEvent;
    /// <summary>
    /// This event is raised when playback stops for any reason. 
    /// This happens when StopImmediate() is called, or when the buffer has been fully played back and is now empty.
    /// Voice fragments received after this event will be subject to buffering again.
    /// </summary>
    public event EventHandler? StopPlaybackEvent;

    public PlayerVoicePlayer(string playerUserId, int jitterBufferMs, float spatialBlend)
    {
        PlayerUserId = playerUserId;
        IsPlaying = false;
        JitterBufferMs = jitterBufferMs;
        
        _spatialBlend = spatialBlend;
        _audioSource = null;
        _streamBuffer = new FifoFloatStream();
        _audioClip = AudioClip.Create("JitterBufferClip", ClipSampleSize, (int) VoiceManager.OpusChannels,
            (int) VoiceManager.DecodeFrequency, false);
        _playbackBuffer = new float[ClipFeedSize];

        StopImmediate();
    }
    
    #endregion
    
    #region API

    /// <summary>
    /// Abort playback, empty buffers, and completely reset all state.
    /// </summary>
    public void StopImmediate()
    {
        _audioClip.SetData(EmptyClipSamples, 0);        
        
        if (_audioSource != null)
        {
            _audioSource.loop = false;
            _audioSource.timeSamples = 0;
            _audioSource.volume = 0f;
            _audioSource.Stop();
        }

        if (IsPlaying)
        {
            IsPlaying = false;
            StopPlaybackEvent?.Invoke(this, EventArgs.Empty);
        }

        _streamBuffer.Flush();
        
        _havePendingFragments = false;
        _isJitterBuffering = false;
        _isWritingBuffer = false;
        _lastPlaybackPos = 0;
        _playbackIterations = 0;
        _bufferPos = 0;
        _bufferIterations = 0;
        _transmissionEnding = false;
        _transmissionEnded = true;
        _deadFrames = 0;
    }

    public void Dispose()
    {
        _streamBuffer.Close();
        UnityEngine.Object.Destroy(_audioClip);
        IsPlaying = false;
    }

    public void FeedFragment(float[] decodeBuffer, int decodedLength)
    {
        if (decodedLength <= 0)
        {
            _transmissionEnding = true;
            return;
        }

        _streamBuffer.Write(decodeBuffer, 0, decodedLength);
        
        _havePendingFragments = true;

        if (_transmissionEnded)
        {
            _transmissionEnding = false;
            _transmissionEnded = false;
        }
    }
    
    #endregion

    #region Audio Source
    
    public void SetMultiplayerAvatarAudioController(MultiplayerAvatarAudioController avatarAudio) =>
        ConfigureAudioSource(avatarAudio.GetField<AudioSource, MultiplayerAvatarAudioController>("_audioSource"));

    public void ConfigureAudioSource(AudioSource audioSource)
    {
        _audioSource = audioSource;
        _audioSource.clip = _audioClip;
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

    private void StartPlayback()
    {
        if (_audioSource == null || IsPlaying)
            return;
        
        _audioSource.timeSamples = 0;
        _audioSource.loop = true;
        _audioSource.volume = 1f;
        
        _audioSource.Play();
        
        IsPlaying = true;
        StartPlaybackEvent?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Update
    
    public void Update()
    {
        if (_audioSource == null)
        {
            // Playback not enabled
            if (_havePendingFragments || _isWritingBuffer)
                StopImmediate();
            return;
        }

        if (_isWritingBuffer)
            UpdateActive();
        else
            UpdateInactive();
    }

    private void UpdateInactive()
    {
        if (!_havePendingFragments)
        {
            // No pending fragments, nothing to do
            _isJitterBuffering = false;
            return;
        }

        if (!_isJitterBuffering)
        {
            // Start jitter buffering
            _jitterStartTime = Time.unscaledTime;
            _isJitterBuffering = true;
            StartBufferingEvent?.Invoke(this, EventArgs.Empty);
            return;
        }
        
        // Wait for jitter buffer completion
        var jitterTime = Time.unscaledTime - _jitterStartTime;
        if ((jitterTime * 1000) < JitterBufferMs)
            return;
        
        // Begin active playback
        _isJitterBuffering = false;
        _isWritingBuffer = true;
        _bufferPos = 0;
        
        StartPlayback();
        UpdateActive(true);
    }

    private void UpdateActive(bool firstUpdate = false)
    {
        var playbackPos = _audioSource!.timeSamples;

        for (var i = 0; i < 3; i++) 
        {
            var peekSampleCount = _streamBuffer.Peek(_playbackBuffer!, 0, ClipFeedSize);

            if (peekSampleCount == 0)
            {
                _havePendingFragments = false;
                
                if (_transmissionEnding)
                {
                    StopImmediate();
                    return;
                }

                var absPlaybackPos = GetAbsoluteSamples(_playbackIterations, playbackPos);
                var absBufferPos = GetAbsoluteSamples(_bufferIterations, _bufferPos);

                if ((absPlaybackPos + ClipFeedSize) <= absBufferPos)
                    return;
                
                // Buffer has been depleted, playback has caught up up to us
                // As audio will loop around, stale samples will be played which we really don't want
                
                if (++_deadFrames < 5)
                    return;
                
                StopImmediate();
                return;
            }

            _streamBuffer.Advance(peekSampleCount);

            _audioClip.SetData(_playbackBuffer, _bufferPos);
            
            _bufferPos += peekSampleCount;
            if (_bufferPos >= ClipSampleSize)
                _bufferIterations++;
            _bufferPos %= ClipSampleSize;
            
            if (playbackPos < _lastPlaybackPos)
                _playbackIterations++;
            _lastPlaybackPos = playbackPos;

            _deadFrames = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetAbsoluteSamples(int iterations, int samples)
    {
        while (samples >= ClipSampleSize)
        {
            samples -= ClipSampleSize;
            iterations++;
        }
        return (ClipSampleSize * iterations) + samples;
    }

    #endregion
}