using HMUI;
using MultiplayerChat.Assets;
using MultiplayerChat.Audio;
using MultiplayerChat.Config;
using MultiplayerChat.Core;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MultiplayerChat.UI.Hud;

public class HudVoiceIndicator : MonoBehaviour, IInitializable
{
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly MicrophoneManager _microphoneManager = null!;
    [Inject] private readonly IMultiplayerSessionManager _multiplayerSession = null!;
    [Inject] private readonly VoiceManager _voiceManager = null!;
    [Inject] private readonly InputManager _inputManager = null!;

    private DisplayState _currentDisplayState = DisplayState.Hidden;
    private DisplayState _targetDisplayState = DisplayState.Hidden;

    private Camera? _mainCamera;
    private CanvasGroup? _canvasGroup;
    private Image? _bgImage;

    public void Initialize()
    {
    }

    public void Awake()
    {
        gameObject.layer = 5; // UI

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.scaleFactor = 1f;
        canvas.referencePixelsPerUnit = 1;
        canvas.planeDistance = 100;

        gameObject.AddComponent<CanvasRenderer>();
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<CurvedCanvasSettings>();

        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        var bg = new GameObject("BG");
        bg.transform.SetParent(transform);
        bg.AddComponent<CanvasRenderer>();

        _bgImage = bg.AddComponent<Image>();
        _bgImage.sprite = Sprites.MicOn;
        _bgImage.color = MpcColors.Red;

        transform.localScale = new Vector3(.0005f, .0005f, .0005f);

        _currentDisplayState = DisplayState.Hidden;
        _targetDisplayState = DisplayState.Hidden;

        _mainCamera = Camera.main;
    }

    public void OnEnable()
    {
        _multiplayerSession.connectedEvent += RefreshStatus;
        _multiplayerSession.disconnectedEvent += RefreshStatus_Disconnected;

        _inputManager.ActivatedEvent += RefreshStatus;
        _inputManager.DeactivatedEvent += RefreshStatus;
        _inputManager.TestModeChangedEvent += RefreshStatus;

        _voiceManager.StartedTransmittingEvent += RefreshStatus;
        _voiceManager.StoppedTransmittingEvent += RefreshStatus;

        RefreshStatus();
    }

    public void OnDisable()
    {
        _multiplayerSession.connectedEvent -= RefreshStatus;
        _multiplayerSession.disconnectedEvent -= RefreshStatus_Disconnected;

        _inputManager.ActivatedEvent -= RefreshStatus;
        _inputManager.DeactivatedEvent -= RefreshStatus;
        _inputManager.TestModeChangedEvent -= RefreshStatus;

        _voiceManager.StartedTransmittingEvent -= RefreshStatus;
        _voiceManager.StoppedTransmittingEvent -= RefreshStatus;
    }

    #region State

    private void RefreshStatus()
    {
        var isPlayingOrTesting = _multiplayerSession.isConnected || _inputManager.TestMode;
        var canActivate = _voiceManager.CanTransmit || _inputManager.TestMode;

        if (!_config.EnableVoiceChat || !_config.EnableHud || !isPlayingOrTesting)
        {
            // Voice and/or HUD is completely disabled, or not in a MP session - hide completely
            _targetDisplayState = DisplayState.Hidden;
        }
        else if (!_microphoneManager.HaveSelectedDevice || !canActivate)
        {
            // No mic selected, or cannot transmit right now - show as locked
            _targetDisplayState = DisplayState.VisibleLocked;
        }
        else if (_voiceManager.IsTransmitting || _voiceManager.IsLoopbackTesting)
        {
            // Actively transmitting - show as active
            _targetDisplayState = DisplayState.VisibleActive;
        }
        else
        {
            // Not transmitting - show as muted
            _targetDisplayState = DisplayState.VisibleMuted;
        }
    }

    private void RefreshStatus_Disconnected(DisconnectedReason obj) => RefreshStatus();

    private enum DisplayState
    {
        Transitioning = -1,
        Hidden = 0,
        VisibleLocked = 1,
        VisibleMuted = 2,
        VisibleActive = 3
    }

    #endregion

    #region UI Update

    private float HudOffsetCamX => _config.HudOffsetCamX;
    private float HudOffsetCamY => _config.HudOffsetCamY;
    private float HudOffsetCamZ => _config.HudOffsetCamZ;
    
    private static readonly Quaternion BaseRotation = Quaternion.Euler(15f, -15f, 0f);
    
    private const float TransitionLerp = .1f;

    public void Update()
    {
        if (_inputManager.TestMode)
            // Test mode - settings open, keep refreshing status
            RefreshStatus();
        
        if (_currentDisplayState == DisplayState.Hidden && _targetDisplayState == DisplayState.Hidden)
            // Fully hidden, do nothing
            return;

        if (_mainCamera != null)
        {
            // Stick to main cam
            var selfTransform = transform;
            selfTransform.position = _mainCamera.ViewportToWorldPoint(new Vector3(HudOffsetCamX, HudOffsetCamY, HudOffsetCamZ));
            selfTransform.rotation = _mainCamera.transform.rotation * BaseRotation;
        }

        if (!_inputManager.TestMode && _currentDisplayState == _targetDisplayState)
            // No transition needed
            return;

        _currentDisplayState = DisplayState.Transitioning;

        var opacityOk = false;
        var colorOk = false;

        float targetOpacity;
        Color targetColor;
        Sprite? targetSprite;

        switch (_targetDisplayState)
        {
            case DisplayState.Hidden:
                targetOpacity = 0f;
                targetColor = MpcColors.Red;
                targetSprite = Sprites.MicOff;
                break;
            default:
            case DisplayState.VisibleActive:
                targetOpacity = _config.HudOpacity;
                targetColor = MpcColors.Green;
                targetSprite = Sprites.MicOn;
                break;
            case DisplayState.VisibleMuted:
            case DisplayState.VisibleLocked:
                targetOpacity = _config.HudOpacity * .5f;
                targetColor = MpcColors.Red;
                targetSprite = Sprites.MicOff;
                break;
        }

        if (_canvasGroup != null)
        {
            // Fade to target opacity
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetOpacity, TransitionLerp);
            if (Mathf.Approximately(_canvasGroup.alpha, targetOpacity))
                opacityOk = true;
        }

        if (_bgImage != null)
        {
            // Fade to target color
            _bgImage.color = Color.Lerp(_bgImage.color, targetColor, TransitionLerp);
            if (IsColorVeryCloseToColor(_bgImage.color, targetColor))
                colorOk = true;

            // Update sprite
            if (targetSprite != null && _bgImage.sprite.name != targetSprite.name)
            {
                _bgImage.sprite = targetSprite;
            }
        }

        if (opacityOk && colorOk)
        {
            // Transition complete
            _currentDisplayState = _targetDisplayState;
        }
    }

    #endregion

    #region Util

    public virtual bool IsColorVeryCloseToColor(Color color0, Color color1)
    {
        return Mathf.Abs(color0.r - color1.r) < 0.002f &&
               Mathf.Abs(color0.g - color1.g) < 0.002f &&
               Mathf.Abs(color0.b - color1.b) < 0.002f &&
               Mathf.Abs(color0.a - color1.a) < 0.002f;
    }

    #endregion
}