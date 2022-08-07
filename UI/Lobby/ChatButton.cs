using HMUI;
using UnityEngine;
using Zenject;

namespace MultiplayerChat.UI.Lobby;

public class ChatButton : MonoBehaviour
{
    public static ChatButton Create(DiContainer container)
    {
        var titleViewTransform = FindObjectOfType<TitleViewController>().transform;
        var backButton = titleViewTransform.Find("BackButton");

        var cloneButton = Instantiate(backButton.gameObject, titleViewTransform);
        cloneButton.name = "ChatButton";
        cloneButton.transform.SetAsLastSibling();
        return container.InstantiateComponent<ChatButton>(cloneButton);
    }
    
    [Inject] private readonly DiContainer _diContainer = null!;
    [Inject] private readonly HoverHintController _hoverHintController = null!;

    public void Awake()
    {
        // Add/set hover hint
        var hoverHint = GetComponent<HoverHint>();
        if (hoverHint == null)
            hoverHint = _diContainer.InstantiateComponent<HoverHint>(gameObject);
        hoverHint.text = "Multiplayer Chat";
    }
}