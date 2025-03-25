using System.Diagnostics;
using System.Reflection;

namespace MultiplayerChat.Network;

/// <summary>
/// Version information for the MultiplayerChat mod.
/// </summary>
public static class MpChatVersionInfo
{
    /// <summary>
    /// The MPC protocol version, indicating compatibility across versions.
    /// This will be incremented whenever there is a change to networked features.
    /// </summary>
    public const uint ProtocolVersion = 1;

    /// <summary>
    /// Gets the product version / display version for this version of MPC.
    /// </summary>
    public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    /// <summary>
    /// Gets the product version / informational version for this version of MPC.
    /// </summary>
    public static string AssemblyProductVersion =>
        FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
}