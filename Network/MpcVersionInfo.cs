using System.Diagnostics;
using System.Reflection;

namespace MultiplayerChat.Network;

/// <summary>
/// Version information for the MultiplayerChat mod.
/// </summary>
public static class MpcVersionInfo
{
    /// <summary>
    /// The MPC protocol version, indicating compatibility across versions.
    /// This will be incremented whenever there is a change to networked features.
    /// </summary>
    public const uint ProtocolVersion = 1;
    
    /// <summary>
    /// Indicates whether this MPC build supports text chat (it does).
    /// </summary>
    public const bool SupportsTextChat = true;
    
    /// <summary>
    /// Indicates whether this MPC build supports voice chat (it doesn't - coming soon - maybe).
    /// </summary>
    public const bool SupportsVoiceChat = false;
    
    /// <summary>
    /// Gets the product version / display version for this version of MPC.
    /// </summary>
    public static string AssemblyProductVersion =>
        FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
}