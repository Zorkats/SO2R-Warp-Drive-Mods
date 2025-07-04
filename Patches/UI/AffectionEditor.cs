using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // Required for using Keyboard.current

namespace SO2R_Warp_Drive_Mods.Patches.UI
{
    public static class AffectionEditor
    {
        public static bool IsVisible = false;
        private static GameObject _window;
        private static GameObject _textTemplate;

        public static void Update()
        {
            
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                IsVisible = !IsVisible;
                Plugin.Logger.LogInfo($"Affection Editor toggled. Visible: {IsVisible}");

                if (IsVisible && _window == null)
                {
                    CreateEditorWindow();
                }

                if (_window != null)
                {
                    _window.SetActive(IsVisible);
                }
            }
        }

        private static void CreateEditorWindow()
        {
            // Create a new GameObject from scratch since cloning is unreliable.
            _window = new GameObject("AffectionEditorWindow");
            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Plugin.Logger.LogError("Could not find a Canvas to attach the editor window to!");
                return;
            }
            _window.transform.SetParent(canvas.transform, false);

            var background = _window.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            var rect = _window.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800, 600);
            rect.anchoredPosition = Vector2.zero;

            // Add a title using a newly created TextMeshProUGUI component
            var titleObject = new GameObject("EditorTitle");
            titleObject.transform.SetParent(_window.transform, false);
            
            var titleText = titleObject.AddComponent<TextMeshProUGUI>();
            titleText.text = "Affection Editor";
            titleText.fontSize = 32;
            titleText.alignment = TextAlignmentOptions.Center;
            // Note: We are not setting a font, relying on TMP's default.
            // This may result in a basic, but functional, font rendering.
            titleText.color = Color.white;

            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(800, 50);
            titleRect.anchoredPosition = new Vector2(0, 250);
            
            Plugin.Logger.LogInfo("Affection Editor window created successfully from scratch.");
        }
    }
}