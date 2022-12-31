using MultiplayerChat.Core;
using MultiplayerChat.UI.Hud;
using Zenject;

namespace MultiplayerChat.Installers;

/// <summary>
/// Installer for AlwaysMultiPlayer (all multiplayer gameplay, spectator or not).
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class MpcMultiplayerInstaller : Installer
{
    public override void InstallBindings()
    {
        // Core
        Container.BindInterfacesAndSelfTo<GameplayIntegrator>().AsSingle();
        
        // HUD
        Container.BindInterfacesAndSelfTo<HudVoiceIndicator>().FromNewComponentOnNewGameObject().AsSingle();
    }
}