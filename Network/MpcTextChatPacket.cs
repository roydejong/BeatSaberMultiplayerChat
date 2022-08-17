using LiteNetLib.Utils;

namespace MultiplayerChat.Network;

/// <summary>
/// Reliable packet containing a simple text chat message.
/// </summary>
public class MpcTextChatPacket : MpcBasePacket
{
    /// <summary>
    /// Raw chat message.
    /// Note: any HTML-style `<tags>` will be stripped from the message before it is displayed, to avoid rich text chaos.
    /// </summary>
    public string? Text;
    
    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        
        writer.Put(Text);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        
        Text = reader.GetString();
    }
}