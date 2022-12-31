using MultiplayerChat.Core;
using MultiplayerChat.UI.Hud;
using MultiplayerChat.UI.Lobby;
using MultiplayerChat.UI.ModSettings;
using Zenject;

namespace MultiplayerChat.Installers;

/// <summary>
/// Installer for Menu (lobby).
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class MpcMenuInstaller : Installer
{
    public override void InstallBindings()
    {
        // UI - Menu
        Container.BindInterfacesAndSelfTo<ModSettingsMenuController>().AsSingle();
        Container.Bind<ModSettingsViewController>().FromNewComponentAsViewController().AsSingle();
        
        // UI - Lobby
        Container.Bind<ChatViewController>().FromNewComponentAsViewController().AsSingle();
        
        // Core
        Container.BindInterfacesAndSelfTo<LobbyIntegrator>().AsSingle();
        
        // HUD
        Container.BindInterfacesAndSelfTo<HudVoiceIndicator>().FromNewComponentOnNewGameObject().AsSingle();
    }
}