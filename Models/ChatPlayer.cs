using MultiplayerChat.Network;

namespace MultiplayerChat.Models;

public class ChatPlayer
{
    public readonly IConnectedPlayer Player;
    public readonly MpChatCapabilitiesPacket Capabilities;

    public string UserId => Player.userId;
    public string UserName => Player.userName;
    public bool IsMe => Player.isMe;

    public bool IsTyping;
    public bool IsSpeaking;
    public bool IsMuted;

    public ChatPlayer(IConnectedPlayer player, MpChatCapabilitiesPacket capabilities)
    {
        Player = player;
        Capabilities = capabilities;

        IsTyping = false;
        IsSpeaking = false;
        IsMuted = false;
    }
}