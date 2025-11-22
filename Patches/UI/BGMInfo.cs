using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Game;
using Common;
using Object = UnityEngine.Object;

namespace SO2R_Warp_Drive_Mods.Patches.UI
{
    public static class BgmCaptionPatch
    {
        internal static UICaptionController _ctrl;
        const string _msgRoot = "QoLBgm";
        const float _duration = 7f;

        static readonly HashSet<BgmID> _shownThisSession = new();

        // Timer logic variables
        private static float _hideTime = 0f;
        private static bool _isShowing = false;

        // Hook 1: Standard PlayBgm
        [HarmonyPatch(typeof(GameSoundManager), nameof(GameSoundManager.PlayBgm), new Type[] { typeof(BgmID), typeof(bool), typeof(bool) })]
        [HarmonyPostfix]
        public static void PlayBgm_Postfix(BgmID bgmID)
        {
            ShowBgmInfo(bgmID);
        }

        // Hook 2: Overload with 'ref SoundParameter'
        [HarmonyPatch(typeof(GameSoundManager), nameof(GameSoundManager.PlayBgm),
            new Type[] { typeof(BgmID), typeof(SoundParameter), typeof(bool), typeof(bool) },
            new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPostfix]
        public static void PlayBgm_Overload_Postfix(BgmID bgmID)
        {
            ShowBgmInfo(bgmID);
        }

        private static void ShowBgmInfo(BgmID id)
        {
            try
            {
                if (!Plugin.EnableBgmInfo.Value) return;
                if (id == BgmID.INVALID) return;

                // Safety check for Controller
                if (_ctrl == null)
                {
                    _ctrl = Object.FindObjectOfType<UICaptionController>();
                    if (_ctrl == null) return;
                }

                // Once per session check
                if (Plugin.ShowOncePerSession.Value)
                {
                    if (_shownThisSession.Contains(id)) return;
                    _shownThisSession.Add(id);
                }

                // Get Metadata
                var meta = BgmNameLoader.Get((int)id);
                string title = meta?.title ?? GameSoundManager.GetBgmName(id, out _);

                if (string.IsNullOrEmpty(title) || title.Contains("Unknown")) return;

                // Prepare Text
                var ostType = GameSoundManager.IsOriginalBgm() ? "Original" : "Remake";
                string msgTitle = $"♪ [{ostType} OST] {title}";

                // Using a safe default position
                Vector2 pos = new Vector2(50, -50); // Top Left offset
                Vector2 posDetails = new Vector2(50, -85);

                // Display
                _ctrl.ShowCaption(msgTitle, pos, _msgRoot + "Title");

                if (meta != null)
                {
                    string composer = meta.composer ?? "";
                    int track = meta.track;
                    string details = $"<size=70%>Track {track:D2}, {composer}</size>";
                    _ctrl.ShowCaption(details, posDetails, _msgRoot + "Details");
                }

                // Set the timer
                _hideTime = Time.time + _duration;
                _isShowing = true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error showing BGM info: {ex}");
            }
        }

        // This method is called manually from LifeCyclePatch.OnGameUpdate_Postfix
        public static void Update()
        {
            if (_isShowing && Time.time >= _hideTime)
            {
                if (_ctrl != null)
                {
                    _ctrl.HideCaption(_msgRoot + "Title");
                    _ctrl.HideCaption(_msgRoot + "Details");
                }
                _isShowing = false;
            }
        }
    }
}