using System;
using LiteNetLib.Utils;
using MultiplayerChat.Audio;
using Zenject;

namespace MultiplayerChat.Network;

/// <summary>
/// Unreliable packet containing a Opus-encoded voice fragment.
/// </summary>
public class MpChatVoicePacket : MpChatBasePacket, IPoolablePacket
{
    /// <summary>
    /// Rolling sequence number of the audio fragment (modulo 256).
    /// </summary>
    public uint Index;
    /// <summary>
    /// Opus-encoded audio fragment (48kHz, 1 channel).
    /// If null/empty, this indicates the end of a transmission.
    /// </summary>
    public byte[]? Data;
    
    private bool _isRentedBuffer;
    private int? _bufferContentSize;

    public int DataLength => _bufferContentSize ?? Data?.Length ?? 0;

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        
        writer.PutVarUInt(Index);

        if (Data == null || _bufferContentSize == 0)
        {
            writer.Put(0);
            return;
        }
        
        writer.PutBytesWithLength(Data, 0, DataLength);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        
        Index = reader.GetVarUInt();
        
        var length = reader.GetInt();

        if (length == 0)
        {
            ReturnPooledBuffer();
            return;
        }
        
        AllocatePooledBuffer(length);
        reader.GetBytes(Data, length);
    }

    #region Packet Pool
    
    protected static PacketPool<MpChatVoicePacket> Pool => ThreadStaticPacketPool<MpChatVoicePacket>.pool;

    public static MpChatVoicePacket Obtain() => Pool.Obtain();
    
    public void Release()
    {
        ReturnPooledBuffer();
        
        Data = null;
        
        _isRentedBuffer = false;
        _bufferContentSize = null;
        
        Pool.Release(this);
    }
    
    #endregion

    #region Byte Pool

    // Frame size should be ~240 bytes based on 20ms @ 96000 bitrate, but may vary in practice
    protected static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.GetPool(512);

    public void AllocatePooledBuffer(int encodedSize)
    {
        ReturnPooledBuffer();
        
        Data = BytePool.Spawn();

        if (Data.Length < encodedSize)
            throw new InvalidOperationException($"Rented buffer is too small (need={encodedSize}, got={Data.Length})");
        
        _isRentedBuffer = true;
        _bufferContentSize = encodedSize;
    }

    private void ReturnPooledBuffer()
    {
        if (Data == null || !_isRentedBuffer)
            return;
        
        BytePool.Despawn(Data);
        
        Data = null;
        
        _isRentedBuffer = false;
        _bufferContentSize = null;
    }
    
    #endregion
}