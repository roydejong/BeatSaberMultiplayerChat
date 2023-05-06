using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using MultiplayerChat.Assets;
using MultiplayerChat.Audio;
using MultiplayerChat.Config;
using MultiplayerChat.Core;
using MultiplayerChat.Models;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MultiplayerChat.UI.ModSettings;

[HotReload]
// ReSharper disable once ClassNeverInstantiated.Global
public class ModSettingsViewController : BSMLAutomaticViewController
{
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly VoiceManager _voiceManager = null!;
    [Inject] private readonly MicrophoneManager _microphoneManager = null!;
    [Inject] private readonly SoundNotifier _soundNotifier = null!;
    [Inject] private readonly InputManager _inputManager = null!;

    [UIComponent("BtnTestMic")] private Button _btnTestMic = null!;
    [UIComponent("DropdownNotification")] private DropDownListSetting _dropdownNotification = null!;
    [UIComponent("ToggleVoice")] private ToggleSetting _toggleVoice = null!;
    [UIComponent("DropdownMic")] private DropDownListSetting _dropdownMic = null!;
    [UIComponent("ImgTestMic")] private ImageView _imgTestMic = null!;
    [UIComponent("DropdownActivation")] private DropDownListSetting _dropdownActivation = null!;
    [UIComponent("DropdownKeybind")] private DropDownListSetting _dropdownKeybind = null!;
    [UIComponent("DropdownController")] private DropDownListSetting _dropdownController = null!;
    [UIComponent("ActivationText")] private CurvedTextMeshPro _activationText = null!;
    [UIComponent("TogglePlayerBubbles")] private ToggleSetting _togglePlayerBubbles = null!;
    [UIComponent("ToggleCenterBubbles")] private ToggleSetting _toggleCenterBubbles = null!;
    [UIComponent("ToggleHud")] private ToggleSetting _toggleHud = null!;
    [UIComponent("SliderHudOpacity")] private SliderSetting _sliderHudOpacity = null!;
    [UIComponent("SliderHudOffsetCamX")] private SliderSetting _sliderHudOffsetCamX = null!;
    [UIComponent("SliderHudOffsetCamY")] private SliderSetting _sliderHudOffsetCamY = null!;
    [UIComponent("SliderHudOffsetCamZ")] private SliderSetting _sliderHudOffsetCamZ = null!;
    [UIComponent("BtnResetHudOffset")] private Button _btnResetHudOffset = null!;

    private bool _bsmlReady = false;

    public override void __Activate(bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.__Activate(addedToHierarchy, screenSystemEnabling);

        RefreshUiState();

        if (addedToHierarchy)
        {
            _inputManager.ActivatedEvent += HandleInputActivate;
            _inputManager.DeactivatedEvent += HandleInputDeactivate;
        }
    }

    public override void __Deactivate(bool removedFromHierarchy, bool deactivateGameObject, bool screenSystemDisabling)
    {
        base.__Deactivate(removedFromHierarchy, deactivateGameObject, screenSystemDisabling);

        if (removedFromHierarchy)
        {
            _inputManager.ActivatedEvent -= HandleInputActivate;
            _inputManager.DeactivatedEvent -= HandleInputDeactivate;
        }
    }

    #region Actions

    [UIAction("#post-parse")]
    private void HandlePostParse()
    {
        _bsmlReady = true;

        _voiceManager.StopLoopbackTest();

        // Make dropdown bigger
        var trDropdownOuter = (RectTransform) _dropdownMic.transform;
        trDropdownOuter.sizeDelta = new Vector2(64f, 0f);
        var trDropdownText = (RectTransform) trDropdownOuter.Find("DropDownButton/Text");
        trDropdownText.anchorMin = new Vector2(0f, .5f);
        trDropdownText.anchorMax = new Vector2(1f, .5f);

        RefreshUiState();
    }

    [UIAction("#apply")]
    private void HandleApply()
    {
        _voiceManager.StopLoopbackTest();
        RefreshUiState();
    }

    [UIAction("#cancel")]
    private void HandleCancel()
    {
        _voiceManager.StopLoopbackTest();
        RefreshUiState();
    }

    [UIAction("BtnTestMicClick")]
    public void HandleBtnTestMicClick()
    {
        if (_voiceManager.IsLoopbackTesting)
            _voiceManager.StopLoopbackTest();
        else
            _voiceManager.StartLoopbackTest();

        RefreshUiState();
    }

