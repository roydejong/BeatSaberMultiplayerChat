using LiteNetLib.Utils;

namespace MultiplayerChat.Network;

/// <summary>
/// Unreliable packet containing a Opus-encoded voice fragment.
/// </summary>
public class MpcVoicePacket : MpcBasePacket
{
    /// <summary>
    /// Opus-encoded audio fragment (48kHz, 1 channel).
    /// If null/empty, this indicates the end of a transmission.
    /// </summary>
    public byte[]? Data;

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);

        if (Data == null)
            writer.Put(0); // int length, PutBytesWithLength doesn't like nulls
        else
            writer.PutBytesWithLength(Data);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        
        Data = reader.GetBytesWithLength();
    }
}