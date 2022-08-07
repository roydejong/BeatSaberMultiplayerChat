using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMultiplayerChat.Assets;
using BeatSaberMultiplayerChat.Audio;
using HMUI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaberMultiplayerChat.UI.ModSettings;

public class ModSettingsController : IInitializable, IDisposable
{
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly MicrophoneManager _microphoneManager = null!;
    [Inject] private readonly SoundNotifier _soundNotifier = null!;
    
    [UIComponent("BtnTestMic")] private Button _btnTestMic = null!;
    [UIComponent("DropdownNotification")] private DropDownListSetting _dropdownNotification = null!;
    [UIComponent("DropdownMic")] private DropDownListSetting _dropdownMic = null!;
    [UIComponent("ImgTestMic")] private ImageView _imgTestMic = null!;

    public void Initialize()
    {
        BSMLSettings.instance.AddSettingsMenu(SettingsMenuName, 
            "BeatSaberMultiplayerChat.UI.ModSettings.ModSettings.bsml", this);
    }
    
    public void Dispose()
    {
        BSMLSettings.instance.RemoveSettingsMenu(this);
    }

    #region Actions

    [UIAction("#post-parse")]
    private void HandlePostParse()
    {
        _microphoneManager.StopCapture();
        
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
        _microphoneManager.StopCapture();
    }

    [UIAction("#cancel")]
    private void HandleCancel()
    {
        _microphoneManager.StopCapture();
    }

    [UIAction("BtnTestMicClick")]
    public void HandleBtnTestMicClick()
    {
        if (_microphoneManager.IsLoopbackTesting)
            _microphoneManager.StopLoopbackTest();
        else
            _microphoneManager.StartLoopbackTest();
        
        RefreshInteractables();
    }

    #endregion

    #region UI Shared

    private void RefreshInteractables()
    {
        // Text
        _dropdownNotification.interactable = EnableTextChat;
        
        // Voice
        _dropdownMic.interactable = EnableVoiceChat;
        if (_microphoneManager.IsLoopbackTesting)
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
                _microphoneManager.StopCapture();
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
            list.AddRange(availableDevices);
            return list;
        }
    }
    
    [UIValue("RecordingDevice")]
    public string RecordingDevice
    {
        get
        {
            var selectedDevice = _microphoneManager.SelectedDeviceName;
    
            if (selectedDevice is not null && MicrophoneOptions.Contains(selectedDevice))
                return selectedDevice;
    
            return "None";
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