using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.UI
{
    public static class AffectionEditor
    {
        public static bool IsVisible = false;
        private static GameObject _window;

        public static void Update()
        {
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        private static void Toggle()
        {
            IsVisible = !IsVisible;

            if (IsVisible)
            {
                if (_window == null)
                {
                    // Safe finding of canvas
                    var caption = UnityEngine.Object.FindObjectOfType<UICaptionController>();
                    if (caption != null)
                    {
                        var canvas = caption.GetComponentInParent<Canvas>();
                        if (canvas != null)
                        {
                            CreateEditorWindow(canvas.transform);
                        }
                    }
                }

                if (_window != null) _window.SetActive(true);
            }
            else
            {
                if (_window != null) _window.SetActive(false);
            }
        }

        private static void CreateEditorWindow(Transform parent)
        {
            _window = new GameObject("AffectionEditorWindow");
            _window.transform.SetParent(parent, false);

            var background = _window.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            var rect = _window.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800, 600);
            rect.anchoredPosition = Vector2.zero;

            // Find a font
            TMP_FontAsset font = null;
            var existingText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            if (existingText != null && existingText.Length > 0) font = existingText[0].font;

            // Title
            var titleObject = new GameObject("EditorTitle");
            titleObject.transform.SetParent(_window.transform, false);

            var titleText = titleObject.AddComponent<TextMeshProUGUI>();
            if (font != null) titleText.font = font;
            titleText.text = "Affection Editor";
            titleText.fontSize = 32;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(800, 50);
            titleRect.anchoredPosition = new Vector2(0, 250);
        }
    }
}