using System.Collections.Generic;

namespace BeatSaberMultiplayerChat;

// ReSharper disable once ClassNeverInstantiated.Global
public class PluginConfig
{
    /// <summary>
    /// If true, text chat functionality is enabled in the lobby.
    /// </summary>
    public bool EnableTextChat = true;

    /// <summary>
    /// List of User IDs that are muted.
    /// </summary>
    public List<string>? MutedUserIds = new();
}