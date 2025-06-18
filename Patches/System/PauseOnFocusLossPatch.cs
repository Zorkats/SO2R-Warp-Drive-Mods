using System;
using HarmonyLib;
using UnityEngine;
using Game;
using Common;

namespace SO2R_Warp_Drive_Mods.Patches.System
{
    public static class PauseOnFocusLossPatch
    {
        private static bool _wasFocused = true;
        private static bool _wePaused = false;

        public static void Postfix()
        {
            try // Safety block to prevent any game crashes.
            {
                if (!Plugin.EnablePauseOnFocusLoss.Value) return;

                bool isFocused = Application.isFocused;
                if (isFocused == _wasFocused) return;

                if (!isFocused && !_wePaused)
                {
                    GameManager.OnChangePauseStatusCallback(PauseStatus.System);
                    // Added a null check for stability during scene changes
                    if (GameSoundManager.Instance != null)
                    {
                        GameSoundManager.Instance.PauseAllEnvSound();
                        GameSoundManager.PauseBgm(GameSoundManager.CurrentBgmID, true);
                    }
                    _wePaused = true;
                }
                else if (isFocused && _wePaused)
                {
                    GameManager.OnChangePauseStatusCallback(PauseStatus.None);
                    // Added a null check for stability
                    if (GameSoundManager.Instance != null)
                    {
                        GameSoundManager.Instance.ResumeAllEnvSound();
                        GameSoundManager.PauseBgm(GameSoundManager.CurrentBgmID, false);
                    }
                    _wePaused = false;
                }

                _wasFocused = isFocused;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Exception in PauseOnFocusLossPatch: {ex}");
            }
        }
    }
}