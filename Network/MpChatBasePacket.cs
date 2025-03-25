using LiteNetLib.Utils;
using MultiplayerCore.Networking.Abstractions;

namespace MultiplayerChat.Network;

public class MpChatBasePacket : MpPacket
{
    /// <summary>
    /// The MPChat protocol version used by the client.
    /// Automatically set for outgoing packets.
    /// </summary>
    /// <see cref="MpChatVersionInfo.ProtocolVersion"/>
    public uint ProtocolVersion;

    public override void Serialize(NetDataWriter writer)
    {
        ProtocolVersion = MpChatVersionInfo.ProtocolVersion;
        
        writer.PutVarUInt(ProtocolVersion);
    }

    public override void Deserialize(NetDataReader reader)
    {
        ProtocolVersion = reader.GetVarUInt();
    }
}