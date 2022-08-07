using System;
using SiraUtil.Affinity;
using Zenject;

namespace MultiplayerChat.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class GameplayIntegrator : IInitializable, IDisposable, IAffinity
{
    [Inject] private readonly ChatManager _chatManager = null!;
    
    public void Initialize()
    {
        
    }

    public void Dispose()
    {
        
    }
}