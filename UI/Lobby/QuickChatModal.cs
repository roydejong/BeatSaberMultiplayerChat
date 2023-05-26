using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using BeatSaberMarkupLanguage.Tags;
using BeatSaberMarkupLanguage.Components;
using System.Reflection;
using System.IO;
using TMPro;
using MultiplayerChat.Core;
using MultiplayerChat.Models;

namespace MultiplayerChat.UI.Lobby
{
    internal class QuickChatModal : MonoBehaviour, IInitializable, IDisposable
    {
        [Inject] private readonly ChatManager _chatManager;
        [Inject] private readonly QuickChatOptions _quickChatOptions;

        [UIComponent("Modal")] private ModalView? _modal;
        [UIComponent("BackButton")] private ClickableImage _backButton = null!;

        private TextMeshProUGUI[] _buttonTexts = null!;

        private const string RootMenu = "Root";
        private string _currentMenu = RootMenu;
        private bool InSubMenu => _currentMenu != RootMenu;

        private string[] ButtonTexts => _quickChatOptions.Options.TryGetValue(_currentMenu, out var texts) ? texts : Array.Empty<string>();

        public void Initialize() => BeatSaberMarkupLanguage.BSMLParser.instance.Parse(GetEmbeddedString("MultiplayerChat.UI.Lobby.QuickChatModal.bsml"), gameObject, this);

        public void Dispose()
        {
            if (_modal != null)
                Destroy(_modal);
        }

        #region Show/Hide API
        public void Show()
        {
            UpdateButtonsText();
            _modal!.Show(true, true);
        }

        public void Hide(bool immediately = false) => _modal?.Hide(!immediately);
        #endregion

        #region UI handling
        [UIAction("#post-parse")]
        void PostParse()
        {
            // add our event
            _modal!.blockerClickedEvent += HandleBlockerClicked;

            // change the background sprite to be an octagon, TODO: higher resolution sprite (192 x 192 not enough)
            var bg = _modal.GetComponentInChildren<ImageView>(true);
            bg.sprite = GetEmbeddedSprite("MultiplayerChat.Assets.BG_Mask.png");

            // create all the buttons & save the texts
            _buttonTexts = CreateButtonWheel(_modal.transform);

            // fixup back button
            _backButton.transform.localScale = Vector3.one * 0.5f;
            _backButton.DefaultColor = Color.white.ColorWithAlpha(0.2f);
            _backButton.HighlightColor = Color.white;
            
            // update buttons text
            UpdateButtonsText();
        }

        TextMeshProUGUI[] CreateButtonWheel(Transform parent)
        {
            // result
            TextMeshProUGUI[] texts = new TextMeshProUGUI[8];

            // used to create items
            var imageTag = new ClickableImageTag();
            var textTag = new TextTag();

            // base values
            var offsetVector = new Vector2(0, 27.5f);
            var center = new Vector2(0, 0);

            // load the sprite, TODO: find a better way of accessing the sprite
            Sprite radialButtonSprite = GetEmbeddedSprite("MultiplayerChat.Assets.RadialButton.png");

            for (int i = 0; i < 8; i++)
            {
                var btn = imageTag.CreateObject(parent).GetComponent<ClickableImage>();
                
                // positioning & rotation
                float unitAngle = 1f/16f + (float)i / 8f;
                btn.rectTransform.anchoredPosition = center + offsetVector.Rotate(unitAngle * 360.0f * Mathf.Deg2Rad);
                btn.rectTransform.localRotation = Quaternion.Euler(0, 0, unitAngle * 360);
                btn.rectTransform.localScale = Vector3.one * 1.25f;
                
                // change the look of the button
                btn.sprite = radialButtonSprite;
                btn.DefaultColor = Color.white.ColorWithAlpha(0.2f);
                btn.HighlightColor = Color.white;
                btn.preserveAspect = true;

                // subscribe event
                int idx = i; // C# why is it always capture by refence unless you do this T_T
                btn.OnClickEvent += (_) => IndexedButtonClicked(idx);
                
                // add button text
                var txt = textTag.CreateObject(btn.transform).GetComponent<TextMeshProUGUI>();
                texts[i] = txt;

                // position text
                txt.rectTransform.anchoredPosition = Vector2.zero;
                txt.rectTransform.anchorMin = Vector2.zero;
                txt.rectTransform.anchorMax = Vector2.one;
                txt.rectTransform.offsetMin = Vector2.zero;
                txt.rectTransform.offsetMax = Vector2.zero;
                txt.rectTransform.localScale = Vector3.one * 0.5f;

                // rotate text back so it's upright
                txt.rectTransform.localRotation = Quaternion.Euler(0, 0, -unitAngle * 360);

                // clear text & align
                txt.alignment = TextAlignmentOptions.Center;
                txt.text = "";
            }

            return texts;
        }

        private void UpdateButtonsText()
        {
            var arr = ButtonTexts;
            for (int i = 0; i < 8; i++)
            {
                _buttonTexts[i].text = i < arr.Length ? arr[i] : string.Empty;
                _buttonTexts[i].transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(_buttonTexts[i].text));
            }
        }
        #endregion
       
        #region UI events
        [UIAction("BackButtonClick")]
        private void HandleBackButtonClicked()
        {
            if (!InSubMenu) Hide();

            _currentMenu = RootMenu;
            UpdateButtonsText();
        }

        private void HandleBlockerClicked()
        {
            _currentMenu = RootMenu;
            HandleBackButtonClicked();
        }

        private void IndexedButtonClicked(int idx)
        {
            if (InSubMenu)
            {
                _chatManager.SendTextChat(ButtonTexts[idx]);
                Hide();
                _currentMenu = RootMenu;
            }
            else
            {
                _currentMenu = ButtonTexts[idx];
            }
                
            UpdateButtonsText();
            
        }
        #endregion

        #region Utils
        static private string GetEmbeddedString(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        static private Sprite GetEmbeddedSprite(string resourceName, float pixelsPerUnit = 100)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new BinaryReader(stream))
                return BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(reader.ReadBytes((int)stream.Length), pixelsPerUnit);
        }
        #endregion
    }
}
