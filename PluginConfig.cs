using System.Collections.Generic;
using IPA.Config.Stores.Attributes;

namespace BeatSaberMultiplayerChat;

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
}