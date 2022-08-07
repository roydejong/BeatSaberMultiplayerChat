using LiteNetLib.Utils;
using MultiplayerCore.Networking.Abstractions;

namespace MultiplayerChat.Network;

public class MpcBasePacket : MpPacket
{
    /// <summary>
    /// The MPC protocol version used by the client.
    /// Automatically set for outgoing packets.
    /// </summary>
    /// <see cref="MpcVersionInfo.ProtocolVersion"/>
    public uint ProtocolVersion;

    public override void Serialize(NetDataWriter writer)
    {
        ProtocolVersion = MpcVersionInfo.ProtocolVersion;
        
        writer.PutVarUInt(ProtocolVersion);
    }

    public override void Deserialize(NetDataReader reader)
    {
        ProtocolVersion = reader.GetVarUInt();
    }
}