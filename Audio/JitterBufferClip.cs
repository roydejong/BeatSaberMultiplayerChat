using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MultiplayerChat.Audio;

public class JitterBufferClip : IDisposable
{
    private static readonly int ClipSampleSize = (int)VoiceManager.OpusFrequency; // 1 full second worth of samples
    private static readonly int ClipFeedSize = VoiceManager.FrameLength;
    
    private static readonly float[] EmptyClipSamples = new float[ClipSampleSize];
    
    public int JitterMs { get; private set; }
    
    private readonly FifoFloatStream _streamBuffer;
    private readonly AudioClip _audioClip;
    private readonly float[] _playbackBuffer;
    
    private bool _havePendingFragments;
    private bool _isJitterBuffering;
    private float _jitterStartTime;
    private bool _isWritingBuffer;
    private int _bufferPos;
    private float _lastOutputTime;

    public AudioClip AudioClip => _audioClip;
    public bool IsActive => _isWritingBuffer;
    
    public JitterBufferClip(int jitterMs)
    {
        JitterMs = jitterMs;
        
        _streamBuffer = new FifoFloatStream();
        _audioClip = AudioClip.Create("JitterBufferClip", ClipSampleSize, (int) VoiceManager.OpusChannels,
            (int) VoiceManager.OpusFrequency, false);
        _playbackBuffer = new float[ClipFeedSize];
        
        Reset();
    }
    
    #region API
    
    public void Dispose()
    {
        _streamBuffer.Close();
        Object.Destroy(_audioClip);
    }

    /// <summary>
    /// Empties any remaining buffer, and sets the audio clip data to full silence.
    /// </summary>
    public void Reset()
    {
        Console.WriteLine($"[JBC.Reset] Reset!");
        
        _streamBuffer.Flush();
        _audioClip.SetData(EmptyClipSamples, 0);        
        
        _havePendingFragments = false;
        _isJitterBuffering = false;
        _isWritingBuffer = false;
        _bufferPos = 0;
        _lastOutputTime = 0;
    }

    public void FeedFragment(float[] decodeBuffer, int decodedLength)
    {
        if (decodedLength <= 0)
            return;

        _streamBuffer.Write(decodeBuffer, 0, decodedLength);
        _havePendingFragments = true;
    }
    
    #endregion

    #region Update
    
    public void Update()
    {
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
            Console.WriteLine($"[JBC.UpdateInactive] Start jitter delay");
            _jitterStartTime = Time.unscaledTime;
            _isJitterBuffering = true;
            return;
        }
        
        // Wait for jitter buffer completion
        var jitterTime = Time.unscaledTime - _jitterStartTime;
        if ((jitterTime * 1000) < JitterMs)
            return;
        
        // Begin active playback
        Console.WriteLine($"[JBC.UpdateInactive] Finish jitter delay, start clip output");
        _isJitterBuffering = false;
        _isWritingBuffer = true;
        _bufferPos = 0;
        _lastOutputTime = 0;
        UpdateActive();
    }

    private void UpdateActive()
    {
        if (!_havePendingFragments)
        {
            // No fragments
            var deltaTime = Time.unscaledTime - _lastOutputTime;
            if (deltaTime >= .500f)
            {
                Console.WriteLine($"[JBC.UpdateActive] 500ms with no fragments, we reset");
                // >=500ms of dead air, stop
                Reset();
            }
            return;
        }

        var sampleReadLength = _streamBuffer.Peek(_playbackBuffer!, 0, ClipFeedSize);

        if (sampleReadLength != ClipFeedSize)
        {
            if (sampleReadLength < 0)
            {
                // We have read all available buffer fragments
                _havePendingFragments = false;
            }

            Console.WriteLine($"[JBC.UpdateActive] Not enough data in buffer to advance");
            return;
        }

        _streamBuffer.Advance(sampleReadLength);
        
        _audioClip.SetData(_playbackBuffer, _bufferPos);
        
        _bufferPos += sampleReadLength;
        _bufferPos %= ClipSampleSize;
        
        _lastOutputTime = Time.unscaledTime;
        
        Console.WriteLine($"[JBC.UpdateActive] Pushed {sampleReadLength} samples to audio clip");
    }

    #endregion
}