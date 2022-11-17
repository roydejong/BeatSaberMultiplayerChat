using UnityOpus;

namespace MultiplayerChat.Audio.VoIP;

public static class OpusConstants
{
    public const NumChannels Channels = NumChannels.Mono;
    public static readonly SamplingFrequency Frequency = SamplingFrequency.Frequency_48000;
    public static readonly OpusSignal Signal = OpusSignal.Voice;
    
    /// <summary>
    /// The complexity setting for the Opus encoder.
    /// This is a value ranging from 1 to 10, where the default is 10.
    /// </summary>
    public const int Complexity = 10;
    
    /// <summary>
    /// The bits per second target for the Opus encoder.
    /// </summary>
    public const int Bitrate = 96000;
    
    /// <summary>
    /// The amount of samples that should be encoded per audio frame.
    /// </summary>
    public const int FrameSampleLength = 120;
    
    /// <summary>
    /// The maximum byte size of an audio frame (before encoding). 
    /// </summary>
    public const int FrameByteLength = FrameSampleLength * sizeof(float);
}