using MultiplayerChat.Audio;
using MultiplayerChat.Core;
using Zenject;

namespace MultiplayerChat.Installers;

/// <summary>
/// Installer for App (global).
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class MpcAppInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<PluginConfig>().FromInstance(Plugin.Config).AsSingle();
        
        Container.BindInterfacesAndSelfTo<MicrophoneManager>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesAndSelfTo<SoundNotifier>().FromNewComponentOnNewGameObject().AsSingle();
        
        Container.BindInterfacesAndSelfTo<ChatManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<VoiceManager>().AsSingle();
    }
}