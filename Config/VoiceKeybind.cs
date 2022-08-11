namespace MultiplayerChat.Models;

public enum VoiceKeybind : byte
{
    /// <summary>
    /// Primary button (B on Index, X/A on Oculus, Sandwich on Vive, Y/B on Oculus OpenVR)
    /// </summary>
    Primary = 0,
    /// <summary>
    /// Secondary button (A on Index, Y/B on Oculus, X/A on Oculus OpenVR)
    /// </summary>
    Secondary = 1,
    /// <summary>
    /// Grip/bumper button
    /// </summary>
    Grip = 2,
    /// <summary>
    /// Thumbstick press, joystick press or touchpad click
    /// </summary>
    StickPress = 3
}