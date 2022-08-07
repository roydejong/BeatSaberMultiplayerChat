using System;
using System.Text.RegularExpressions;
using MultiplayerChat.Network;

namespace MultiplayerChat.Models;

public class ChatMessage
{
    public readonly ChatMessageType Type;
    public readonly string UserId;
    public readonly string UserName;
    public readonly string Text;

    public readonly bool SenderIsHost;
    public readonly bool SenderIsMe;

    private ChatMessage(ChatMessageType type, string userId, string userName, string text, bool senderIsHost, bool senderIsMe)
    {
        Type = type;
        UserId = userId;
        UserName = StripTags(userName);
        Text = StripTags(text);

        SenderIsHost = senderIsHost;
        SenderIsMe = senderIsMe;
    }
    
    private static string StripTags(string input)
        => Regex.Replace(input, "<.*?>", String.Empty);

    public static ChatMessage CreateForLocalPlayer(IConnectedPlayer localPlayer, string text) => new
    (
        type: ChatMessageType.PlayerMessage,
        userId: localPlayer.userId,
        userName: localPlayer.userName,
        text: text,
        senderIsHost: localPlayer.isConnectionOwner,
        senderIsMe: localPlayer.isMe
    );

    public static ChatMessage CreateFromPacket(MpcTextChatPacket packet, IConnectedPlayer sender) => new
    (
        type: ChatMessageType.PlayerMessage,
        userId: sender.userId,
        userName: sender.userName,
        text: packet.Text ?? "",
        senderIsHost: sender.isConnectionOwner,
        senderIsMe: sender.isMe
    );

    public static ChatMessage CreateSystemMessage(string text) => new
    (
        type: ChatMessageType.SystemMessage,
        userId: "system",
        userName: "System",
        text: text,
        senderIsHost: false,
        senderIsMe: false
    );
}

public enum ChatMessageType
{
    PlayerMessage = 0,
    SystemMessage = 1
}