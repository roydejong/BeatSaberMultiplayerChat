using BeatSaberMultiplayerChat.Network;

namespace BeatSaberMultiplayerChat.Models;

public class ChatPlayer
{
    public readonly IConnectedPlayer Player;
    public readonly MpcCapabilitiesPacket Capabilities;

    public string UserId => Player.userId;
    public string UserName => Player.userName;
    public bool IsMe => Player.isMe;

    public bool IsTyping;
    public bool IsSpeaking;
    public bool IsMuted;

    public ChatPlayer(IConnectedPlayer player, MpcCapabilitiesPacket capabilities)
    {
        Player = player;
        Capabilities = capabilities;

        IsTyping = false;
        IsSpeaking = false;
        IsMuted = false;
    }
}