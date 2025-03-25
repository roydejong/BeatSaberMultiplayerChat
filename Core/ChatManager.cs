using System;
using System.Collections.Generic;
using MultiplayerChat.Audio;
using MultiplayerChat.Config;
using MultiplayerChat.Models;
using MultiplayerChat.Network;
using MultiplayerCore.Networking;
using SiraUtil.Logging;
using Zenject;

namespace MultiplayerChat.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class ChatManager : IInitializable, IDisposable
{
    public const char CommandPrefix = '/';
    
    [Inject] private readonly SiraLog _log = null!;
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly IMultiplayerSessionManager _sessionManager = null!;
    [Inject] private readonly MpPacketSerializer _packetSerializer = null!;
    [Inject] private readonly MicrophoneManager _microphoneManager = null!;
    [Inject] private readonly InputManager _inputManager = null!;

    private MpChatCapabilitiesPacket _localCapabilities = null!;
    private Dictionary<string, ChatPlayer> _chatPlayers = null!;

    public bool SessionConnected { get; private set; }

    public bool TextChatEnabled => _config.EnableTextChat;
    public bool VoiceChatEnabled => _config.EnableVoiceChat;
    public bool VoiceChatHasValidRecordingDevice => VoiceChatEnabled && _microphoneManager.HaveSelectedDevice;

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
        
        _localCapabilities = new MpChatCapabilitiesPacket()
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

        _packetSerializer.RegisterCallback<MpChatCapabilitiesPacket>(HandleCapabilitiesPacket);
        _packetSerializer.RegisterCallback<MpChatTextChatPacket>(HandleTextChat);
    }

    public void Dispose()
    {
        _sessionManager.connectedEvent -= HandleSessionConnected;
        _sessionManager.disconnectedEvent -= HandleSessionDisconnected;
        _sessionManager.playerConnectedEvent -= HandleSessionPlayerConnected;

        _packetSerializer.UnregisterCallback<MpChatCapabilitiesPacket>();
        _packetSerializer.UnregisterCallback<MpChatTextChatPacket>();

        SessionConnected = false;

        _chatPlayers.Clear();
    }

    #region API - Text chat

    /// <summary>
    /// Clears the chat box.
    /// </summary>
    public void ClearChat()
    {
        ChatClearEvent?.Invoke(this, EventArgs.Empty);

        if (!TextChatEnabled)
            return;

        ShowSystemMessage($"MultiplayerChat v{MpcVersionInfo.AssemblyVersion} " +
                          $"<color=#95a5a6>({MpcVersionInfo.AssemblyProductVersion})</color>");
    }

    /// <summary>
    /// Shows a local system message in the chat box.
    /// </summary>
    public void ShowSystemMessage(string text)
    {
        if (!TextChatEnabled || !SessionConnected)
            return;
        
        _log.Debug(text);

        ChatMessageEvent?.Invoke(this, ChatMessage.CreateSystemMessage(text));
    }

    /// <summary>
    /// Sends a text chat message to the current multiplayer session.
    /// </summary>
    public void SendTextChat(string text)
    {
        if (!SessionConnected || !TextChatEnabled || string.IsNullOrWhiteSpace(text))
            return;

        var chatPacket = new MpChatTextChatPacket()
        {
            Text = text
        };
        
        var isCommand = (text[0] == CommandPrefix);

        if (isCommand)
        {
            // Command - send to server only
            _sessionManager.SendToPlayer(chatPacket, _sessionManager.connectionOwner);
        }
        else
        {
            // Regular message - broadcast to session
            _sessionManager.Send(chatPacket);
        }
            
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

    #region API - Player States

    public void SetLocalPlayerIsSpeaking(bool isSpeaking)
    {
        if (_sessionManager.localPlayer != null) // may be null on disconnect handler
            SetPlayerIsSpeaking(_sessionManager.localPlayer.userId, isSpeaking);
    }

    public void SetPlayerIsSpeaking(string? userId, bool isSpeaking)
    {
        if (userId == null)
            return;
        
        if (!_chatPlayers.TryGetValue(userId, out var chatPlayer))
            return;

        if (chatPlayer.IsSpeaking == isSpeaking)
            return;
        
        chatPlayer.IsSpeaking = isSpeaking;
        ChatPlayerUpdateEvent?.Invoke(this, chatPlayer);
    }

    public void SetIsPlayerMuted(string userId, bool isMuted)
    {
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
        
        // Enable input
        _inputManager.TestMode = false;
        _inputManager.gameObject.SetActive(true);
    }

    private void HandleSessionDisconnected(DisconnectedReason reason)
    {
        SessionConnected = false;

        ClearChat();

        _chatPlayers.Clear();
        
        // Disable input
        _inputManager.gameObject.SetActive(false);
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

    private void HandleCapabilitiesPacket(MpChatCapabilitiesPacket packet, IConnectedPlayer sender)
    {
        // Another player that has MPC installed has announced their capabilities to us

        if (!SessionConnected)
            return;

        _log.Debug($"Received capabilities (userId={sender.userId}, protoVersion={packet.ProtocolVersion}, " +
                  $"canText={packet.CanTextChat}, canVoice={packet.CanTransmitVoiceChat})");

        var isNewEntry = !_chatPlayers.TryGetValue(sender.userId, out var prevChatPlayer);

        var chatPlayer = new ChatPlayer(sender, packet);
        chatPlayer.IsMuted = GetIsPlayerMuted(chatPlayer.UserId);
        
        if (prevChatPlayer != null)
        {
            // Keep existing state
            chatPlayer.IsTyping = prevChatPlayer.IsTyping;
            chatPlayer.IsSpeaking = prevChatPlayer.IsSpeaking;
        }
        
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

    private void HandleTextChat(MpChatTextChatPacket packet, IConnectedPlayer sender)
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