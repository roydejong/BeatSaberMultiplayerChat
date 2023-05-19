using System;
using System.Text.RegularExpressions;
using MultiplayerChat.Network;
using BeatSaberMarkupLanguage;
using UnityEngine;
using System.Linq;

namespace MultiplayerChat.Models;

public class ChatMessage
{
    public readonly ChatMessageType Type;
    public readonly string UserId;
    public readonly string UserName;
    public readonly string Text;

    public readonly bool SenderIsHost;
    public readonly bool SenderIsMe;

    private static Sprite? _playerIcon = null;
    public static Sprite PlayerIcon => _playerIcon ?? (_playerIcon = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "PlayerIcon"));

    private static Sprite? _noFailIcon = null;
    public static Sprite NoFailIcon => _noFailIcon ?? (_noFailIcon = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "NoFailIcon"));

    private static Sprite? _globalIcon = null;
    public static Sprite GlobalIcon => _globalIcon ?? (_globalIcon = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "GlobalIcon"));

    private ChatMessage(ChatMessageType type, string userId, string userName, string text, bool senderIsHost, 
        bool senderIsMe, bool stripTags = true)
    {
        Type = type;
        UserId = userId;
        UserName = stripTags ? StripTags(userName) : userName;
        Text = stripTags ? StripTags(text) : text;

        SenderIsHost = senderIsHost;
        SenderIsMe = senderIsMe;
    }

    public string FormatMessage(bool inPlayerBubble = false, bool inChatList = false)
    {
        // extra spacing used when a sprite is used for the icon
        var spacing = inChatList ? "" : "\t";
        if (inPlayerBubble)
            return $"{spacing}<i>{Text}</i>";
        else if (Type is ChatMessageType.SystemMessage)
            return $"{spacing}<i><color=#f1c40f>[System]</color> <color=#ecf0f1>{Text}</color></i>";
        else if (SenderIsHost)
            return $"{spacing}<i><color=#2ecc71>[Server]</color> {Text}</i>";
        else if (SenderIsMe)
            return $"{spacing}<i><color=#95a5a6>[{UserName}]</color> {Text}</i>";
        else
            return $"{spacing}<i><color=#3498db>[{UserName}]</color> {Text}</i>";
    }

    public Sprite SpriteForMessage(bool inPlayerBubble = false)
    {
        if (inPlayerBubble)
            return PlayerIcon;
        else if (Type is ChatMessageType.SystemMessage)
            return NoFailIcon;
        else if (SenderIsHost)
            return GlobalIcon;
        else if (SenderIsMe)
            return PlayerIcon;
        else
            return PlayerIcon;
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
        senderIsMe: localPlayer.isMe,
        stripTags: true
    );

    public static ChatMessage CreateFromPacket(MpcTextChatPacket packet, IConnectedPlayer sender) => new
    (
        type: ChatMessageType.PlayerMessage,
        userId: sender.userId,
        userName: sender.userName,
        text: packet.Text ?? "",
        senderIsHost: sender.isConnectionOwner,
        senderIsMe: sender.isMe,
        stripTags: true
    );

    public static ChatMessage CreateSystemMessage(string text) => new
    (
        type: ChatMessageType.SystemMessage,
        userId: "system",
        userName: "System",
        text: text,
        senderIsHost: false,
        senderIsMe: false,
        stripTags: false
    );
}

public enum ChatMessageType
{
    PlayerMessage = 0,
    SystemMessage = 1
}