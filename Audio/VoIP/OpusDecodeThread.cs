using System;
using System.Collections.Concurrent;
using System.Threading;
using MultiplayerChat.Network;
using SiraUtil.Logging;
using UnityOpus;
using Zenject;

namespace MultiplayerChat.Audio.VoIP;

public class OpusDecodeThread : IInitializable, IDisposable
{
    private static readonly ArrayPool<float> DecodePool = ArrayPool<float>.GetPool(OpusConstants.FrameSampleLength);

    [Inject] private readonly SiraLog _log = null!;

    private readonly Decoder _opusDecoder;
    private bool _keepAlive;
    private Thread? _thread;
    private ConcurrentQueue<DecodeRequest> _queue;
    private AutoResetEvent _waitHandle;

    public OpusDecodeThread()
    {
        _opusDecoder = new(OpusConstants.Frequency, OpusConstants.Channels);
        _keepAlive = false;
        _thread = null;
        _queue = new();
        _waitHandle = new(true);
    }

    public void Initialize() => Start();

    public void Dispose() => Stop();

    public void Start()
    {
        Stop();

        _keepAlive = true;

        _thread = new Thread(__Run);
        _thread.Name = "MpcOpusDecodeThread";
        _thread.Start();

        _log.Info("Started OpusDecodeThread");
    }

    public void Stop()
    {
        _keepAlive = false;
        _waitHandle.Set();

        if (_thread != null)
        {
            _thread.Abort();
            _thread = null;
        }

        _log.Info("Stopped OpusDecodeThread");
    }

    private void __Run()
    {
        try
        {
            while (_keepAlive)
            {
                while (_queue.TryDequeue(out var request))
                    HandleRequest(request);

                _waitHandle.WaitOne();
            }
        }
        catch (ThreadAbortException)
        {
        }
    }

    public void Enqueue(MpcVoicePacket packet, Action<DecodedBuffer> onDecodeSuccess) =>
        Enqueue(new DecodeRequest(packet, onDecodeSuccess));

    public void Enqueue(DecodeRequest request)
    {
        _queue.Enqueue(request);
        _waitHandle.Set();
    }

    private void HandleRequest(DecodeRequest request)
    {
        DecodedBuffer? buffer = null;

        try
        {
            buffer = DecodedBuffer.Spawn();

            var length = _opusDecoder.Decode(request.VoicePacket.Data, request.VoicePacket.DataLength, buffer.Data);
            buffer.Length = length;

            request.OnDecodeSuccess(buffer);
        }
        catch (Exception ex)
        {
            buffer?.Dispose();
            _log.Error($"Opus decode error: {ex}");
        }
    }

    public class DecodedBuffer : IDisposable
    {
        /// <summary>
        /// Decoded PCM sample buffer.
        /// Should not be accessed once disposed.
        /// </summary>
        public float[] Data { get; private set; }

        /// <summary>
        /// Length of the contents within the sample buffer.
        /// </summary>
        public int Length;

        /// <summary>
        /// Indicates whether Dispose() has been called, and the buffer has been released.
        /// Once disposed, the buffer should not be accessed anymore as it is no longer reserved. 
        /// </summary>
        public bool IsDisposed { get; private set; }

        public DecodedBuffer(float[] data, int length = 0)
        {
            Data = data;
            Length = length;
            IsDisposed = false;
        }

        public static DecodedBuffer Spawn() => new(DecodePool.Spawn());

        public void Dispose()
        {
            if (IsDisposed)
                return;

            DecodePool.Despawn(Data);
            IsDisposed = true;
        }
    }

    public class DecodeRequest
    {
        public MpcVoicePacket VoicePacket;
        public Action<DecodedBuffer> OnDecodeSuccess;

        public DecodeRequest(MpcVoicePacket voicePacket, Action<DecodedBuffer> onDecodeSuccess)
        {
            VoicePacket = voicePacket;
            OnDecodeSuccess = onDecodeSuccess;
        }
    }
}