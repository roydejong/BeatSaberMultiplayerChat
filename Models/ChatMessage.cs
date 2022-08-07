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

    public string FormatMessage(bool inPlayerBubble = false)
    {
        if (inPlayerBubble)
            return $"💬 <i>{Text}</i>";
        else if (Type is ChatMessageType.SystemMessage)
            return $"🔔 <i><color=#f1c40f>[System]</color> <color=#ecf0f1>{Text}</color></i>";
        else if (SenderIsHost)
            return $"💬 <i><color=#2ecc71>[Server]</color> {Text}</i>";
        else
            return $"💬 <i><color=#3498db>[{UserName}]</color> {Text}</i>";
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