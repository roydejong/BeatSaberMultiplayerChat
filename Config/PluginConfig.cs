﻿using System.Collections.Generic;
using IPA.Config.Stores.Attributes;
using MultiplayerChat.Models;

namespace MultiplayerChat.Config;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global

// ReSharper disable once ClassNeverInstantiated.Global
public class PluginConfig
{
    #region Text

    /// <summary>
    /// Controls whether text chat features are globally enabled or not.
    /// </summary>
    public bool EnableTextChat = true;

    /// <summary>
    /// Controls whether player chat bubbles are enabled.
    /// </summary>
    public bool EnablePlayerBubbles = true;
    
    /// <summary>
    /// Controls whether center screen chat bubbles are enabled.
    /// </summary>
    public bool EnableCenterBubbles = true;

    /// <summary>
    /// Notification sound to play on incoming text chat messages.
    /// Must refer to a file within "./UserData/MultiplayerChat/" directory.
    /// If set to null or invalid file, no notification sound will play. 
    /// </summary>
    public string? SoundNotification = "ClubPing2.ogg";

    /// <summary>
    /// Volume scale applied when playing the configured sound notification.
    /// Notification sounds are disabled if set to 0 or below.
    /// Currently affects mic activation/deactivation sound volume as well.
    /// </summary>
    public float SoundNotificationVolume = 0.8f;

    #endregion

    #region Voice

    /// <summary>
    /// Controls whether voice chat features are globally enabled or not.
    /// Affects both incoming and outgoing voice chat.
    /// </summary>
    public bool EnableVoiceChat = true;
    
    /// <summary>
    /// Selected recording device name for voice chat.
    /// If set to "None", outgoing voice chat is explicitly disabled.
    /// If set to null, default device will be used. 
    /// </summary>
    public string? MicrophoneDevice = null;

    /// <summary>
    /// Gain to apply to outgoing mic audio.
    /// </summary>
    public float MicrophoneGain = 2.0f;

    /// <summary>
    /// Controls the jitter buffer time in milliseconds. 
    /// Incoming voice fragments will be held until the buffer is filled to this duration before voice playback begins.
    /// This is typically set to 30 - 50ms. It should generally not exceed 200ms. 
    /// </summary>
    public int JitterBufferMs = 50;

    /// <summary>
    /// Controls spatial audio blending. Spatial audio is directional/3D and originates from avatar's heads.
    /// Value between 0.0 and 1.0 indicating the spatial blend, where 0 = Fully 2D, 1 = Fully 3D.
    /// Note: 3D will be significantly quieter due to distance scaling, probably *too* quiet for Beat Saber...
    /// </summary>
    public float SpatialBlend = .1f;

    /// <summary>
    /// How is outgoing voice chat activated?
    /// </summary>
    public VoiceActivationMode VoiceActivationMode = VoiceActivationMode.Hold;

    /// <summary>
    /// What button toggles or triggers voice chat?
    /// </summary>
    public VoiceKeybind VoiceKeybind = VoiceKeybind.PrimaryButton;
    
    /// <summary>
    /// What controller(s) does the voice keybind apply to?
    /// </summary>
    public VoiceKeybindController VoiceKeybindController = VoiceKeybindController.Either;

    /// <summary>
    /// When enabled, pressing "V" on the keyboard will also act like the voice keybind.
    /// Hold/toggle setting still applies.
    /// </summary>
    public bool DebugKeyboardMicActivation = true;
    
    #endregion

    #region HUD

    /// <summary>
    /// Controls whether the mic indicator HUD is enabled in multiplayer.
    /// </summary>
    public bool EnableHud = true;

    /// <summary>
    /// Controls maximum HUD opacity.
    /// </summary>
    public float HudOpacity = DefaultHudOpacity;

    /// <summary>
    /// Camera offset for the HUD: X.
    /// </summary>
    public float HudOffsetCamX = DefaultHudOffsetCamX;

    /// <summary>
    /// Camera offset for the HUD: Y.
    /// </summary>
    public float HudOffsetCamY = DefaultHudOffsetCamY;

    /// <summary>
    /// Camera offset for the HUD: Z.
    /// </summary>
    public float HudOffsetCamZ = DefaultHudOffsetCamZ;

    public const float DefaultHudOpacity = .75f;
    public const float DefaultHudOffsetCamX = .3f;
    public const float DefaultHudOffsetCamY = .4f;
    public const float DefaultHudOffsetCamZ = .5f;
    
    #endregion
    
    #region Shared
    
    /// <summary>
    /// List of User IDs that are muted. Can be toggled in the lobby player list.
    /// </summary>
    [UseConverter]
    public List<string>? MutedUserIds = new();

    #endregion
}