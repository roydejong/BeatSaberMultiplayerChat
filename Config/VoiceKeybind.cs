namespace MultiplayerChat.Models;

public enum VoiceKeybind : byte
{
    /// <summary>
    /// Primary button (B on Index, X/A on Oculus, Sandwich on Vive, Y/B on Oculus OpenVR)
    /// </summary>
    PrimaryButton = 0,
    /// <summary>
    /// Secondary button (A on Index, Y/B on Oculus, X/A on Oculus OpenVR)
    /// </summary>
    SecondaryButton = 1,
    /// <summary>
    /// Grip/bumper button
    /// </summary>
    GripButton = 2,
    /// <summary>
    /// Thumbstick press, joystick press or touchpad click
    /// </summary>
    AxisButton = 3
}