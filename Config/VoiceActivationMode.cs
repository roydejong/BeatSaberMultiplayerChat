namespace MultiplayerChat.Models;

public enum VoiceActivationMode : byte
{
    /// <summary>
    /// Push to talk, keybind toggles mic on/off.
    /// </summary>
    Toggle = 0,
    /// <summary>
    /// Push to talk, hold keybind down to activate voice. 
    /// </summary>
    Hold = 1
}