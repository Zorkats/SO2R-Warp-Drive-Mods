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

                if (_ctrl == null)
                {
                    _ctrl = Object.FindObjectOfType<UICaptionController>();
                    if (_ctrl == null)
                    {
                        return; // Exit quietly if the controller isn't ready
                    }
                    Plugin.Logger.LogInfo("[BGMInfo] UICaptionController found and cached.");
                }

                var id = GameSoundManager.CurrentBgmID;
                if (id != _lastID)
                {
                    _lastID = id;

                    _ctrl.HideCaption(_msgRoot + "Title");
                    _ctrl.HideCaption(_msgRoot + "Details");
                    _shown = false;

                    if (Plugin.ShowOncePerSession.Value && _shownThisSession.Contains(id))
                    {
                        return;
                    }
                    _shownThisSession.Add(id);

                    var meta = BgmNameLoader.Get((int)id);
                    string title = meta?.title ?? GameSoundManager.GetBgmName(id, out _);

                    if (string.IsNullOrEmpty(title) || title.Contains("Unknown"))
                    {
                        return;
                    }
                    

                    Vector2 pos = Vector2.zero;
                    Vector2 posDetails = Vector2.zero;
                    float marginX = 95f;
                    float marginXBattle = 400f;
                    float marginY = 40f;
                    float marginXDetails = -1700f;
                    float marginXDetailsBattle = 400f;
                    float charPx = 16f;

                    var ostType = GameSoundManager.IsOriginalBgm() ? "Original" : "Remake";
                    string msgTitle = $"♪ [{ostType} OST] {title}";
                    float totalPx = msgTitle.Length * charPx;

                    if (Plugin.IsBattleActive)
                    {
                        float x = (-Screen.width / 2f) + marginXBattle;
                        float xDetails = (-Screen.width / 2f) + marginXDetailsBattle;
                        float y = (Screen.height / 2f) - marginY;
                        pos = new Vector2(x, y);
                        posDetails = new Vector2(xDetails, y);
                    }
                    else
                    {
                        float x = (Screen.width / 2f) - marginX - (totalPx / 2f);
                        float xDetails = (-Screen.width / 2f) - marginXDetails;
                        float y = (Screen.height / 2f) - marginY;
                        pos = new Vector2(x, y);
                        posDetails = new Vector2(xDetails, y);
                    }
                    
                    _ctrl.ShowCaption(msgTitle, pos, _msgRoot + "Title");

                    if (meta != null)
                    {
                        string composer = meta.composer ?? "";
                        int track = meta.track;
                        string details = $"<size=70%>Track {track:D2}, {composer}</size>";
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
    }
}