    [UIAction("BtnResetHudOffsetClick")]
    public void HandleBtnResetHudOffsetClick()
    {
        EnableHud = true;
        
        HudOpacity = PluginConfig.DefaultHudOpacity;
        HudOffsetCamX = PluginConfig.DefaultHudOffsetCamX;
        HudOffsetCamY = PluginConfig.DefaultHudOffsetCamY;
        HudOffsetCamZ = PluginConfig.DefaultHudOffsetCamZ;
        
        RefreshUiState();
    }

    #endregion

    #region UI Shared

    public void RefreshUiState()
    {
        if (!_bsmlReady)
            return;

        // Text
        _dropdownNotification.interactable = EnableTextChat;
        _togglePlayerBubbles.interactable = EnableTextChat;
        _toggleCenterBubbles.interactable = EnableTextChat;

        // Voice
        _toggleVoice.interactable = !_voiceManager.IsLoopbackTesting;
        _dropdownMic.interactable = EnableVoiceChat && !_voiceManager.IsLoopbackTesting;
        _dropdownActivation.interactable = EnableVoiceChat && !_voiceManager.IsLoopbackTesting && _microphoneManager.HaveSelectedDevice;
        _dropdownKeybind.interactable = EnableVoiceChat && !_voiceManager.IsLoopbackTesting && _microphoneManager.HaveSelectedDevice;
        _dropdownController.interactable = EnableVoiceChat && !_voiceManager.IsLoopbackTesting && _microphoneManager.HaveSelectedDevice;

        if (_voiceManager.IsLoopbackTesting)
        {
            _btnTestMic.interactable = true;
            _btnTestMic.SetButtonText("<color=#ff3b3b>Testing mic</color>");
            _imgTestMic.sprite = Sprites.MicOn;
            _imgTestMic.color = MpcColors.Green;
        }
        else
        {
            _btnTestMic.interactable = EnableVoiceChat && _microphoneManager.HaveSelectedDevice;
            _btnTestMic.SetButtonText("<color=#ffffff>Test mic</color>");
            _imgTestMic.sprite = Sprites.MicOff;
            _imgTestMic.color = Color.gray;
        }

        // Activation text
        if (!_config.EnableVoiceChat)
            _activationText.text = "Voice chat is completely disabled. You won't be able to speak or hear others.";
        else if (!_microphoneManager.HaveSelectedDevice) 
            _activationText.text = "No microphone selected, voice activation is disabled. You'll still be able to hear others.";
        else if (_config.EnableVoiceChat)
            _activationText.text = "While the settings are open, you can test your keybind to control the mic test" +
                                   $"\r\n<color=#3498db>{_inputManager.DescribeKeybindConfig()}</color>";
        
        // HUD
        _toggleHud.interactable = EnableVoiceChat;

        var canSetHudOptions = EnableVoiceChat && EnableHud; 
        
        _sliderHudOpacity.interactable = canSetHudOptions;
        _sliderHudOffsetCamX.interactable = canSetHudOptions;
        _sliderHudOffsetCamY.interactable = canSetHudOptions;
        _sliderHudOffsetCamZ.interactable = canSetHudOptions;
        _btnResetHudOffset.interactable = canSetHudOptions;
    }

    #endregion

    #region Settings/bindings

    [UIValue("EnableTextChat")]
    public bool EnableTextChat
    {
        get => _config.EnableTextChat;
        set
        {
            _config.EnableTextChat = value;
            RefreshUiState();
        }
    }

    [UIValue("SoundNotification")]
    public string SoundNotification
    {
        get => _config.SoundNotification ?? "None";
        set
        {
            if (value != "None" && EnableTextChat)
                _soundNotifier.LoadAndPlayPreview(value);
            _config.SoundNotification = value;
            RefreshUiState();
        }
    }

    [UIValue("EnablePlayerBubbles")]
    public bool EnablePlayerBubbles
    {
        get => _config.EnablePlayerBubbles;
        set
        {
            _config.EnablePlayerBubbles = value;
            RefreshUiState();
        }
    }

    [UIValue("EnableCenterBubbles")]
    public bool EnableCenterBubbles
    {
        get => _config.EnableCenterBubbles;
        set
        {
            _config.EnableCenterBubbles = value;
            RefreshUiState();
        }
    }

