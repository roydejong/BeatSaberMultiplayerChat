using BeatSaberMultiplayerChat.Assets;
using BeatSaberMultiplayerChat.Installers;
using IPA;
using IPA.Config.Stores;
using SiraUtil.Web.SiraSync;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace BeatSaberMultiplayerChat
{
    [Plugin(RuntimeOptions.DynamicInit)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Plugin
    {
        internal static PluginConfig Config { get; private set; } = null!;
        
        [Init]
        public void Init(IPALogger logger, Zenjector zenjector, IPA.Config.Config config)
        {
            Config = config.Generated<PluginConfig>();
            
            zenjector.UseMetadataBinder<Plugin>();
            zenjector.UseLogger(logger);
            zenjector.UseSiraSync(SiraSyncServiceType.GitHub, "roydejong", "BeatSaberMultiplayerChat");
            
            zenjector.Install<MpcAppInstaller>(Location.App);
            zenjector.Install<MpcMenuInstaller>(Location.Menu);
            zenjector.Install<MpcMultiplayerInstaller>(Location.AlwaysMultiPlayer);
        }
        
        [OnEnable]
        public void OnEnable()
        {
            if (!Sprites.IsInitialized)
                Sprites.Initialize();
        }

        [OnDisable]
        public void OnDisable()
        {
        }
    }
}