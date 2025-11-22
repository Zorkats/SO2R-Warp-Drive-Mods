using HarmonyLib;
using Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using SO2R_Warp_Drive_Mods.Patches.UI;
using System;
using Common;

namespace SO2R_Warp_Drive_Mods.Patches.System
{
    public static class LifeCyclePatch
    {
        // Track the scene name manually
        private static string _lastSceneName = "";
        private static bool _hasInitialized = false;

        // 1. Hook into GameManager.OnInitialize just to log that we are alive
        [HarmonyPatch(typeof(GameManager), "OnInitialize")]
        [HarmonyPostfix]
        public static void OnGameInitialize_Postfix()
        {
            Plugin.Logger.LogInfo("[LifeCycle] GameManager initialized (Boot). Waiting for Title Screen...");
            // DO NOT apply gameplay patches here. It is too early and causes crashes.
        }

        // 2. Called manually from our Update loop when the scene changes
        private static void OnSceneLoaded(Scene scene)
        {
            try
            {
                string sceneName = scene.name;
                Plugin.Logger.LogInfo($"[LifeCycle] Scene Loaded: {sceneName}");

                // IGNORE BootScene. Waiting for it to finish prevents the crash.
                if (sceneName == "BootScene" || sceneName == "Entry")
                {
                    return;
                }

                // Perform First-Time Initialization if we hit a real scene (like TitleScene)
                if (!_hasInitialized)
                {
                    Plugin.Logger.LogInfo("[LifeCycle] Valid scene loaded. Initializing Mod Systems...");
                    Plugin.Instance.ApplyGameplayPatches();
                    _hasInitialized = true;
                }

                // Check if we are in battle
                Plugin.IsBattleActive = (sceneName == "Battle");

                // Refresh UI resources (Find new Canvas/Font)
                RuntimeConfigManager.RefreshResources();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[LifeCycle] Error during Scene Loaded: {ex}");
            }
        }

        // 3. Global Update Hook
        [HarmonyPatch(typeof(GameManager), "OnUpdate")]
        [HarmonyPostfix]
        public static void OnGameUpdate_Postfix()
        {
            // --- Scene Polling Logic ---
            try
            {
                Scene currentScene = SceneManager.GetActiveScene();
                if (currentScene.IsValid() && currentScene.name != _lastSceneName)
                {
                    _lastSceneName = currentScene.name;
                    // Only trigger load logic if the scene name is valid
                    if (!string.IsNullOrEmpty(_lastSceneName))
                    {
                        OnSceneLoaded(currentScene);
                    }
                }
            }
            catch {}

            // Only run the rest if we have actually initialized
            if (!_hasInitialized) return;

            // Handle Input for Config
            if (Plugin.IsPatchesApplied)
            {
                RuntimeConfigManager.Update();
                AffectionEditor.Update();

                if (Plugin.EnableBgmInfo.Value)
                {
                    BgmCaptionPatch.Update();
                }
            }

            // Handle Pause on Focus Loss Logic
            if (Plugin.EnablePauseOnFocusLoss.Value)
            {
                HandleFocusLoss();
            }
        }

        private static bool _wasFocused = true;
        private static bool _wePaused = false;

        private static void HandleFocusLoss()
        {
            // Safety: Don't pause if game manager isn't ready
            if (GameManager.Instance == null) return;

            bool isFocused = Application.isFocused;
            if (isFocused == _wasFocused) return;

            if (!isFocused && !_wePaused)
            {
                try
                {
                    GameManager.OnChangePauseStatusCallback(PauseStatus.System);
                    if (GameSoundManager.Instance != null)
                    {
                        GameSoundManager.Instance.PauseAllEnvSound();
                        if (GameSoundManager.CurrentBgmID != BgmID.INVALID)
                            GameSoundManager.PauseBgm(GameSoundManager.CurrentBgmID, true);
                    }
                    _wePaused = true;
                    Plugin.Logger.LogInfo("Game paused (Focus Loss)");
                }
                catch {}
            }
            else if (isFocused && _wePaused)
            {
                try
                {
                    GameManager.OnChangePauseStatusCallback(PauseStatus.None);
                    if (GameSoundManager.Instance != null)
                    {
                        GameSoundManager.Instance.ResumeAllEnvSound();
                        if (GameSoundManager.CurrentBgmID != BgmID.INVALID)
                            GameSoundManager.PauseBgm(GameSoundManager.CurrentBgmID, false);
                    }
                    _wePaused = false;
                    Plugin.Logger.LogInfo("Game resumed (Focus Regained)");
                }
                catch {}
            }
            _wasFocused = isFocused;
        }
    }
}