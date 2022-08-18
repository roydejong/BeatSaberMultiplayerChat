using BeatSaberMarkupLanguage;
using HMUI;
using MultiplayerChat.Core;
using Zenject;

namespace MultiplayerChat.UI.ModSettings;

public class ModSettingsFlowCoordinator : FlowCoordinator
{
    [Inject] private readonly ModSettingsViewController _viewController = null!;
    [Inject] private readonly InputManager _inputManager = null!;
    
    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        _inputManager.TestMode = true;
        _inputManager.gameObject.SetActive(true);
        
        if (!firstActivation)
            return;

        SetTitle("Multiplayer Chat");
        showBackButton = true;
        ProvideInitialViewControllers(_viewController);
    }

    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        
        _inputManager.TestMode = false;
        _inputManager.gameObject.SetActive(false);
    }

    protected override void BackButtonWasPressed(ViewController topViewController) =>
        BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
}