using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using HarmonyLib;
using Game;
using Object = UnityEngine.Object;

namespace SO2R_Warp_Drive_Mods.Patches.UI
{
    public static class BgmCaptionPatch
    {
        // Change from 'static' to 'internal static' so other patches can access and clear it.
        internal static UICaptionController _ctrl = null!;

        static BgmID _lastID;
        const string _msgRoot = "QoLBgm";
        const float _duration = 7f;

        private static float _hideAt;
        private static bool _shown;
        static readonly HashSet<BgmID> _shownThisSession = new();

        public static void Postfix()
        {
            try
            {
                if (!Plugin.EnableBgmInfo.Value) return;
                if (GameManager.Instance == null) return;

                if (_ctrl == null)
                {
                    try
                    {
                        _ctrl = Object.FindObjectOfType<UICaptionController>();
                        if (_ctrl == null)
                        {
                            // Don't spam logs, just return quietly
                            return;
                        }
                        Plugin.Logger.LogInfo("UICaptionController found and cached");
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogWarning($"Could not find UICaptionController: {ex.Message}");
                        return;
                    }
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
                    Vector2 pos = Vector2.zero;
                    Vector2 posDetails = Vector2.zero;

                    // Margins from the edge of the screen
                    float marginX = 385f;
                    float marginY = 40f;
                    float marginXDetails = -1500f; // Details text has a larger margin
                    float marginXDetailsBattle = 400f;

                    if (Plugin.IsBattleActive)
                    {
                        // BATTLE: Calculate Top-Left anchoredPosition
                        float x = (-Screen.width / 2f) + marginX;
                        float xDetails = (-Screen.width / 2f) + marginXDetailsBattle;
                        float y = (Screen.height / 2f) - marginY;
                        pos = new Vector2(x, y);
                        posDetails = new Vector2(xDetails, y);
                    }

                    else if (Plugin.IsBattleActive == false)
                    {
                        // FIELD: Calculate Top-Right anchoredPosition
                        float x = (Screen.width / 2f) - marginX;
                        float xDetails = (-Screen.width / 2f) - marginXDetails;
                        float y = (Screen.height / 2f) - marginY;
                        pos = new Vector2(x, y);
                        posDetails = new Vector2(xDetails, y);
                    }

                    var ostType = GameSoundManager.IsOriginalBgm() ? "Original" : "Remake";
                    string msgTitle = $"♪ [{ostType} OST] {title}";

                    _ctrl.ShowCaption(msgTitle, pos, _msgRoot + "Title");

                    if (meta != null)
                    {
                        string composer = meta.composer ?? "";
                        int track = meta.track;
                        string album = meta.album ?? "";
                        string details = $"<size=60%>Track {track:D2}, {composer}, {album}</size>";
                        _ctrl.ShowCaption(details, posDetails + new Vector2(0, -35), _msgRoot + "Details");
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
                _ctrl = null;
            }
        }
        private static void SetTextAlignment(string messageID, TextAnchor alignment)
        {
            var selectorField = typeof(UICaptionController)
                .GetField("captionSelector", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance);
            if (selectorField == null) return;
            var selector = selectorField.GetValue(_ctrl);
            if (selector == null) return;

            var dictField = selector.GetType()
                .GetField("showCaptionDictionary",global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance);
            if (dictField == null) return;
            var dict = dictField.GetValue(selector) as global::System.Collections.IDictionary;
            if (dict == null) return;

            if (!dict.Contains(messageID)) return;
            var presenter = dict[messageID] as Component;
            if (presenter == null) return;

            var txt = presenter.GetComponentInChildren<Text>();
            if (txt != null)
                txt.alignment = alignment;
        }
    }
}