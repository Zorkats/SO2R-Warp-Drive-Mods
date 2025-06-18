using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using HarmonyLib;
using Game;
using Object = UnityEngine.Object;

namespace SO2R_Warp_Drive_Mods.Patches.UI
{
    internal static class BgmCaptionPatch
    {
        // Change from 'static' to 'internal static' so other patches can access and clear it.
        internal static UICaptionController _ctrl = null!;

        static BgmID _lastID;
        const string _msgRoot = "QoLBgm";
        const float _duration = 7f;
        const float _marginX = 150f;
        const float _marginY = 60f;
        const float _charPx = 16f;

        private static float _hideAt;
        private static bool _shown;
        static readonly HashSet<BgmID> _shownThisSession = new();

        public static void Postfix()
        {
            try
            {
                if (!Plugin.EnableBgmInfo.Value) return;

                if (_ctrl == null)
                {
                    _ctrl = Object.FindObjectOfType<UICaptionController>();
                    if (_ctrl == null) return;
                }

                var id = GameSoundManager.CurrentBgmID;
                if (id != _lastID)
                {
                    _lastID = id;

                    _ctrl.HideCaption(_msgRoot + "Title");
                    _ctrl.HideCaption(_msgRoot + "Details");
                    _shown = false;

                    if (Plugin.ShowOncePerSession.Value && _shownThisSession.Contains(id)) return;
                    _shownThisSession.Add(id);

                    var meta = BgmNameLoader.Get((int)id);
                    string title = meta?.title ?? GameSoundManager.GetBgmName(id, out _);

                    if (string.IsNullOrEmpty(title) || title.Contains("Unknown")) return;

                    // --- FINAL, CORRECT POSITIONING LOGIC ---
                    Vector2 pos;

                    TextAnchor alignment;

                    // Margins from the edge of the screen
                    float marginX = 400f;
                    float marginY = 40f;

                    if (Plugin.IsBattleActive)
                    {
                        // BATTLE: Calculate Top-Left anchoredPosition
                        float x = (-Screen.width / 2f) + marginX;
                        float y = (Screen.height / 2f) - marginY;
                        pos = new Vector2(x, y);
                        // For battle, we want the text to be left-aligned
                        alignment = TextAnchor.UpperLeft;
                    }
                    else
                    {
                        // FIELD: Calculate Top-Right anchoredPosition
                        float x = (Screen.width / 2f) - marginX;
                        float y = (Screen.height / 2f) - marginY;
                        pos = new Vector2(x, y);
                        // For field, we want the text to be right-aligned
                        alignment = TextAnchor.UpperRight;
                    }

                    // For this UI system, we must align the text itself for the position to be correct.
                    // We will need to get the actual Text component and change its alignment property.
                    // (This will be the next step after we confirm this positioning works).

                    var ostType = GameSoundManager.IsOriginalBgm() ? "Original" : "Remake";
                    string msgTitle = $"♪ [{ostType} OST] {title}";

                    _ctrl.ShowCaption(msgTitle, pos, _msgRoot + "Title");

                    if (meta != null)
                    {
                        string composer = meta.composer ?? "";
                        int track = meta.track;
                        string album = meta.album ?? "";
                        string details = $"<size=60%>Track {track:D2}, {composer}, {album}</size>";
                        _ctrl.ShowCaption(details, pos + new Vector2(0, -35), _msgRoot + "Details");
                    }

                    _hideAt = Time.time + _duration;
                    _shown = true;
                }

                if (_shown && Time.time >= _hideAt)
                {
                    if (_ctrl != null)
                    {
                        _ctrl.HideCaption(_msgRoot + "Title");
                        _ctrl.HideCaption(_msgRoot + "Details");
                    }
                    _shown = false;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Exception in BgmCaptionPatch: {ex}");
            }
        }
    }
}