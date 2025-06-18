using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;

namespace SO2R_Warp_Drive_Mods
{
    [BepInPlugin("com.zorkats.so2r_qol", "SO2R QoL Patches", "1.0.1")]
    public class Plugin : BasePlugin
    {
        // --- Static variables for easy access from patches ---
        internal static ManualLogSource Logger = null!;

        // General Settings
        internal static ConfigEntry<bool> EnablePauseOnFocusLoss = null!;

        // BGM Info Settings
        internal static ConfigEntry<bool> EnableBgmInfo = null!;
        internal static ConfigEntry<bool> ShowOncePerSession = null!;

        // Gameplay Settings
        internal static ConfigEntry<bool> EnableMovementMultiplier = null!;
        internal static ConfigEntry<float> MovementSpeedMultiplier = null!;
        internal static ConfigEntry<bool> EnableAggroRangeMultiplier = null!;
        internal static ConfigEntry<float>? AggroRangeMultiplier;

        // Debug Settings
        internal static ConfigEntry<bool> EnableDebugLogging = null!;

        // This will be our global flag, accessible from any patch.
        internal static bool IsBattleActive = false;

        public override void Load()
        {
            Logger = Log;

            try
            {
                Logger.LogInfo("Starting SO2R QoL Mod initialization...");

                // --- Configuration Setup ---
                SetupConfiguration();

                // --- Load external data ---
                LoadExternalData();

                // --- Apply Harmony patches ---
                ApplyHarmonyPatches();

                Logger.LogInfo("SO2R QoL - All-in-One loaded successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Critical error during mod initialization: {ex}");
                throw; // Re-throw to prevent mod from appearing to load successfully
            }
        }

        private void SetupConfiguration()
        {
            try
            {
                Logger.LogInfo("Binding configuration settings...");

                // Debug Settings (put first so other sections can use it)
                EnableDebugLogging = Config.Bind(
                    "0. Debug",
                    "Enable Debug Logging",
                    false,
                    "Enables verbose debug logging for troubleshooting. May impact performance."
                );

                // General Settings
                EnablePauseOnFocusLoss = Config.Bind(
                    "1. General",
                    "Pause On Focus Loss",
                    true,
                    "Automatically pauses the game when the window loses focus."
                );

                // BGM Info Settings
                EnableBgmInfo = Config.Bind(
                    "2. BGM Info",
                    "Enable",
                    true,
                    "Shows the current BGM track name and details on screen when a new song starts."
                );

                ShowOncePerSession = Config.Bind(
                    "2. BGM Info",
                    "Show Once Per Session",
                    true,
                    "If true, BGM info is shown only the first time a track plays per session. If false, it shows every time."
                );

                // Gameplay Settings
                EnableMovementMultiplier = Config.Bind(
                    "3. Gameplay",
                    "Enable Movement Speed Multiplier",
                    true,
                    "Enables a multiplier for player movement speed on the field."
                );

                MovementSpeedMultiplier = Config.Bind(
                    "3. Gameplay",
                    "Movement Speed Multiplier",
                    1.75f, // Default to 75% faster
                    "The multiplier for player movement speed. 1.0 is normal, 2.0 is double speed."
                );

                EnableAggroRangeMultiplier = Config.Bind(
                    "3. Gameplay",
                    "Enable Aggro Range Multiplier",
                    true,
                    "Enables a multiplier for enemy detection range."
                );

                AggroRangeMultiplier = Config.Bind(
                    "3. Gameplay",
                    "Aggro Range Multiplier",
                    3.0f, // Default multiplier
                    "Multiplier for the enemy detection range. 0.5 is half, 2.0 is double. 0 is effectively invisible."
                );

                Logger.LogInfo("Configuration binding completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error setting up configuration: {ex}");
                throw;
            }
        }

        private void LoadExternalData()
        {
            try
            {
                Logger.LogInfo("Loading external data files...");
                BgmNameLoader.Load();
                Logger.LogInfo("External data loading completed.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error loading external data (non-critical): {ex.Message}");
                // Don't throw here as this is non-critical
            }
        }

        private void ApplyHarmonyPatches()
        {
            try
            {
                Logger.LogInfo("Applying Harmony patches...");

                var harmony = new Harmony("com.zorkats.so2r_qol");

                // Apply patches with individual error handling
                int patchCount = 0;
                int failureCount = 0;

                try
                {
                    harmony.PatchAll();

                    // Count successful patches
                    var patchedMethods = harmony.GetPatchedMethods();
                    foreach (var method in patchedMethods)
                    {
                        patchCount++;
                        if (EnableDebugLogging.Value)
                        {
                            Logger.LogInfo($"Successfully patched: {method.DeclaringType?.Name}.{method.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error applying Harmony patches: {ex}");
                    failureCount++;
                }

                Logger.LogInfo($"Harmony patching completed. Success: {patchCount}, Failures: {failureCount}");

                if (failureCount > 0)
                {
                    Logger.LogWarning("Some patches failed to apply. Mod may not function correctly.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Critical error applying Harmony patches: {ex}");
                throw;
            }
        }

        // Method to safely log debug messages
        internal static void LogDebug(string message)
        {
            if (EnableDebugLogging?.Value == true)
            {
                Logger?.LogInfo($"[DEBUG] {message}");
            }
        }

        // Method to reset all patch states (useful for debugging)
        internal static void ResetAllPatchStates()
        {
            try
            {
                Logger.LogInfo("Resetting all patch states...");

                Patches.System.PauseOnFocusLossPatch.ResetPauseState();
                Patches.System.GameManager_OnUpdate_CombinedPatch.ResetUpdateState();
                Patches.Gameplay.ProximityPatch.ResetPatchState();

                Logger.LogInfo("All patch states reset successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error resetting patch states: {ex}");
            }
        }
    }
}