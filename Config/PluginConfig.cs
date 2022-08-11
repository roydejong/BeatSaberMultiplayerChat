using System.Collections.Generic;
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
    public string? SoundNotification = "ComputerChirp.ogg";

    /// <summary>
    /// Volume scale applied when playing the configured sound notification.
    /// Notification sounds are disabled if set to 0 or below.
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
    /// How is outgoing voice chat activated?
    /// </summary>
    public VoiceActivationMode VoiceActivationMode = VoiceActivationMode.Hold;

    /// <summary>
    /// What button toggles or triggers voice chat?
    /// </summary>
    public VoiceKeybind VoiceKeybind = VoiceKeybind.Secondary;
    
    /// <summary>
    /// What controller(s) does the voice keybind apply to?
    /// </summary>
    public VoiceKeybindController VoiceKeybindController = VoiceKeybindController.Either;
    
    #endregion

    #region Shared
    
    /// <summary>
    /// List of User IDs that are muted. Can be toggled in the lobby player list.
    /// </summary>
    [UseConverter]
    public List<string>? MutedUserIds = new();

    #endregion
}