    [UIValue("EnableVoiceChat")]
    public bool EnableVoiceChat
    {
        get => _config.EnableVoiceChat;
        set
        {
            _config.EnableVoiceChat = value;
            if (!value)
                _voiceManager.StopLoopbackTest();
            RefreshUiState();
        }
    }

    [UIValue("RecordingDevice")]
    public string RecordingDevice
    {
        get
        {
            var selectedDevice = _microphoneManager.SelectedDeviceName ?? "Default";
            return MicrophoneOptions.Contains(selectedDevice) ? selectedDevice : "None";
        }
        set
        {
            _microphoneManager.TrySelectDevice(value);
            _config.MicrophoneDevice = value;
            RefreshUiState();
        }
    }

    [UIValue("VoiceActivationMode")]
    public string VoiceActivationMode
    {
        get => _config.VoiceActivationMode.ToString();
        set
        {
            _config.VoiceActivationMode = (VoiceActivationMode) Enum.Parse(typeof(VoiceActivationMode), value);
            RefreshUiState();
        }
    }

    [UIValue("VoiceKeybind")]
    public string VoiceKeybind
    {
        get => _config.VoiceKeybind.ToString();
        set
        {
            _config.VoiceKeybind = (VoiceKeybind) Enum.Parse(typeof(VoiceKeybind), value);
            RefreshUiState();
        }
    }

    [UIValue("VoiceKeybindController")]
    public string VoiceKeybindController
    {
        get => _config.VoiceKeybindController.ToString();
        set
        {
            _config.VoiceKeybindController =
                (VoiceKeybindController) Enum.Parse(typeof(VoiceKeybindController), value);
            RefreshUiState();
        }
    }

    [UIValue("EnableHud")]
    public bool EnableHud
    {
        get => _config.EnableHud;
        set
        {
            _config.EnableHud = value;
            RefreshUiState();
        }
    }

    [UIValue("HudOpacity")]
    public float HudOpacity
    {
        get => _config.HudOpacity;
        set
        {
            _config.HudOpacity = value;
            NotifyPropertyChanged();
            RefreshUiState();
        }
    }

    [UIValue("HudOffsetCamX")]
    public float HudOffsetCamX
    {
        get => _config.HudOffsetCamX;
        set
        {
            _config.HudOffsetCamX = value;
            NotifyPropertyChanged();
            RefreshUiState();
        }
    }

    [UIValue("HudOffsetCamY")]
    public float HudOffsetCamY
    {
        get => _config.HudOffsetCamY;
        set
        {
            _config.HudOffsetCamY = value;
            NotifyPropertyChanged();
            RefreshUiState();
        }
    }

    [UIValue("HudOffsetCamZ")]
    public float HudOffsetCamZ
    {
        get => _config.HudOffsetCamZ;
        set
        {
            _config.HudOffsetCamZ = value;
            NotifyPropertyChanged();
            RefreshUiState();
        }
    }

    #endregion

    #region Option lists

    [UIValue("SoundNotificationOptions")]
    public List<object> SoundNotificationOptions
    {
        get
        {
            var availableSounds = _soundNotifier.GetAvailableClipNames().ToArray();

            var list = new List<object>(availableSounds.Count() + 1);
            list.Add("None");
            list.AddRange(availableSounds);
            return list;
        }
    }

    [UIValue("MicrophoneOptions")]
    public List<object> MicrophoneOptions
    {
        get
        {
            var availableDevices = _microphoneManager.AvailableDeviceNames;

            var list = new List<object>(availableDevices.Count() + 1);
            list.Add("None");
            list.Add("Default");
            list.AddRange(availableDevices);
            return list;
        }
    }

    [UIValue("ActivationOptions")]
    public List<object> ActivationOptions =>
        Enum.GetNames(typeof(VoiceActivationMode)).ToList<object>();

    [UIValue("KeybindOptions")]
    public List<object> KeybindOptions =>
        Enum.GetNames(typeof(VoiceKeybind)).ToList<object>();

    [UIValue("ControllerOptions")]
    public List<object> ControllerOptions =>
        Enum.GetNames(typeof(VoiceKeybindController)).ToList<object>();

    #endregion

    #region Input events

    private void HandleInputActivate() => RefreshUiState();
    private void HandleInputDeactivate() => RefreshUiState();

    #endregion
}