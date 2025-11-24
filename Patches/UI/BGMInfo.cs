using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Game;
using Common;
using TMPro;
using Object = UnityEngine.Object;

namespace SO2R_Warp_Drive_Mods.Patches.UI
{
    public static class BgmCaptionPatch
    {
        internal static UICaptionController _ctrl;
        const string _msgRoot = "QoLBgm";
        const float _duration = 7f;

        static readonly HashSet<BgmID> _shownThisSession = new();
        static BgmID _lastID = BgmID.INVALID;

        private static float _hideTime = 0f;
        private static bool _isShowing = false;

        private static float _pollTimer = 0f;
        private const float POLL_INTERVAL = 0.5f;

        // --- Hooks ---
        [HarmonyPatch(typeof(UICaptionController), "Awake")]
        [HarmonyPostfix]
        public static void UICaptionController_Awake(UICaptionController __instance)
        {
            _ctrl = __instance;
        }

        [HarmonyPatch(typeof(GameSoundManager), nameof(GameSoundManager.PlayBgm), new Type[] { typeof(BgmID), typeof(bool), typeof(bool) })]
        [HarmonyPostfix]
        public static void PlayBgm_Postfix(BgmID bgmID) => ShowBgmInfo(bgmID);

        [HarmonyPatch(typeof(GameSoundManager), nameof(GameSoundManager.PlayBgmForceRemake))]
        [HarmonyPostfix]
        public static void PlayBgmForceRemake_Postfix(BgmID bgmID) => ShowBgmInfo(bgmID);

        // --- Public Methods ---

        public static void ForceRefresh()
        {
            _lastID = BgmID.INVALID;
            ShowCurrentBgm();
        }

        public static void ShowCurrentBgm()
        {
            try
            {
                if (GameSoundManager.Instance == null) return;
                var id = GameSoundManager.CurrentBgmID;
                if (id != BgmID.INVALID) ShowBgmInfo(id);
            }
            catch {}
        }

        public static void Update()
        {
            // F8 Debug
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
                ShowCurrentBgm();

            if (!Plugin.EnableBgmInfo.Value) return;

            // Poll
            _pollTimer += Time.deltaTime;
            if (_pollTimer >= POLL_INTERVAL)
            {
                _pollTimer = 0f;
                try
                {
                    if (GameSoundManager.Instance != null)
                    {
                        var currentID = GameSoundManager.CurrentBgmID;
                        if (currentID != _lastID)
                        {
                            if (currentID != BgmID.INVALID) ShowBgmInfo(currentID);
                        }
                    }
                }
                catch {}
            }

            // Hide
            if (_isShowing && Time.time >= _hideTime)
            {
                if (_ctrl != null)
                {
                    _ctrl.HideCaption(_msgRoot + "Title");
                    _ctrl.HideCaption(_msgRoot + "Details");
                    _ctrl.HideCaption(_msgRoot + "Album");
                }
                _isShowing = false;
            }
        }

        private static void ShowBgmInfo(BgmID id)
        {
            try
            {
                _lastID = id;

                if (!Plugin.EnableBgmInfo.Value) return;
                if (id == BgmID.INVALID) return;

                if (_ctrl == null)
                {
                    _ctrl = Object.FindObjectOfType<UICaptionController>(true);
                    if (_ctrl == null) return;
                }

                if (Plugin.ShowOncePerSession.Value)
                {
                    if (_shownThisSession.Contains(id)) return;
                    _shownThisSession.Add(id);
                }

                var meta = BgmNameLoader.Get((int)id);
                string title = meta?.title ?? GameSoundManager.GetBgmName(id, out _);

                if (string.IsNullOrEmpty(title) || title.Contains("Unknown")) return;

                // --- Content ---
                var ostType = GameSoundManager.IsOriginalBgm() ? "Original" : "Remake";

                string titleStr = $"♪ [{ostType} OST] {title}";
                string detailsStr = "";
                if (meta != null) detailsStr = $"<size=75%>Track {meta.track:D2}, {meta.composer}</size>";
                string albumStr = "<size=65%>STAR OCEAN THE SECOND STORY R OST</size>";

                // --- Display ---
                _ctrl.ShowCaption(titleStr, Vector2.zero, _msgRoot + "Title");
                if (!string.IsNullOrEmpty(detailsStr)) _ctrl.ShowCaption(detailsStr, Vector2.zero, _msgRoot + "Details");
                _ctrl.ShowCaption(albumStr, Vector2.zero, _msgRoot + "Album");

                // --- Layout (Tuned for Top-Right Corner) ---

                float marginX = 25f;      // Closer to right edge (Was 75)
                float startY = 430f;      // Closer to top edge (Was -20)
                float lineSpacing = 35f;  // More space between lines (Was 28)

                // 1. Title
                AlignToTopRight(titleStr, marginX, startY);

                // 2. Details
                if (!string.IsNullOrEmpty(detailsStr))
                    AlignToTopRight(detailsStr, marginX, startY - lineSpacing);

                // 3. Album
                AlignToTopRight(albumStr, marginX, startY - (lineSpacing * 2));

                _hideTime = Time.time + _duration;
                _isShowing = true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error showing BGM info: {ex}");
            }
        }

        private static void AlignToTopRight(string content, float offsetX, float offsetY)
        {
            if (_ctrl == null) return;

            // FIX: Do not stop at the first match. Iterate ALL matches.
            // This fixes the bug where the Album text (which is identical between songs)
            // would get fixed for the fading-out object but the new one would spawn in the center.
            var texts = _ctrl.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach(var t in texts)
            {
                // Check if the text matches our content
                if (t.text == content)
                {
                    t.rectTransform.anchorMin = new Vector2(1, 1);
                    t.rectTransform.anchorMax = new Vector2(1, 1);
                    t.rectTransform.pivot = new Vector2(1, 1);
                    t.alignment = TextAlignmentOptions.TopRight;
                    t.rectTransform.anchoredPosition = new Vector2(-offsetX, offsetY);

                    t.gameObject.SetActive(true);
                    // Do NOT return here. Process duplicates too.
                }
            }
        }
    }
}