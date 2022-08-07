using System;
using HMUI;
using MultiplayerChat.Assets;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MultiplayerChat.UI.Lobby;

public class ChatButton : MonoBehaviour
{
    public static ChatButton Create(DiContainer container)
    {
        var titleViewTransform =
            GameObject.Find("Wrapper/MenuCore/UI/ScreenSystem/TopScreen/TitleViewController").transform;
        var backButton = titleViewTransform.Find("BackButton");

        var cloneButton = Instantiate(backButton.gameObject, titleViewTransform);
        cloneButton.name = "ChatButton";
        cloneButton.transform.SetAsLastSibling();
        return container.InstantiateComponent<ChatButton>(cloneButton);
    }

    [Inject] private readonly DiContainer _diContainer = null!;
    [Inject] private readonly HoverHintController _hoverHintController = null!;

    public event EventHandler<EventArgs>? OnClick; 

    public void Awake()
    {
        // Add/set hover hint
        var hoverHint = GetComponent<HoverHint>();
        if (hoverHint == null)
            hoverHint = _diContainer.InstantiateComponent<HoverHint>(gameObject);
        hoverHint.text = "Multiplayer Chat";

        // Set icon
        transform.Find("Icon").GetComponent<ImageView>().sprite = Sprites.Chat;

        // Set position
        var rectTransform = (transform as RectTransform)!;
        
        var localPosition = rectTransform.localPosition;
        localPosition = new Vector3(68f, localPosition.y, localPosition.z);
        rectTransform.localPosition = localPosition;

        // Bind action
        GetComponent<Button>().onClick.AddListener(() => OnClick?.Invoke(this, EventArgs.Empty));
    }
}