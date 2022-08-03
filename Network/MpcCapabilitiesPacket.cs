using LiteNetLib.Utils;
using MultiplayerCore.Networking.Abstractions;

namespace BeatSaberMultiplayerChat.Network;

/// <summary>
/// Signals MPC support with version and capabilities to other players.
/// </summary>
public class MpcCapabilitiesPacket : MpcBasePacket
{
    /// <summary>
    /// Indicates whether the client is capable of, and has enabled, text chat.
    /// </summary>
    public bool CanTextChat;

    /// <summary>
    /// Indicates whether the client is capable of, and has enabled, voice chat.
    /// </summary>
    public bool CanVoiceChat;

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        
        writer.Put(CanTextChat);
        writer.Put(CanVoiceChat);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        
        CanTextChat = reader.GetBool();
        CanVoiceChat = reader.GetBool();
    }
}