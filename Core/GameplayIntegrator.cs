using System;
using MultiplayerChat.Audio;
using SiraUtil.Affinity;
using Zenject;

namespace MultiplayerChat.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class GameplayIntegrator : IInitializable, IDisposable, IAffinity
{
    [Inject] private readonly VoiceManager _voiceManager = null!;
    
    public void Initialize()
    {
        
    }

    public void Dispose()
    {
        
    }
}