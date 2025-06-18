using System;
using HarmonyLib;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.System
{
    [HarmonyPatch(typeof(GameManager), "OnUpdate")]
    public static class GameManager_OnUpdate_CombinedPatch
    {
        private static bool _gameInitialized = false;
        private static float _lastUpdateTime = 0f;
        private static int _skipFrames = 0;
        private const float UPDATE_INTERVAL = 0.1f; // Only run heavy operations every 0.1 seconds
        private const int INITIALIZATION_SKIP_FRAMES = 120; // Skip first 2 seconds at 60fps

        static void Postfix()
        {
            try
            {
                // Skip operations during initial game loading
                if (_skipFrames < INITIALIZATION_SKIP_FRAMES)
                {
                    _skipFrames++;
                    return;
                }

                // Check if game is properly initialized
                if (!_gameInitialized)
                {
                    if (GameManager.Instance == null) return;

                    // More thorough initialization check
                    try
                    {
                        // Check if essential game systems are ready
                        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded)
                        {
                            _gameInitialized = true;
                            Plugin.Logger.LogInfo("Game initialization detected, enabling update patches");
                        }
                        else
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogWarning($"Error checking game initialization: {ex.Message}");
                        return;
                    }
                }

                // Throttle update frequency for heavy operations
                float currentTime = UnityEngine.Time.unscaledTime;
                if (currentTime - _lastUpdateTime < UPDATE_INTERVAL)
                {
                    // Still call focus loss patch as it's lightweight and important
                    PauseOnFocusLossPatch.Postfix();
                    return;
                }
                _lastUpdateTime = currentTime;

                // Call patch methods with individual error handling
                try
                {
                    PauseOnFocusLossPatch.Postfix();
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in PauseOnFocusLossPatch: {ex.Message}");
                }

                try
                {
                    UI.BgmCaptionPatch.Postfix();
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in BgmCaptionPatch: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Critical error in GameManager_OnUpdate_CombinedPatch: {ex}");
                // Reset initialization state on critical error
                _gameInitialized = false;
                _skipFrames = 0;
            }
        }

        // Method to reset update patch state
        public static void ResetUpdateState()
        {
            _gameInitialized = false;
            _skipFrames = 0;
            _lastUpdateTime = 0f;
            Plugin.Logger.LogInfo("Update patch state reset");
        }
    }
}