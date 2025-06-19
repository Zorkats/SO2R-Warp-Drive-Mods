using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using System.Collections.Generic;
using SO2R_Warp_Drive_Mods.Patches.UI;
using UnityEngine;

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
        internal static ConfigEntry<float> AggroRangeMultiplier = null!;

        // Debug Settings
        internal static ConfigEntry<bool> EnableDebugMode = null!;
        internal static ConfigEntry<bool> DisableComplexPatches = null!;

        // Battle state tracking
        internal static bool IsBattleActive = false;

        // Harmony instance
        private static Harmony _harmonyInstance = null!;

        public override void Load()
        {
            Logger = Log;
            Logger.LogInfo("Starting SO2R Warp Drive Plugins...");

            try
            {
                SetupConfiguration();
                ApplySafePatches();
                Logger.LogInfo("SO2R Warp Drive Plugins loaded successfully!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Critical error during plugin load: {ex}");
                // Don't re-throw - let the plugin load in a reduced state
            }
        }

        private void SetupConfiguration()
        {
            Logger.LogInfo("Setting up configuration...");

            // Debug settings first
            EnableDebugMode = Config.Bind(
                "Debug",
                "Enable Debug Logging",
                false,
                "Enables detailed debug logging for troubleshooting."
            );

            // General settings
            EnablePauseOnFocusLoss = Config.Bind(
                "General",
                "Pause On Focus Loss",
                true,
                "Automatically pauses the game when the window loses focus."
            );

            // BGM Info - CHANGED: Enable by default, user can disable if needed
            EnableBgmInfo = Config.Bind(
                "BGM Info",
                "Enable",
                true, // Changed from false to true
                "Shows the current BGM track name and details on screen."
            );

            ShowOncePerSession = Config.Bind(
                "BGM Info",
                "Show Once Per Session",
                true,
                "If true, BGM info is shown only once per track per session."
            );

            // Gameplay
            EnableMovementMultiplier = Config.Bind(
                "Gameplay",
                "Enable Movement Speed Multiplier",
                true,
                "Enables a multiplier for player movement speed."
            );

            MovementSpeedMultiplier = Config.Bind(
                "Gameplay",
                "Movement Speed Multiplier",
                1.75f,
                "The multiplier for movement speed. 1.0 is normal, 2.0 is double."
            );



            EnableAggroRangeMultiplier = Config.Bind(
                "Gameplay",
                "Enable Aggro Range Multiplier",
                false, // Default to false for safety
                "Enables a multiplier for enemy detection range. (EXPERIMENTAL, MIGHT CAUSE CRASHES ON BOOT)"
            );

            AggroRangeMultiplier = Config.Bind(
                "Gameplay",
                "Aggro Range Multiplier",
                0.5f,
                "Multiplier for enemy detection range. 0.5 is half, 2.0 is double."
            );

            Logger.LogInfo("Configuration setup completed.");
        }

        private void ApplySafePatches()
        {
            try
            {
                Logger.LogInfo("Applying safe patches...");
                _harmonyInstance = new Harmony("com.zorkats.so2r_qol");

                int successCount = 0;
                int failCount = 0;


                // Apply all movement speed equalization patches if enabled
                if (EnableMovementMultiplier.Value)
                {
                    // Apply all three movement patches individually
                    if (TryApplyPatch(typeof(Patches.Gameplay.PlayerMoveSpeed_Patch), "Player Move Speed Patch"))
                        successCount++;
                    else
                        failCount++;

                    if (TryApplyPatch(typeof(Patches.Gameplay.FollowerMoveSpeed_Patch), "Follower Move Speed Patch"))
                        successCount++;
                    else
                        failCount++;

                    if (TryApplyPatch(typeof(Patches.Gameplay.UniversalWalkSpeed_Patch), "Universal Walk Speed Patch"))
                        successCount++;
                    else
                        failCount++;
                }

                if (TryApplyPatch(typeof(Patches.System.BattleManager_StartBattle_Patch), "Battle Start Patch"))
                    successCount++;
                else
                    failCount++;

                if (TryApplyPatch(typeof(Patches.System.BattleManager_FinishBattle_Patch), "Battle End Patch"))
                    successCount++;
                else
                    failCount++;

                if (EnablePauseOnFocusLoss.Value)
                {
                    if (TryApplyPatch(typeof(Patches.System.GameManager_OnUpdate_CombinedPatch), "Update Patch"))
                        successCount++;
                    else
                        failCount++;
                }

                    // BGM Info patch - FIXED: Only apply if enabled AND not disabled by complex patches
                if (EnableBgmInfo.Value)
                {
                    try
                    {
                            // Try to load the BGM database (this is safe even if file doesn't exist)
                        BgmNameLoader.Load();
                        Logger.LogInfo("BGM database loading attempted.");

                            // The BGM info functionality is actually handled by the GameManager_OnUpdate_CombinedPatch
                            // which calls BgmCaptionPatch.Postfix(), so we don't need a separate patch here
                        Logger.LogInfo("BGM Info functionality enabled via Update patch.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"BGM database failed to load: {ex.Message}");
                            // Don't fail the entire patch process for this
                    }
                }

                    // Proximity patches - most likely to cause issues
                if (EnableAggroRangeMultiplier.Value)
                {
                    Logger.LogWarning("Aggro range patches are experimental and may cause instability.");

                    if (TryApplyPatch(typeof(Patches.Gameplay.SafeProximityPatch), "Proximity Patch"))
                        successCount++;
                    else
                        failCount++;
                }

                Logger.LogInfo($"Patch application complete: {successCount} successful, {failCount} failed");

                if (failCount > 0)
                {
                    Logger.LogWarning("Some patches failed. The mod will run with reduced functionality.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error applying patches: {ex}");
            }
        }

        private bool TryApplyPatch(Type patchType, string patchName)
        {
            try
            {
                if (EnableDebugMode.Value)
                {
                    Logger.LogInfo($"Attempting to apply: {patchName}");
                }

                _harmonyInstance.PatchAll(patchType);

                if (EnableDebugMode.Value)
                {
                    Logger.LogInfo($"✓ {patchName} applied successfully");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"✗ {patchName} failed: {ex.Message}");

                if (EnableDebugMode.Value)
                {
                    Logger.LogError($"Full exception for {patchName}: {ex}");
                }

                return false;
            }
        }

        // Update method for runtime patches
        void Update()
        {
            try
            {
                if (EnableAggroRangeMultiplier.Value && !DisableComplexPatches.Value)
                {

                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in plugin update: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (_harmonyInstance != null)
                {
                    Logger.LogInfo("Cleaning up patches...");
                    _harmonyInstance.UnpatchSelf();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during cleanup: {ex}");
            }
        }
    }
}