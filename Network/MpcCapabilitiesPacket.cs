using LiteNetLib.Utils;

namespace MultiplayerChat.Network;

/// <summary>
/// Reliable packet sent to each player indicating that they have the mod, and which features are supported and enabled.
/// Could be sent as an update when already previously sent.
/// </summary>
public class MpcCapabilitiesPacket : MpcBasePacket
{
    /// <summary>
    /// Is text chat supported and enabled?
    /// </summary>
    public bool CanTextChat;

    /// <summary>
    /// Is voice chat supported and enabled?
    /// </summary>
    public bool CanReceiveVoiceChat;

    /// <summary>
    /// Is voice chat supported and enabled, and is a valid recording device configured?
    /// </summary>
    public bool CanTransmitVoiceChat;

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        
        writer.Put(CanTextChat);
        writer.Put(CanReceiveVoiceChat);
        writer.Put(CanTransmitVoiceChat);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        
        CanTextChat = reader.GetBool();
        CanReceiveVoiceChat = reader.GetBool();
        CanTransmitVoiceChat = reader.GetBool();
    }
}