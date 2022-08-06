using System;
using System.Collections.Generic;
using BeatSaberMultiplayerChat.Models;
using BeatSaberMultiplayerChat.Network;
using MultiplayerCore.Networking;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaberMultiplayerChat.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class ChatManager : IInitializable, IDisposable
{
    [Inject] private readonly SiraLog _log = null!;
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly IMultiplayerSessionManager _sessionManager = null!;
    [Inject] private readonly MpPacketSerializer _packetSerializer = null!;

    private MpcCapabilitiesPacket _localCapabilities = null!;
    private Dictionary<string, ChatPlayer> _chatPlayers = null!;

    public bool SessionConnected { get; private set; }

    public bool TextChatEnabled => _config.EnableTextChat;
    public bool VoiceChatEnabled => false;
    public bool VoiceChatHasValidRecordingDevice => false;

    /// <summary>
    /// Invoked whenever a chat message has been received or should be presented.
    /// </summary>
    public event EventHandler<ChatMessage>? ChatMessageEvent;

    /// <summary>
    /// Invoked whenever the chat box should be cleared.
    /// </summary>
    public event EventHandler<EventArgs>? ChatClearEvent;

    /// <summary>
    /// Invoked whenever a player connects to chat or updates their settings, meaning they sent their capabilities.
    /// </summary>
    public event EventHandler<ChatPlayer>? ChatPlayerUpdateEvent;

    public void Initialize()
    {
        if (_config.MutedUserIds == null)
            _config.MutedUserIds = new();
        
        _localCapabilities = new MpcCapabilitiesPacket()
        {
            CanTextChat = TextChatEnabled,
            CanReceiveVoiceChat = VoiceChatEnabled,
            CanTransmitVoiceChat = VoiceChatEnabled && VoiceChatHasValidRecordingDevice
        };

        _chatPlayers = new(10);

        SessionConnected = false;

        _sessionManager.connectedEvent += HandleSessionConnected;
        _sessionManager.disconnectedEvent += HandleSessionDisconnected;
        _sessionManager.playerConnectedEvent += HandleSessionPlayerConnected;
        _sessionManager.playerDisconnectedEvent += HandleSessionPlayerDisconnected;

        _packetSerializer.RegisterCallback<MpcCapabilitiesPacket>(HandleCapabilitiesPacket);
        _packetSerializer.RegisterCallback<MpcTextChatPacket>(HandleTextChat);
    }

    public void Dispose()
    {
        _sessionManager.connectedEvent -= HandleSessionConnected;
        _sessionManager.disconnectedEvent -= HandleSessionDisconnected;
        _sessionManager.playerConnectedEvent -= HandleSessionPlayerConnected;

        _packetSerializer.UnregisterCallback<MpcCapabilitiesPacket>();
        _packetSerializer.UnregisterCallback<MpcTextChatPacket>();

        SessionConnected = false;

        _chatPlayers.Clear();
    }

    #region API - Text chat

    /// <summary>
    /// Clears the chat box.
    /// </summary>
    public void ClearChat()
    {
        _log.Info("<<<<<TEMP_DEBUG>>>>> Clearing chat");

        ChatClearEvent?.Invoke(this, EventArgs.Empty);

        if (!TextChatEnabled)
            return;

        ShowSystemMessage($"MultiplayerChat v{MpcVersionInfo.AssemblyProductVersion}");
    }

    /// <summary>
    /// Shows a local system message in the chat box.
    /// </summary>
    public void ShowSystemMessage(string text)
    {
        if (!TextChatEnabled || !SessionConnected)
            return;

        _log.Info($"<<<<<TEMP_DEBUG>>>>> Show system message: {text}");

        ChatMessageEvent?.Invoke(this, ChatMessage.CreateSystemMessage(text));
    }

    /// <summary>
    /// Sends a text chat message to the current multiplayer session.
    /// </summary>
    public void SendTextChat(string text)
    {
        _log.Info($"<<<<<TEMP_DEBUG>>>>> Send text: {text}");

        if (!SessionConnected || !TextChatEnabled)
            return;

        // Broadcast to session
        _sessionManager.Send(new MpcTextChatPacket()
        {
            Text = text
        });

        // Show our own message locally
        ChatMessageEvent?.Invoke(this, ChatMessage.CreateForLocalPlayer(_sessionManager.localPlayer, text));
    }

    public bool TryGetChatPlayer(string userId, out ChatPlayer? value)
    {
        if (_chatPlayers.ContainsKey(userId))
        {
            value = _chatPlayers[userId];
            return true;
        }

        value = null;
        return false;
    }

    #endregion

    #region API - Mute

    public void SetIsPlayerMuted(string userId, bool isMuted)
    {
        _log.Info($"Toggle player mute (userId={userId}, isMuted={isMuted})");

        if (isMuted && !_config.MutedUserIds!.Contains(userId))
            _config.MutedUserIds.Add(userId);
        else if (!isMuted && _config.MutedUserIds!.Contains(userId))
            _config.MutedUserIds.Remove(userId);
    }

    public bool GetIsPlayerMuted(string userId) => _config.MutedUserIds!.Contains(userId);

    #endregion

    #region Session events

    private void HandleSessionConnected()
    {
        SessionConnected = true;

        ClearChat();
        ShowSystemMessage($"Connected to {DescribeServerName()}");

        var localChatPlayer = new ChatPlayer(_sessionManager.localPlayer, _localCapabilities);
        localChatPlayer.IsMuted = GetIsPlayerMuted(_sessionManager.localPlayer.userId);
        _chatPlayers[_sessionManager.localPlayer.userId] = localChatPlayer;
        ChatPlayerUpdateEvent?.Invoke(localChatPlayer.UserId, localChatPlayer);

        // Broadcast our capabilities
        _sessionManager.Send(_localCapabilities);
    }

    private void HandleSessionDisconnected(DisconnectedReason reason)
    {
        SessionConnected = false;

        ClearChat();
    }

    private void HandleSessionPlayerConnected(IConnectedPlayer player)
    {
        if (!SessionConnected)
            return;

        _sessionManager.SendToPlayer(_localCapabilities, player);
    }

    private void HandleSessionPlayerDisconnected(IConnectedPlayer player)
    {
        if (!SessionConnected)
            return;

        _chatPlayers.Remove(player.userId);
    }

    #endregion

    #region Packet handlers

    private void HandleCapabilitiesPacket(MpcCapabilitiesPacket packet, IConnectedPlayer sender)
    {
        // Another player that has MPC installed has announced their capabilities to us

        if (!SessionConnected)
            return;

        _log.Info($"Received capabilities (userId={sender.userId}, protoVersion={packet.ProtocolVersion}, " +
                  $"canText={packet.CanTextChat}, canVoice={packet.CanTransmitVoiceChat})");

        var isNewEntry = _chatPlayers.ContainsKey(sender.userId);

        var chatPlayer = new ChatPlayer(sender, packet);
        chatPlayer.IsMuted = GetIsPlayerMuted(chatPlayer.UserId);
        _chatPlayers[sender.userId] = chatPlayer;

        if (isNewEntry)
        {
            if (packet.CanTextChat)
                ShowSystemMessage($"Player connected to chat: {sender.userName}");

            if (packet.ProtocolVersion > MpcVersionInfo.ProtocolVersion)
                ShowSystemMessage(
                    $"Player {sender.userName} is using a newer version of MultiplayerChat. " +
                    $"You should update for the best experience.");
        }

        ChatPlayerUpdateEvent?.Invoke(this, chatPlayer);
    }

    private void HandleTextChat(MpcTextChatPacket packet, IConnectedPlayer sender)
    {
        if (!SessionConnected || !TextChatEnabled)
            return;

        if (GetIsPlayerMuted(sender.userId))
            return;

        ChatMessageEvent?.Invoke(this, ChatMessage.CreateFromPacket(packet, sender));
    }

    #endregion

    #region Utils

    private string DescribeServerName()
    {
        if (_sessionManager.connectionOwner is not null &&
            !string.IsNullOrWhiteSpace(_sessionManager.connectionOwner.userName))
        {
            // Specific server name
            return _sessionManager.connectionOwner.userName;
        }

        return "Dedicated Server";
    }

    #endregion
}