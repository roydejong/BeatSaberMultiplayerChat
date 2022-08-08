using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Settings;
using HMUI;
using MultiplayerChat.Assets;
using MultiplayerChat.Audio;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MultiplayerChat.UI.ModSettings;

public class ModSettingsController : IInitializable, IDisposable
{
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly VoiceManager _voiceManager = null!;
    [Inject] private readonly MicrophoneManager _microphoneManager = null!;
    [Inject] private readonly SoundNotifier _soundNotifier = null!;
    
    [UIComponent("BtnTestMic")] private Button _btnTestMic = null!;
    [UIComponent("DropdownNotification")] private DropDownListSetting _dropdownNotification = null!;
    [UIComponent("ToggleVoice")] private ToggleSetting _toggleVoice = null!;
    [UIComponent("DropdownMic")] private DropDownListSetting _dropdownMic = null!;
    [UIComponent("ImgTestMic")] private ImageView _imgTestMic = null!;

    public void Initialize()
    {
        BSMLSettings.instance.AddSettingsMenu(SettingsMenuName, 
            "MultiplayerChat.UI.ModSettings.ModSettings.bsml", this);
    }
    
    public void Dispose()
    {
        BSMLSettings.instance.RemoveSettingsMenu(this);
    }

    #region Actions

    [UIAction("#post-parse")]
    private void HandlePostParse()
    {
        _voiceManager.StopLoopbackTest();
        
        // Make dropdown bigger
        var trDropdownOuter = (RectTransform) _dropdownMic.transform;
        trDropdownOuter.sizeDelta = new Vector2(64f, 0f);
        var trDropdownText = (RectTransform) trDropdownOuter.Find("DropDownButton/Text");
        trDropdownText.anchorMin = new Vector2(0f, .5f);
        trDropdownText.anchorMax = new Vector2(1f, .5f);
        
        RefreshInteractables();
    }

    [UIAction("#apply")]
    private void HandleApply()
    {
        _voiceManager.StopLoopbackTest();
    }

    [UIAction("#cancel")]
    private void HandleCancel()
    {
        _voiceManager.StopLoopbackTest();
    }

    [UIAction("BtnTestMicClick")]
    public void HandleBtnTestMicClick()
    {
        if (_voiceManager.IsLoopbackTesting)
            _voiceManager.StopLoopbackTest();
        else
            _voiceManager.StartLoopbackTest();
        
        RefreshInteractables();
    }

    #endregion

    #region UI Shared

    private void RefreshInteractables()
    {
        // Text
        _dropdownNotification.interactable = EnableTextChat;
        
        // Voice
        _toggleVoice.interactable = !_voiceManager.IsLoopbackTesting;
        _dropdownMic.interactable = EnableVoiceChat && !_voiceManager.IsLoopbackTesting;
        if (_voiceManager.IsLoopbackTesting)
        {
            _btnTestMic.interactable = true;
            _btnTestMic.SetButtonText("<color=#ff3b3b>Testing mic</color>");
            _imgTestMic.sprite = Sprites.MicOn;
            _imgTestMic.color = Color.red;
        }
        else
        {
            _btnTestMic.interactable = EnableVoiceChat && RecordingDevice != "None";
            _btnTestMic.SetButtonText("<color=#ffffff>Test mic</color>");
            _imgTestMic.sprite = Sprites.MicOff;
            _imgTestMic.color = Color.white;
        }
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
            RefreshInteractables();
        }
    }

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
    
    [UIValue("SoundNotification")]
    public string SoundNotification
    {
        get => _config.SoundNotification ?? "None";
        set
        {
            if (value != "None" && EnableTextChat)
                _soundNotifier.LoadAndPlayPreview(value);
            _config.SoundNotification = value;
            RefreshInteractables();
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
            RefreshInteractables();
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
            RefreshInteractables();
        }
    }
    
    #endregion
    
    private const string SettingsMenuName = "Multiplayer Chat";
}