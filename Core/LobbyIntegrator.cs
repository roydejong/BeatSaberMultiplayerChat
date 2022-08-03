using System;
using HMUI;
using IPA.Utilities;
using SiraUtil.Affinity;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaberMultiplayerChat.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class LobbyIntegrator : IInitializable, IDisposable, IAffinity
{
    [Inject] private readonly DiContainer _diContainer = null!;
    [Inject] private readonly ChatManager _chatManager = null!;

    private Sprite? _nativeIconSpeakerSound;
    private Sprite? _nativeIconMuted;
    
    public void Initialize()
    {
    }

    public void Dispose()
    {
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(GameServerPlayerTableCell), nameof(GameServerPlayerTableCell.SetData))]
    public void PostfixPlayerCellSetData(IConnectedPlayer connectedPlayer, Button ____mutePlayerButton)
    {
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
        
        // Icon reference + color
        var muteButtonIcon = ____mutePlayerButton.transform.Find("Icon").GetComponent<ImageView>();
        muteButtonIcon.sprite = _nativeIconMuted;
        muteButtonIcon.color = Color.red;
        
        // Add hover hint
        var hoverHint = ____mutePlayerButton.GetComponent<HoverHint>();

        if (hoverHint == null)
            hoverHint = _diContainer.InstantiateComponent<HoverHint>(____mutePlayerButton.gameObject);
        
        hoverHint.text = "Coming soon!";
        
        // TODO Store reference per player
        // TODO Typing/voice activity
        // TODO Click/mute toggle logic
    }
}