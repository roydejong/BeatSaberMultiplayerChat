namespace MultiplayerChat.Audio;

public static class AudioGain
{
    public static void Apply(float[] samples, float gain)
    {
        for (var i = 0; i < samples.Length; i++)
            samples[i] *= gain;
    }
}