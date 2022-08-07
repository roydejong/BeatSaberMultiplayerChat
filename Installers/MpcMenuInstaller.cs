using MultiplayerChat.Core;
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
        // Core
        Container.BindInterfacesAndSelfTo<LobbyIntegrator>().AsSingle();

        // UI
        Container.BindInterfacesAndSelfTo<ModSettingsController>().AsSingle();
    }
}