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
        private static bool _isInitialized = false;
        private static float _lastFocusCheckTime = 0f;
        private const float FOCUS_CHECK_INTERVAL = 1f; // Check focus every 0.5 seconds instead of every frame

        public static void Postfix()
        {
            try
            {
                if (!Plugin.EnablePauseOnFocusLoss.Value) return;   
                
                // Throttle focus checking to reduce overhead
                float currentTime = Time.unscaledTime;
                if (currentTime - _lastFocusCheckTime < FOCUS_CHECK_INTERVAL)
                {
                    return;
                }
                _lastFocusCheckTime = currentTime;

                // Wait for game to be properly initialized
                if (!_isInitialized)
                {
                    if (GameManager.Instance == null) return;

                    try
                    {
                        // Check if essential systems are ready
                        if (GameSoundManager.Instance != null)
                        {
                            _isInitialized = true;
                            Plugin.Logger.LogInfo("PauseOnFocusLoss initialized");
                        }
                        else
                        {
                            return; // Wait for sound manager
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogWarning($"Error initializing PauseOnFocusLoss: {ex.Message}");
                        return;
                    }
                }

                bool isFocused = Application.isFocused;
                if (isFocused == _wasFocused) return;

                if (!isFocused && !_wePaused)
                {
                    try
                    {
                        // Pause game
                        GameManager.OnChangePauseStatusCallback(PauseStatus.System);

                        // Safely pause audio
                        if (GameSoundManager.Instance != null)
                        {
                            GameSoundManager.Instance.PauseAllEnvSound();

                            // Check if there's a current BGM before trying to pause it
                            if (GameSoundManager.CurrentBgmID != BgmID.INVALID)
                            {
                                GameSoundManager.PauseBgm(GameSoundManager.CurrentBgmID, true);
                            }
                        }

                        _wePaused = true;
                        Plugin.Logger.LogInfo("Game paused due to focus loss");
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"Error pausing game on focus loss: {ex.Message}");
                        _wePaused = false; // Reset state on error
                    }
                }
                else if (isFocused && _wePaused)
                {
                    try
                    {
                        // Resume game
                        GameManager.OnChangePauseStatusCallback(PauseStatus.None);

                        // Safely resume audio
                        if (GameSoundManager.Instance != null)
                        {
                            GameSoundManager.Instance.ResumeAllEnvSound();

                            // Check if there's a current BGM before trying to resume it
                            if (GameSoundManager.CurrentBgmID != BgmID.INVALID)
                            {
                                GameSoundManager.PauseBgm(GameSoundManager.CurrentBgmID, false);
                            }
                        }

                        _wePaused = false;
                        Plugin.Logger.LogInfo("Game resumed after focus regained");
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"Error resuming game on focus regain: {ex.Message}");
                        _wePaused = true; // Keep paused state on error for safety
                    }
                }

                _wasFocused = isFocused;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Critical exception in PauseOnFocusLossPatch: {ex}");
                // Reset state on critical error
                _isInitialized = false;
                _wePaused = false;
                _wasFocused = true;
            }
        }

        // Method to reset pause state
        public static void ResetPauseState()
        {
            _isInitialized = false;
            _wePaused = false;
            _wasFocused = true;
            _lastFocusCheckTime = 0f;
            Plugin.Logger.LogInfo("Pause patch state reset");
        }
    }
}