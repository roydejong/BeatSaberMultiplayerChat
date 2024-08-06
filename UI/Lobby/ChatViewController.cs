using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Tags;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities;
using MultiplayerChat.Core;
using MultiplayerChat.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MultiplayerChat.UI.Lobby;

[HotReload]
public class ChatViewController : BSMLAutomaticViewController
{
    [Inject] private readonly ChatManager _chatManager = null!;

#pragma warning disable CS0649
    [UIComponent("ChatViewBg")] private Backgroundable? _chatViewBg;
    [UIComponent("MessagesContainer")] private BSMLScrollableContainer? _scrollableContainer;
    [UIComponent("ChatMessageInput")] private StringSetting? _chatInput;
#pragma warning restore CS0649
    
    private readonly List<ChatMessage> _messageBuffer;
    private Transform? _scrollableContainerContent;
    private bool _bsmlReady;
    private bool _chatLockedToBottom;

    public ChatViewController()
    {
        _messageBuffer = new(MaxBufferSize);
        _scrollableContainerContent = null;
        _bsmlReady = false;
        _chatLockedToBottom = true;
    }

    #region Core events

    [UIAction("#post-parse")]
    private void HandlePostParse()
    {
        if (_scrollableContainer != null)
            _scrollableContainerContent = _scrollableContainer.transform.Find("Viewport/Content Wrapper");

        _bsmlReady = true;
        
        _scrollableContainer!.PageUpButton.onClick.AddListener(HandleScrollablePageUp);
        _scrollableContainer!.PageDownButton.onClick.AddListener(HandleScrollablePageDown);

        ApplyUiMutations();
        FillChat();
        
        _chatInput!.modalKeyboard.keyboard.EnterPressed += HandleKeyboardInput; 
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        
        if (!_bsmlReady || firstActivation)
            return;
        
        FillChat();
        ResetChatInputText();
    }
    
    private async void HandleKeyboardInput(string input)
    {
        await Task.Delay(1); // we need to run OnChange after BSML's own EnterPressed, and this, well, it works
        
        ResetChatInputText();
        
        input = input.Trim();

        if (string.IsNullOrWhiteSpace(input))
            return;
        
        _chatManager.SendTextChat(input);
    }

    public void Update()
    {
        if (_scrollableContainer != null && _chatLockedToBottom)
            // BSML is aggressive about scrolling up, so we have to be aggressive about scrolling down...
            _scrollableContainer.ScrollTo(float.MaxValue, false);
    }

    #endregion

    #region Core UI
    
    private void ApplyUiMutations()
    {
        if (_chatViewBg == null || _chatInput == null)
            return;   
        
        // Remove skew from main chat background
        var bgImage = _chatViewBg.GetComponent<ImageView>();
        bgImage.SetField("_skew", 0f);
        bgImage.__Refresh();
        
        // Make the keyboard input look nice
        // > Remove the label
        _chatInput.transform.Find("NameText")?.gameObject.SetActive(false);
        
        // > Stretch the input element to span the full width
        var valuePickerRect = (_chatInput.transform.Find("ValuePicker").transform as RectTransform)!;
        valuePickerRect.offsetMin = new Vector2(-105f, 0f);
        
        // > Make the background look nice
        var buttonLeftSide = valuePickerRect.Find("DecButton") as RectTransform;
        var valueText = valuePickerRect.Find("ValueText") as RectTransform;

        var leftSideWidth = 0.05f;

        buttonLeftSide!.anchorMin = new Vector2(0.0f, 0.0f);
        buttonLeftSide.anchorMax = new Vector2(leftSideWidth, 1.0f);
        buttonLeftSide.offsetMin = new Vector2(0.0f, 0.0f);
        buttonLeftSide.offsetMax = new Vector2(0.0f, 0.0f);
        buttonLeftSide.sizeDelta = new Vector2(0.0f, 0.0f);

        valueText!.anchorMin = new Vector2(0.0f, 0.0f);
        valueText.anchorMax = new Vector2(1.0f, 1.0f);
        valueText.offsetMin = new Vector2(0.0f, -0.33f);
        valueText.offsetMax = new Vector2(0.0f, 0.0f);
        valueText.sizeDelta = new Vector2(0.0f, 0.0f);
        
        // > Remove skew from backgrounds
        var bgLeft = buttonLeftSide.Find("BG").GetComponent<ImageView>(); 
        bgLeft.SetField("_skew", 0f);
        bgLeft.__Refresh();

		// > Remove ugly edit icon
		_chatInput.editButton.transform.Find("EditIcon")?.GetComponent<Image>().gameObject.SetActive(false);


		// > Make placeholder text look like placeholder text
		var valueTextMesh = valueText.GetComponent<CurvedTextMeshPro>();
        valueTextMesh.alignment = TextAlignmentOptions.Center;
        valueTextMesh.color = Color.gray;
        
        ResetChatInputText();
    }

    private void ResetChatInputText()
    {
        _chatInput!.Text = ""; // keep internal value empty
        _chatInput.text.text = ChatInputPlaceholderText; // keep face value set to placeholder
    }

    #endregion

    #region Messages data/rendering
    
    private void FillChat()
    {
        ClearMessages(visualOnly: true);

        foreach (var message in _messageBuffer)
            AddMessage(message, visualOnly: true);

        _chatLockedToBottom = true;
    }
    
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

        var (textComponent, icon) = AddMessageObject();

        textComponent.Data = message.FormatMessage(extraIconSpacing: true);
        textComponent.RefreshText();

        icon.sprite = message.SpriteForMessage();

        _chatLockedToBottom = true; // this sucks but the alternative is BSML scrolling all the way to the top every msg 
    }

    private Tuple<FormattableText, ImageView> AddMessageObject()
    {
        var layoutTag = new HorizontalLayoutTag();
        var layoutGo = layoutTag.CreateObject(_scrollableContainerContent);

        var imageTag = new ImageTag();
        var imageGo = imageTag.CreateObject(layoutGo.transform);
        var imageRect = imageGo.transform as RectTransform;
        imageRect.pivot = new Vector2(0, 1);
        imageRect.anchorMin = new Vector2(0, 1);
        imageRect.anchorMax = new Vector2(0, 1);
        imageRect.sizeDelta = new Vector2(5, 5);

        var layoutElement = imageGo.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        var imageView = imageGo.GetComponent<ImageView>();
        imageView.preserveAspect = true;
        imageView.SetField("_skew", 0f);
        imageView.__Refresh();

        var textTag = new TextTag();
        var textGo = textTag.CreateObject(layoutGo.transform);
        var textComponent = textGo.GetComponent<FormattableText>();
        textComponent.text = "";
        textComponent.fontSize = 3.4f;
        textComponent.richText = true;
        textComponent.enableWordWrapping = true;

        return new(textComponent, imageView);
    }

    #endregion

    #region Scroll pain

    private void HandleScrollablePageUp()
    {
        _chatLockedToBottom = false;
    }

    private void HandleScrollablePageDown()
    {
        if (_scrollableContainer == null)
            return;
        
        _chatLockedToBottom = !_scrollableContainer.PageDownButton.interactable;
    }

    #endregion

    private const int MaxBufferSize = 32;
    private const string ChatInputPlaceholderText = "Click here to type a message";
}