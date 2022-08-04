using BeatSaberMultiplayerChat.Network;

namespace BeatSaberMultiplayerChat.Models;

public class ChatMessage
{
    public ChatMessageType Type;
    public string UserId;
    public string UserName;
    public string Text;

    private ChatMessage(ChatMessageType type, string userId, string userName, string text)
    {
        Type = type;
        UserId = userId;
        UserName = userName;
        Text = text;
    }

    public static ChatMessage CreateForLocalPlayer(IConnectedPlayer localPlayer, string text) => new
    (
        type: ChatMessageType.PlayerMessage,
        userId: localPlayer.userId,
        userName: localPlayer.userName,
        text: text
    );

    public static ChatMessage CreateFromPacket(MpcTextChatPacket packet, IConnectedPlayer sender) => new
    (
        type: ChatMessageType.PlayerMessage,
        userId: sender.userId,
        userName: sender.userName,
        text: packet.Text ?? ""
    );

    public static ChatMessage CreateSystemMessage(string text) => new
    (
        type: ChatMessageType.SystemMessage,
        userId: "system",
        userName: "System",
        text: text
    );
}

public enum ChatMessageType
{
    PlayerMessage = 0,
    SystemMessage = 1
}