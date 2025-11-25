using HarmonyLib;
using Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using SO2R_Warp_Drive_Mods.Patches.UI;
using SO2R_Warp_Drive_Mods.Patches.Gameplay;
using SO2R_Warp_Drive_Mods.Patches.Debug;
using System;
using Common;

namespace SO2R_Warp_Drive_Mods.Patches.System
{
    public static class LifeCyclePatch
    {
        private static string _lastSceneName = "";
        private static bool _hasInitialized = false;
        private static bool _isEngineReady = false;

        public static bool IsPausedByFocus { get; private set; } = false;

        [HarmonyPatch(typeof(GameManager), "OnInitialize")]
        [HarmonyPostfix]
        public static void OnGameInitialize_Postfix()
        {
            Plugin.Logger.LogInfo("[LifeCycle] Boot.");
            Application.runInBackground = true;
        }

        private static void OnSceneLoaded(Scene scene)
        {
            try
            {
                string sceneName = scene.name;
                if (sceneName == "BootScene" || sceneName == "Entry")
                {
                    _isEngineReady = false;
                    return;
                }

                _isEngineReady = true;

                // Reset battle-related state when leaving battle scenes
                if (!sceneName.Contains("Battle"))
                {
                    BattleRewardsPatch.ResetState();
                    LevelUpNoHealPatch.ClearCache();
                    EnemyStatBuffPatch.ClearCache();
                }

                if (!_hasInitialized)
                {
                    Plugin.Instance.ApplyGameplayPatches();
                    _hasInitialized = true;
                }

                RuntimeConfigManager.RefreshResources();
                if (Plugin.EnableBgmInfo.Value) BgmCaptionPatch.ForceRefresh();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[LifeCycle] OnSceneLoaded error: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(GameManager), "OnUpdate")]
        [HarmonyPostfix]
        public static void OnGameUpdate_Postfix()
        {
            try
            {
                // Scene change detection
                Scene currentScene = SceneManager.GetActiveScene();
                if (currentScene.IsValid() && currentScene.name != _lastSceneName)
                {
                    _lastSceneName = currentScene.name;
                    if (!string.IsNullOrEmpty(_lastSceneName)) OnSceneLoaded(currentScene);
                }
            }
            catch { }

            // BGM Info update
            if (Plugin.EnableBgmInfo.Value) BgmCaptionPatch.Update();

            // UI and Config updates
            if (_hasInitialized && _isEngineReady)
            {
                RuntimeConfigManager.Update();
                AffectionEditor.Update();
                Plugin.IsMenuOpen = RuntimeConfigManager.IsVisible || AffectionEditor.IsVisible;

                // No Heal on Level Up (polling-based)
                LevelUpNoHealPatch.Update();

                // Enemy stat buffing (polling for newly spawned enemies)
                EnemyStatBuffPatch.Update();

                // Debug logger
                if (Plugin.EnableDebugMode.Value) DeepStateLogger.Update();
            }

            // Focus loss handling
            if (Plugin.EnablePauseOnFocusLoss.Value) HandleFocusLoss();
        }

        [HarmonyPatch(typeof(GameInputManager), "OnLateUpdate")]
        [HarmonyPostfix]
        public static void OnInputLateUpdate_Postfix()
        {
            if (Plugin.IsMenuOpen || IsPausedByFocus)
            {
                if (Time.timeScale != 0f) Time.timeScale = 0f;
            }
        }

        private static bool _wasFocused = true;
        private static int _restoreVolume = 100;

        private static void HandleFocusLoss()
        {
            if (GameManager.Instance == null) return;

            bool isFocused = Application.isFocused;
            if (isFocused == _wasFocused) return;

            if (!isFocused)
            {
                IsPausedByFocus = true;
                Plugin.IsFocusLost = true;
                try
                {
                    GameManager.OnChangePauseStatusCallback(PauseStatus.System);
                    if (GameSoundManager.Instance != null)
                    {
                        GameSoundManager.Instance.PauseAllEnvSound();
                        GameSoundManager.ChangeMasterVolume(0);
                    }
                    AudioListener.pause = true;
                    Time.timeScale = 0f;
                    Application.runInBackground = false;
                }
                catch { }
            }
            else
            {
                IsPausedByFocus = false;
                Plugin.IsFocusLost = false;
                try
                {
                    Application.runInBackground = true;
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    if (GameSoundManager.Instance != null)
                    {
                        GameSoundManager.Instance.ResumeAllEnvSound();
                        GameSoundManager.ChangeMasterVolume(_restoreVolume);
                    }
                    GameManager.OnChangePauseStatusCallback(PauseStatus.None);
                }
                catch { }
            }
            _wasFocused = isFocused;
        }
    }
}