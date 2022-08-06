using BeatSaberMultiplayerChat.Core;
using Zenject;

namespace BeatSaberMultiplayerChat.Installers;

/// <summary>
/// Installer for App (global).
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class MpcAppInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<PluginConfig>().FromInstance(Plugin.Config).AsSingle();
        
        Container.BindInterfacesAndSelfTo<ChatManager>().AsSingle();
    }
}