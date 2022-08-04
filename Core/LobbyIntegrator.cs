using System;
using System.Collections.Generic;
using BeatSaberMultiplayerChat.Models;
using BeatSaberMultiplayerChat.UI;
using HMUI;
using IPA.Utilities;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaberMultiplayerChat.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class LobbyIntegrator : IInitializable, IDisposable, IAffinity
{
    [Inject] private readonly SiraLog _log = null!;
    [Inject] private readonly DiContainer _diContainer = null!;
    [Inject] private readonly ChatManager _chatManager = null!;
    [Inject] private readonly HoverHintController _hoverHintController = null!;

    private Sprite? _nativeIconSpeakerSound;
    private Sprite? _nativeIconMuted;

    private Dictionary<string, Button> _playerListButtons = null!;

    private ChatBubble _centerBubble = null!;
    private Dictionary<string, ChatBubble> _perUserBubbles = null!;

    public void Initialize()
    {
        _playerListButtons = new(10);

        var mainScreen = GameObject.Find("Wrapper/MenuCore/UI/ScreenSystem/ScreenContainer/MainScreen");
        _centerBubble = ChatBubble.Create(_diContainer, mainScreen.transform);
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
    }

    #region Text chat

    private void HandleChatClear(object sender, EventArgs e)
    {
        _log.Info("HandleChatClear");
        
        _centerBubble.HideImmediate();
    }

    private void HandleChatMessage(object sender, ChatMessage message)
    {
        _log.Info("HandleChatMessage");
        
        string? messageText = null;

        switch (message.Type)
        {
            case ChatMessageType.PlayerMessage:
            {
                messageText = $"<color=#3498db>[{message.UserName}]</color> {message.Text}";
                break;
            }
            case ChatMessageType.SystemMessage:
            {
                messageText = $"<color=#f1c40f>[System]</color> <color=#ecf0f1>{message.Text}</color>";
                break;
            }
        }

        if (messageText == null)
            return;
        
        if (_centerBubble.IsShowing)
            _centerBubble.HideImmediate();
           
        _log.Info("HandleChatMessage -> Show");
        _centerBubble.Show(messageText);
    }

    private void HandleChatPlayerUpdate(object sender, ChatPlayer player)
    {
        UpdatePlayerState(player.UserId);
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
        UpdatePlayerState(connectedPlayer.userId);
    }

    private void HandleMuteToggleClick(string userId)
    {
        _chatManager.TryGetChatPlayer(userId, out var chatPlayer);

        if (chatPlayer is null)
            return;

        _chatManager.SendTextChat($"Mute toggle for {chatPlayer.UserName}");

        chatPlayer.IsMuted = !chatPlayer.IsMuted;
        UpdatePlayerState(userId, chatPlayer);

        _hoverHintController.HideHintInstant();

        // TODO Proper mute logic (persist to config)
    }

    private void UpdatePlayerState(string userId)
    {
        _chatManager.TryGetChatPlayer(userId, out var chatPlayer);
        UpdatePlayerState(userId, chatPlayer);
    }

    private void UpdatePlayerState(string userId, ChatPlayer? chatPlayer)
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
}