using MultiplayerChat.Core;
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
        // UI
        Container.Bind<ChatViewController>().FromNewComponentAsViewController().AsSingle();
        Container.BindInterfacesAndSelfTo<ModSettingsController>().AsSingle();
        
        // Core
        Container.BindInterfacesAndSelfTo<LobbyIntegrator>().AsSingle();
    }
}