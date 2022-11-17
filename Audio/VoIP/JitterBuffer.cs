using System;
using System.Collections.Concurrent;
using ModestTree;
using MultiplayerChat.Network;
using UnityEngine;

namespace MultiplayerChat.Audio.VoIP;

/// <summary>
/// Buffers a queue of voice fragments for (hopefully) latency-proof playback.
/// </summary>
public class JitterBuffer : IDisposable
{
    public const int DefaultSizeMs = 60;

    private readonly OpusDecodeThread _decoder;
    private readonly int _durationMs;
    private float? _playbackStartTimeMs;
    private readonly ConcurrentQueue<OpusDecodeThread.DecodedBuffer> _decodedFragments;
    private OpusDecodeThread.DecodedBuffer? _currentFragment;
    private int _currentFragmentPos;

    /// <summary>
    /// Gets whether the jitter buffer is completely empty, i.e. all received packets were fully returned.
    /// </summary>
    public bool IsEmpty => _decodedFragments.IsEmpty() && _currentFragment is not null;

    /// <summary>
    /// Gets the current buffer delay until playback should begin.
    /// Will return 0 if already playing.
    /// </summary>
    public float CurrentDelayMs
    {
        get
        {
            if (_playbackStartTimeMs.HasValue)
            {
                var now = Time.realtimeSinceStartup;

                if (_playbackStartTimeMs > now)
                {
                    // Playback has not started yet, we have a delay
                    return _playbackStartTimeMs.Value - now;
                }

                // Playback (should have started), there is no delay
                return 0;
            }

            // Playback is not queued, the theoretical delay is the full duration
            return _durationMs;
        }
    }

    public JitterBuffer(OpusDecodeThread decoder, int durationMsMs = DefaultSizeMs)
    {
        _decoder = decoder;
        _durationMs = durationMsMs;
        _decodedFragments = new();

        Clear();
    }

    public void Dispose() => Clear();

    public void Clear()
    {
        while (_decodedFragments.TryDequeue(out var disposeFragment))
            disposeFragment.Dispose();

        if (_currentFragment != null)
        {
            _currentFragment.Dispose();
            _currentFragment = null;
        }
        
        _playbackStartTimeMs = null;
        _currentFragment = null;
        _currentFragmentPos = 0;
    }

    public void Feed(MpcVoicePacket packet)
    {
        if (_playbackStartTimeMs == null)
            _playbackStartTimeMs = Time.realtimeSinceStartup + (_durationMs / 1000f);
        
        _decoder.Enqueue(packet, OnDecodeSuccess);
    }

    private void OnDecodeSuccess(OpusDecodeThread.DecodedBuffer obj) => 
        _decodedFragments.Enqueue(obj);

    public void ReadNext(float[] targetBuffer, out bool isEndOfTransmission, out bool isSilence, out string logText)
    {
        isEndOfTransmission = false;
        isSilence = false;
        logText = "stepbro stuck test";
        return;

        if (IsEmpty || _playbackStartTimeMs is null || _playbackStartTimeMs > Time.realtimeSinceStartup)
        {
            // No data yet / no data remaining / still delaying playback -- clear samples from buffer section  
            Array.Clear(targetBuffer, 0, targetBuffer.Length);

            if (IsEmpty)
            {
                // There is no data, ReadNext shouldn't have been called, trigger end of transmission
                isEndOfTransmission = true;
                Clear();
            }

            isSilence = true;
            logText = "waiting for data / waiting for delay timer";
            return;
        }

        var bufferPos = 0;
        var bufferLengthRemaining = targetBuffer.Length;

        logText = "read loop - before";

        while (bufferLengthRemaining > 0)
        {
            if (_currentFragment is null)
            {
                // Try move to next fragment
                if (_decodedFragments.TryDequeue(out var nextFragment))
                {
                    _currentFragment = nextFragment; 
                    _currentFragmentPos = 0;
                    logText = "read loop - frag next";
                }
                else
                {
                    // There is no more data in the queue, add silence to remainder and reset
                    Array.Clear(targetBuffer, bufferPos, bufferLengthRemaining);
                    isEndOfTransmission = true;
                    Clear();
                    logText = "eot because no more frags";
                    return;
                }
            }
            else
            {
                logText = "read loop - frag continue";
            }

            var fragmentLength = _currentFragment.Length;

            if (fragmentLength == 0)
            {
                // Fragment is empty, i.e. end-of-transmission
                isEndOfTransmission = true;
                Clear();
                logText = "empty fragment (end of transmission)";
                return;
            }
            
            var writeLength = fragmentLength < bufferLengthRemaining ? fragmentLength : bufferLengthRemaining;

            Buffer.BlockCopy(_currentFragment.Data, _currentFragmentPos, targetBuffer, bufferPos, writeLength);

            bufferPos += writeLength;
            bufferLengthRemaining -= writeLength;
            _currentFragmentPos += writeLength;

            if (_currentFragmentPos >= _currentFragment.Length)
            {
                // Fragment is complete
                _currentFragment = null;
                _currentFragmentPos = 0;
                logText = "read loop - frag complete";
            }
        }
    }
}