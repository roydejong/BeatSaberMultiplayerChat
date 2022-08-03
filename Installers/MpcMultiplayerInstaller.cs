using BeatSaberMultiplayerChat.Core;
using Zenject;

namespace BeatSaberMultiplayerChat.Installers;

/// <summary>
/// Installer for AlwaysMultiPlayer (local active or inactive player) for multiplayer gameplay.
/// </summary>
public class MpcMultiplayerInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GameplayIntegrator>().AsSingle();
    }
}