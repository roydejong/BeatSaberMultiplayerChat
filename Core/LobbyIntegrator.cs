using System;
using System.Collections.Generic;
using HMUI;
using IPA.Utilities;
using MultiplayerChat.Audio;
using MultiplayerChat.Models;
using MultiplayerChat.UI;
using MultiplayerChat.UI.Lobby;
using SiraUtil.Affinity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace MultiplayerChat.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class LobbyIntegrator : IInitializable, IDisposable, IAffinity
{
    [Inject] private readonly DiContainer _diContainer = null!;
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly ChatManager _chatManager = null!;
    [Inject] private readonly VoiceManager _voiceManager = null!;
    [Inject] private readonly HoverHintController _hoverHintController = null!;
    [Inject] private readonly SoundNotifier _soundNotifier = null!;
    [Inject] private readonly ChatViewController _chatViewController = null!;
    [Inject] private readonly GameServerLobbyFlowCoordinator _lobbyFlowCoordinator = null!;
    [Inject] private readonly ServerPlayerListViewController _serverPlayerListViewController = null!;

    private Sprite? _nativeIconSpeakerSound;
    private Sprite? _nativeIconMuted;

    private Dictionary<string, MultiplayerLobbyAvatarController> _playerAvatars = null!;
    private Dictionary<string, Button> _playerListButtons = null!;
    private ChatBubble _centerBubble = null!;
    private Dictionary<string, ChatBubble> _perUserBubbles = null!;
    
    private ChatButton _chatTitleButton = null!;

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
        
        _chatTitleButton = ChatButton.Create(_diContainer);
        _chatTitleButton.gameObject.SetActive(false);
        _chatTitleButton.OnClick += HandleChatTitleButtonClick;
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

        if (_chatTitleButton != null)
            Object.Destroy(_chatTitleButton.gameObject);
    }

    #region Text chat events

    private void HandleChatClear(object sender, EventArgs e)
    {
        // Player bubbles
        foreach (var userBubble in _perUserBubbles.Values)
            if (userBubble != null)
                userBubble.HideImmediate();
        
        // Center bubble
        if (_centerBubble != null)
            _centerBubble.HideImmediate();
        
        // Chat view
        _chatViewController.ClearMessages();
        
        // Unread badge
        _chatTitleButton.HideUnread();
    }

    private void HandleChatMessage(object sender, ChatMessage message)
    {
        // Player bubble
        var showPlayerBubble = _config.EnablePlayerBubbles && !message.SenderIsHost && !message.SenderIsMe;

        if (showPlayerBubble && _perUserBubbles.TryGetValue(message.UserId, out var userBubble) && userBubble != null)
        {
            if (userBubble.IsShowing)
                userBubble.HideImmediate();
            
            userBubble.Show(message.FormatMessage(inPlayerBubble: true));
        }
        
        // Center bubble
        var showCenterBubble =  _config.EnableCenterBubbles && !message.SenderIsMe;

        if (showCenterBubble && _centerBubble != null)
        { 
            if (_centerBubble.IsShowing)
                _centerBubble.HideImmediate();
            
            _centerBubble.Show(message.FormatMessage());
        }
        
        // Notification sound
        if (!message.SenderIsMe && message.Type == ChatMessageType.PlayerMessage)
            _soundNotifier.Play();
        
        // Chat view 
        _chatViewController.AddMessage(message);
        
        // Unread badge
        if (!message.SenderIsMe && message.Type == ChatMessageType.PlayerMessage && !_chatViewController.isActivated)
            _chatTitleButton.ShowUnread();
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
        var callback = new UnityAction(() => HandleMuteToggleClick(connectedPlayer.userId));
        ____mutePlayerButton.onClick.RemoveListener(callback);
        ____mutePlayerButton.onClick.AddListener(callback);

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
        
        // Register voice playback
        _voiceManager.ProvideAvatarAudio(playerAvatarController.GetComponent<MultiplayerAvatarAudioController>());
    }

    #endregion

    #region Lobby setup view

    [AffinityPostfix]
    [AffinityPatch(typeof(LobbySetupViewController), "DidActivate")]
    public void PostfixLobbySetupActivation()
    {
        _chatTitleButton.gameObject.SetActive(true);   
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(ViewController), "DidDeactivate")]
    public void PostfixLobbySetupDeactivation()
    {
        // This will trigger for *any* view controller because LobbySetupViewController doesn't explicitly
        //  implement this, and Harmony will complain 
        _chatTitleButton.gameObject.SetActive(false);   
    }

    private void HandleChatTitleButtonClick(object sender, EventArgs e)
    {
        _lobbyFlowCoordinator.InvokeMethod<object, FlowCoordinator>("PresentViewController", new object[]
        {
            _chatViewController,
            null, // Action finishedCallback
            ViewController.AnimationDirection.Horizontal,
            false // bool immediately
        });

        _lobbyFlowCoordinator.InvokeMethod<object, FlowCoordinator>("SetRightScreenViewController", new object[]
        {
            _serverPlayerListViewController,
            ViewController.AnimationType.None
        });
        
        _chatTitleButton.HideUnread();
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(GameServerLobbyFlowCoordinator), "SetTitle")]
    private bool PrefixFlowCoordinatorSetTitle(ViewController newViewController, ViewController.AnimationType animationType)
    {
        if (newViewController is ChatViewController)
        {
            _lobbyFlowCoordinator.ShowBackButton(true);
            _lobbyFlowCoordinator.InvokeMethod<object, FlowCoordinator>("SetTitle", new object[]
            {
                "Multiplayer Chat",
                animationType
            });
            return false;
        }

        return true;
    }

    #endregion
}