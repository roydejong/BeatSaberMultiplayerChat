using MultiplayerChat.Core;
using Zenject;

namespace MultiplayerChat.Installers;

/// <summary>
/// Installer for AlwaysMultiPlayer (local active or inactive player) for multiplayer gameplay.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class MpcMultiplayerInstaller : Installer
{
    public override void InstallBindings()
    {
        // Core
        Container.BindInterfacesAndSelfTo<GameplayIntegrator>().AsSingle();
    }
}