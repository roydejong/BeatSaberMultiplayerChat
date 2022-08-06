using System.Collections.Generic;
using IPA.Config.Stores.Attributes;

namespace BeatSaberMultiplayerChat;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global

// ReSharper disable once ClassNeverInstantiated.Global
public class PluginConfig
{
    /// <summary>
    /// If true, text chat functionality is enabled in the lobby.
    /// </summary>
    public bool EnableTextChat = true;

    /// <summary>
    /// List of User IDs that are muted. Can be toggled in the lobby player list.
    /// </summary>
    [UseConverter]
    public List<string>? MutedUserIds = new();

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
}