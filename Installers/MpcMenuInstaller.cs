using BeatSaberMultiplayerChat.Audio;
using BeatSaberMultiplayerChat.Core;
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
        Container.BindInterfacesAndSelfTo<SoundNotifier>().FromNewComponentOnNewGameObject().AsSingle();
        
        Container.BindInterfacesAndSelfTo<LobbyIntegrator>().AsSingle();
    }
}