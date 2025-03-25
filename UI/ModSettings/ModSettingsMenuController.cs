using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;
using Object = UnityEngine.Object;

namespace MultiplayerChat.UI.ModSettings;

// ReSharper disable once ClassNeverInstantiated.Global
public class ModSettingsMenuController : IInitializable, IDisposable
{
    [Inject] private readonly DiContainer _container = null!;
    
    private MenuButton? _menuButton;
    private ModSettingsFlowCoordinator? _flowCoordinator;
    
    public void Initialize()
    {
        _menuButton = new MenuButton
        (
            text: "Multiplayer Chat",
            hoverHint: "Manage and test your Multiplayer Chat settings",
            HandleMenuButtonClick,
            interactable: true
        );
        
        MenuButtons.Instance.RegisterButton(_menuButton);
    }

    public void Dispose()
    {
        if (_menuButton != null)
        {
            if (MenuButtons.Instance != null)
                MenuButtons.Instance.UnregisterButton(_menuButton);
            _menuButton = null;
        }

        if (_flowCoordinator != null)
        {
            Object.Destroy(_flowCoordinator);
            _flowCoordinator = null;
        }
    }

    private void HandleMenuButtonClick()
    {
        if (_flowCoordinator == null)
        {
            _flowCoordinator = BeatSaberUI.CreateFlowCoordinator<ModSettingsFlowCoordinator>();
            _container.Inject(_flowCoordinator);
        }

        BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(_flowCoordinator);
    }
}