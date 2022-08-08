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

    private ImageView? _unreadBadge;

    public event EventHandler<EventArgs>? OnClick; 

    public void Awake()
    {
        // Add/set hover hint
        var hoverHint = GetComponent<HoverHint>();
        if (hoverHint == null)
            hoverHint = _diContainer.InstantiateComponent<HoverHint>(gameObject);
        hoverHint.text = "Multiplayer Chat";

        // Set icon
        var icon = transform.Find("Icon").GetComponent<ImageView>();
        icon.sprite = Sprites.Chat;

        // Set position
        var rectTransform = (transform as RectTransform)!;
        
        var localPosition = rectTransform.localPosition;
        localPosition = new Vector3(68f, localPosition.y, localPosition.z);
        rectTransform.localPosition = localPosition;

        // Bind action
        GetComponent<Button>().onClick.AddListener(() => OnClick?.Invoke(this, EventArgs.Empty));
        
        // Add Unread badge
        var unreadBase = Instantiate(icon).transform;
        unreadBase.name = "UnreadBadge";
        unreadBase.SetParent(transform, false);
        unreadBase.SetAsLastSibling();
        _unreadBadge = unreadBase.gameObject.GetComponent<ImageView>();
        _unreadBadge.sprite = Sprites.UnreadBadge;
        unreadBase.localPosition = new Vector3(-10f, 3.33f, 0f);
        HideUnread();
    }

    public void ShowUnread()
    {
        if (_unreadBadge == null)
            return;
        
        _unreadBadge.gameObject.SetActive(true);
    }
    
    public void HideUnread()
    {
        if (_unreadBadge == null)
            return;
        
        _unreadBadge.gameObject.SetActive(false);
    }
}