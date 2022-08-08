using LiteNetLib.Utils;

namespace MultiplayerChat.Network;

public class MpcVoicePacket : MpcBasePacket
{
    public byte[] Data;
    
    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        
        writer.PutBytesWithLength(Data);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        
        Data = reader.GetBytesWithLength();
    }
}