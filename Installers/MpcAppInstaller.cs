using BeatSaberMultiplayerChat.Core;
using Zenject;

namespace BeatSaberMultiplayerChat.Installers;

/// <summary>
/// Installer for App (global).
/// </summary>
public class MpcAppInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<PluginConfig>().FromInstance(Plugin.Config).AsSingle();
        
        Container.BindInterfacesAndSelfTo<ChatManager>().AsSingle();
    }
}