using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Tags;
using BeatSaberMarkupLanguage.ViewControllers;
using MultiplayerChat.Models;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace MultiplayerChat.UI.Lobby;

[HotReload]
public class ChatViewController : BSMLAutomaticViewController
{
    [Inject] private readonly SiraLog _log = null!;

    [UIComponent("ChatViewRoot")] private Backgroundable _chatViewRoot = null!;
    [UIComponent("MessagesContainer")] private BSMLScrollableContainer? _scrollableContainer;

    private readonly List<ChatMessage> _messageBuffer;
    private Transform? _scrollableContainerContent;
    private bool _bsmlReady;

    public ChatViewController() : base()
    {
        _messageBuffer = new(MaxBufferSize);
        _scrollableContainerContent = null;
        _bsmlReady = false;
    }

    [UIAction("#post-parse")]
    private void HandlePostParse()
    {
        if (_scrollableContainer != null)
            _scrollableContainerContent = _scrollableContainer.transform.Find("Viewport/Content Wrapper");

        _bsmlReady = true;

        FillChat();
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        
        if (!_bsmlReady || firstActivation)
            return;
        
        FillChat();
    }

    private void FillChat()
    {
        ClearMessages(visualOnly: true);

        foreach (var message in _messageBuffer)
            AddMessage(message, visualOnly: true);

        ScrollDown();
    }

    #region Chat messages

    public void ClearMessages(bool visualOnly = false)
    {
        if (!visualOnly)
            _messageBuffer.Clear();

        var scrollableContent = _scrollableContainerContent;

        if (scrollableContent is null)
            return;

        var childCount = scrollableContent.transform.childCount;

        for (var i = childCount - 1; i >= 0; i--)
            DestroyImmediate(scrollableContent.transform.GetChild(i).gameObject);

        _scrollableContainer!.RefreshContent();
    }

    public void AddMessage(ChatMessage message, bool visualOnly = false)
    {
        if (!visualOnly)
        {
            if (_messageBuffer.Count >= MaxBufferSize)
                _messageBuffer.RemoveAt(0);
            _messageBuffer.Add(message);
        }

        if (!gameObject.activeInHierarchy || !_bsmlReady)
            // Only buffer until we're activated
            return;

        var scrollableContent = _scrollableContainerContent;

        if (scrollableContent is null)
            return;

        var layoutTag = new HorizontalLayoutTag();
        var layoutGo = layoutTag.CreateObject(scrollableContent.transform);

        var textTag = new TextTag();
        var textGo = textTag.CreateObject(layoutGo.transform);
        var textComponent = textGo.GetComponent<FormattableText>();
        textComponent.Data = message.FormatMessage();
        textComponent.RefreshText();
        textComponent.fontSize = 3.4f;
        textComponent.richText = true;

        ScrollDown();
    }

    public void ScrollDown()
    {
        if (_scrollableContainer is null)
            return;

        _scrollableContainer.ScrollDown(true);
    }

    #endregion

    private const int MaxBufferSize = 32;
}