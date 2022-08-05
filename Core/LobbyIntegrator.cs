using System;
using System.Collections.Generic;
using BeatSaberMultiplayerChat.Models;
using BeatSaberMultiplayerChat.UI;
using HMUI;
using IPA.Utilities;
using SiraUtil.Affinity;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace BeatSaberMultiplayerChat.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class LobbyIntegrator : IInitializable, IDisposable, IAffinity
{
    [Inject] private readonly DiContainer _diContainer = null!;
    [Inject] private readonly ChatManager _chatManager = null!;
    [Inject] private readonly HoverHintController _hoverHintController = null!;

    private Sprite? _nativeIconSpeakerSound;
    private Sprite? _nativeIconMuted;

    private Dictionary<string, MultiplayerLobbyAvatarController> _playerAvatars = null!;
    private Dictionary<string, Button> _playerListButtons = null!;
    private ChatBubble _centerBubble = null!;
    private Dictionary<string, ChatBubble> _perUserBubbles = null!;

    public void Initialize()
    {
        _playerAvatars = new(10);
        _playerListButtons = new(10);
        var mainScreen = GameObject.Find("Wrapper/MenuCore/UI/ScreenSystem/ScreenContainer/MainScreen");
        _centerBubble = ChatBubble.Create(_diContainer, mainScreen.transform, ChatBubble.AlignStyle.CenterScreen);
        _perUserBubbles = new(10);

        _chatManager.ChatClearEvent += HandleChatClear;
        _chatManager.ChatMessageEvent += HandleChatMessage;
        _chatManager.ChatPlayerUpdateEvent += HandleChatPlayerUpdate;
    }

    public void Dispose()
    {
        _chatManager.ChatClearEvent -= HandleChatClear;
        _chatManager.ChatMessageEvent -= HandleChatMessage;
        _chatManager.ChatPlayerUpdateEvent -= HandleChatPlayerUpdate;
        
        _playerAvatars.Clear();
        _playerListButtons.Clear();
        
        if (_centerBubble != null)
            Object.Destroy(_centerBubble);
        
        foreach (var bubble in _perUserBubbles.Values)
            if (bubble != null)
                Object.Destroy(bubble);
        
        _perUserBubbles.Clear();
    }

    #region Text chat events

    private void HandleChatClear(object sender, EventArgs e)
    {
        if (_centerBubble != null)
            _centerBubble.HideImmediate();
        
        foreach (var userBubble in _perUserBubbles.Values)
            if (userBubble != null)
                userBubble.HideImmediate();
    }

    private void HandleChatMessage(object sender, ChatMessage message)
    {
        string? centerText = null;

        switch (message.Type)
        {
            case ChatMessageType.PlayerMessage:
            {
                if (message.SenderIsMe)
                    // No bubble
                    return;
                    
                if (message.SenderIsHost)
                    centerText = $"💬 <i><color=#2ecc71>[Server]</color> {message.Text}</i>";
                else
                    centerText = $"💬 <i><color=#3498db>[{message.UserName}]</color> {message.Text}</i>";
 
                // Show bubble over user head
                if (_perUserBubbles.TryGetValue(message.UserId, out var userBubble))
                {
                    if (userBubble.IsShowing)
                        userBubble.HideImmediate();
                    userBubble.Show($"💬 <i>{message.Text}</i>");
                }
                break;
            }
            case ChatMessageType.SystemMessage:
            {
                centerText = $"🔔 <i><color=#f1c40f>[System]</color> <color=#ecf0f1>{message.Text}</color></i>";
                break;
            }
        }

        // Show center screen bubble
        if (centerText == null)
            return;

        if (_centerBubble.IsShowing)
            _centerBubble.HideImmediate();

        _centerBubble.Show(centerText);
    }

    private void HandleChatPlayerUpdate(object sender, ChatPlayer player)
    {
        UpdatePlayerListState(player.UserId);
    }

    #endregion

    #region Player list

    [AffinityPostfix]
    [AffinityPatch(typeof(GameServerPlayerTableCell), nameof(GameServerPlayerTableCell.SetData))]
    public void PostfixPlayerCellSetData(IConnectedPlayer connectedPlayer, Button ____mutePlayerButton)
    {
        _playerListButtons[connectedPlayer.userId] = ____mutePlayerButton;

        // Show button
        ____mutePlayerButton.gameObject.SetActive(true);

        // Remove default sprite swap
        var spriteSwap = ____mutePlayerButton.GetComponent<ButtonSpriteSwapToggle>();
        if (spriteSwap != null)
        {
            if (_nativeIconSpeakerSound == null)
                _nativeIconSpeakerSound = spriteSwap.GetField<Sprite, ButtonSpriteSwap>("_normalStateSprite");
            if (_nativeIconMuted == null)
                _nativeIconMuted = spriteSwap.GetField<Sprite, ButtonSpriteSwap>("_pressedStateSprite");

            spriteSwap.enabled = false;
        }

        // Add click handler
        // TODO: Better solution. Calling this twice breaks click sfx/ripple(?).
        ____mutePlayerButton.onClick.RemoveAllListeners();
        ____mutePlayerButton.onClick.AddListener(() => HandleMuteToggleClick(connectedPlayer.userId));

        // Initial state update
        UpdatePlayerListState(connectedPlayer.userId);
    }

    private void HandleMuteToggleClick(string userId)
    {
        _chatManager.TryGetChatPlayer(userId, out var chatPlayer);

        if (chatPlayer is null)
            return;
        
        chatPlayer.IsMuted = !chatPlayer.IsMuted;
        _chatManager.SetIsPlayerMuted(userId, chatPlayer.IsMuted);
        
        UpdatePlayerListState(userId, chatPlayer);

        _hoverHintController.HideHintInstant();
        
        if (chatPlayer.IsMuted && _perUserBubbles.TryGetValue(userId, out var userBubble))
            if (userBubble != null)
                userBubble.HideAnimated();
    }

    private void UpdatePlayerListState(string userId)
    {
        _chatManager.TryGetChatPlayer(userId, out var chatPlayer);
        UpdatePlayerListState(userId, chatPlayer);
    }

    private void UpdatePlayerListState(string userId, ChatPlayer? chatPlayer)
    {
        if (!_playerListButtons.TryGetValue(userId, out var muteButton))
            return;

        var muteButtonIcon = muteButton.transform.Find("Icon").GetComponent<ImageView>();

        var hoverHint = muteButton.GetComponent<HoverHint>();
        if (hoverHint == null)
            hoverHint = _diContainer.InstantiateComponent<HoverHint>(muteButton.gameObject);

        if (chatPlayer is null)
        {
            // This player has not sent their capabilities, they are not using this mod
            hoverHint.text = "Not connected to chat";
            muteButtonIcon.sprite = _nativeIconMuted;
            muteButtonIcon.color = Color.gray;
            muteButton.interactable = false;
            return;
        }

        // The player has sent capabilities, so they are connected to chat in some form, mute should be toggleable
        muteButton.interactable = true;

        if (chatPlayer.IsMuted)
        {
            // This player is muted by us
            hoverHint.text = "Click to unmute";
            muteButtonIcon.sprite = _nativeIconMuted;
            muteButtonIcon.color = Color.red;
            return;
        }

        if (chatPlayer.IsSpeaking)
        {
            // This player is speaking and can be muted
            hoverHint.text = "Speaking (click to mute)";
            muteButtonIcon.sprite = _nativeIconSpeakerSound;
            muteButtonIcon.color = Color.white;
        }
        else
        {
            // This player is idle and can be muted
            hoverHint.text = "Connected to chat (click to mute)";
            muteButtonIcon.sprite = _nativeIconSpeakerSound;
            muteButtonIcon.color = Color.white;
        }
    }

    #endregion

    #region Lobby avatars

    [AffinityPostfix]
    [AffinityPatch(typeof(MultiplayerLobbyAvatarManager), nameof(MultiplayerLobbyAvatarManager.AddPlayer))]
    public void PostfixLobbyAvatarAddPlayer(IConnectedPlayer connectedPlayer,
        Dictionary<string, MultiplayerLobbyAvatarController> ____playerIdToAvatarMap)
    {
        if (!____playerIdToAvatarMap.TryGetValue(connectedPlayer.userId, out var playerAvatarController))
            return;

        _playerAvatars[connectedPlayer.userId] = playerAvatarController;
        
        // Create chat bubble
        if (_perUserBubbles.TryGetValue(connectedPlayer.userId, out var previousBubble))
            Object.Destroy(previousBubble);
        
        var avatarCaption = playerAvatarController.transform.Find("AvatarCaption");
        var chatBubble = ChatBubble.Create(_diContainer, avatarCaption, ChatBubble.AlignStyle.LobbyAvatar);
        
        _perUserBubbles[connectedPlayer.userId] = chatBubble;
    }

    #endregion
}