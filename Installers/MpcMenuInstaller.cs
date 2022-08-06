using BeatSaberMultiplayerChat.Core;
using BeatSaberMultiplayerChat.UI.ModSettings;
using Zenject;

namespace BeatSaberMultiplayerChat.Installers;

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