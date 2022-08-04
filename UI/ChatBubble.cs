using System;
using System.Collections;
using HMUI;
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace BeatSaberMultiplayerChat.UI;

public class ChatBubble : MonoBehaviour
{
    public static ChatBubble Create(DiContainer container, Transform parent)
    {
        // Hijack hover hint panel prefab as our base
        var hhc = container.Resolve<HoverHintController>();
        var hhpPrefab = hhc.GetField<HoverHintPanel, HoverHintController>("_hoverHintPanelPrefab")!;

        var instance = Instantiate(hhpPrefab, parent: parent, worldPositionStays: false);
        instance.transform.SetAsLastSibling();
        instance.name = "MpcChatBubble";

        Destroy(instance.GetComponent<HoverHintPanel>());

        return container.InstantiateComponent<ChatBubble>(instance.gameObject);
    }

    private InnerState _state = InnerState.Idle;
    public bool IsShowing => _state != InnerState.Idle;

    private RectTransform? _rectTransform;
    private CanvasGroup? _canvasGroup;
    private ImageView? _bg;
    private CurvedTextMeshPro? _textMesh;
    private Vector3? _localPosTarget;

    private event EventHandler<EventArgs>? WasHiddenEvent;

    private static readonly Vector2 Padding = new(8f, 4f);
    private const float YOffset = 60f;
    private const float ZOffset = -0.1f;

    public void Awake()
    {
        _rectTransform = (RectTransform) transform;
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        _canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        _bg = GetComponent<ImageView>();
        _bg.color = new Color(0f, 0f, 0f, .95f);

        _textMesh = transform.Find("Text").GetComponent<CurvedTextMeshPro>();
        _textMesh.text = "!ChatBubble!";
        _textMesh.color = Color.white;
        _textMesh.fontSize = 4.8f;
        _textMesh.richText = true;

        var textMeshRect = (_textMesh.transform as RectTransform)!;
        textMeshRect.offsetMin = new Vector2(-60f, 0f);
        textMeshRect.offsetMax = new Vector2(60f, 0f);

        _state = InnerState.Idle;

        RefreshSize();
        HideImmediate();
    }

    #region Show/hide

    public void Show(string text)
    {
        if (IsShowing)
            throw new InvalidOperationException("Cannot call Show() while IsShowing is true");

        if (_rectTransform is null || _canvasGroup is null || _textMesh is null)
            return;

        _rectTransform.localScale = Vector3.one;
        _rectTransform.localRotation = Quaternion.identity;

        _canvasGroup.alpha = 0f;

        gameObject.SetActive(true);

        _textMesh.text = text;

        RefreshSize();

        StopAllCoroutines();
        StartCoroutine(nameof(AnimateInRoutine));
    }

    public void RefreshSize()
    {
        if (_rectTransform is null || _textMesh is null)
            return;

        _textMesh.ForceMeshUpdate();

        var vector = _textMesh.bounds.size + new Vector3(Padding.x, Padding.y, 0);
        _rectTransform.sizeDelta = vector;

        var localPosition = _rectTransform.localPosition;
        localPosition.z = ZOffset;
        localPosition.y = YOffset;
        _rectTransform.localPosition = localPosition;
        _localPosTarget = localPosition;
    }

    public void HideAnimated()
    {
        if (_state is not InnerState.AnimateIn and not InnerState.ShowWait)
            return;

        StopAllCoroutines();
        StartCoroutine(nameof(AnimateOutRoutine));
    }

    public void HideImmediate()
    {
        if (!gameObject.activeSelf)
            return;

        StopAllCoroutines();

        gameObject.SetActive(false);

        _state = InnerState.Idle;

        WasHiddenEvent?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Animation/Coroutines

    private IEnumerator AnimateInRoutine()
    {
        if (_canvasGroup is null || _localPosTarget is null)
            yield break;

        _state = InnerState.AnimateIn;

        var runTime = 0f;

        while (runTime < AnimationDuration)
        {
            runTime += Time.deltaTime;
            var animationProgress = (runTime / AnimationDuration);

            _canvasGroup.alpha = 1.0f * animationProgress;

            var yOffset = AnimationOffsetY - (AnimationOffsetY * (runTime / AnimationDuration));
            transform.localPosition = new Vector3
            (
                _localPosTarget.Value.x,
                _localPosTarget.Value.y + yOffset,
                _localPosTarget.Value.z
            );

            yield return null;
        }

        _canvasGroup.alpha = 1f;
        transform.localPosition = _localPosTarget.Value;

        // Wait for some time then animate out
        _state = InnerState.ShowWait;

        yield return new WaitForSeconds(DisplayTime);
        
        HideAnimated();
    }

    private IEnumerator AnimateOutRoutine()
    {
        if (_canvasGroup is null || _localPosTarget is null)
            yield break;

        _state = InnerState.AnimateOut;

        var runTime = 0f;

        while (runTime < AnimationDuration)
        {
            runTime += Time.deltaTime;
            var animationProgress = (runTime / AnimationDuration);

            _canvasGroup.alpha = 1.0f - (1.0f * (animationProgress));

            var yOffset = (-AnimationOffsetY * animationProgress);
            transform.localPosition = new Vector3
            (
                _localPosTarget.Value.x,
                _localPosTarget.Value.y + yOffset,
                _localPosTarget.Value.z
            );

            yield return null;
        }

        _canvasGroup.alpha = 0f;

        // Animated out and no longer visible, end of presentation
        yield return new WaitForEndOfFrame();
        
        HideImmediate();
    }

    #endregion

    private enum InnerState
    {
        Idle,
        AnimateIn,
        ShowWait,
        AnimateOut
    }

    private const float AnimationDuration = .15f;
    private const float AnimationOffsetY = -3f;
    private const float DisplayTime = 5f;
}