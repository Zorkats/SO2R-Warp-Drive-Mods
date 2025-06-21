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
        public static Plugin Instance { get; private set; }
        internal static ManualLogSource Logger = null!;
        internal static Harmony _harmonyInstance = null!;
        public static bool DelayedPatchesApplied = false;

        // All your ConfigEntry declarations remain here...
        internal static ConfigEntry<bool> EnablePauseOnFocusLoss, EnableBgmInfo, ShowOncePerSession, EnableMovementMultiplier, EnableAggroRangeMultiplier, EnableDebugMode, DisableComplexPatches;
        internal static ConfigEntry<float> MovementSpeedMultiplier, AggroRangeMultiplier;
        internal static bool IsBattleActive = false;
        
        public override void Load()
        {   
            Instance = this;
            Logger = base.Log;
            Logger.LogInfo("Starting SO2R Warp Drive Plugins...");

            SetupConfiguration();
            
            if (EnableBgmInfo.Value)
            {
                BgmNameLoader.Load();
                Logger.LogInfo("BGM database loading attempted immediately at startup.");
            }
            
            // --- PHASE 1: Apply only the essential update patch immediately. ---
            _harmonyInstance = new Harmony("com.zorkats.so2r_qol");
            Logger.LogInfo("Applying immediate patches...");
            if (!TryApplyPatch(typeof(Patches.System.GameManager_OnUpdate_CombinedPatch), "GameManager Update Patch"))
            {
                Logger.LogError("CRITICAL: Failed to apply GameManager_OnUpdate_CombinedPatch. Mod will not function.");
            }
            
            Logger.LogInfo("Configuration loaded. Other patches will be applied after a delay.");
        }

        // --- PHASE 2: This method will be called by our update patch after the delay. ---
        public void ApplyDelayedPatches()
        {
            try
            {
                Logger.LogInfo("Applying delayed patches...");
                int successCount = 0;
                int failCount = 0;

                if (EnableMovementMultiplier.Value)
                {
                    if (TryApplyPatch(typeof(Patches.Gameplay.PlayerMoveSpeed_Patch), "Player Move Speed Patch")) successCount++; else failCount++;
                    if (TryApplyPatch(typeof(Patches.Gameplay.FollowerMoveSpeed_Patch), "Follower Move Speed Patch")) successCount++; else failCount++;
                    if (TryApplyPatch(typeof(Patches.Gameplay.UniversalWalkSpeed_Patch), "Universal Walk Speed Patch")) successCount++; else failCount++;
                }

                if (TryApplyPatch(typeof(Patches.UI.FavorabilityGauge_AddNumber_Patch.CatchValuePatch), "Affection Value Catcher")) successCount++; else failCount++;
                
                if (TryApplyPatch(typeof(Patches.System.BattleManager_StartBattle_Patch), "Battle Start Patch")) successCount++; else failCount++;
                if (TryApplyPatch(typeof(Patches.System.BattleManager_FinishBattle_Patch), "Battle End Patch")) successCount++; else failCount++;

                if (EnableAggroRangeMultiplier.Value)
                {
                    if (TryApplyPatch(typeof(Patches.Gameplay.SafeProximityPatch), "Proximity Patch")) successCount++; else failCount++;
                }

                Logger.LogInfo($"Delayed patch application complete: {successCount} successful, {failCount} failed");
                if (failCount > 0) Logger.LogWarning("Some delayed patches failed.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error applying delayed patches: {ex}");
            }
        }

        private bool TryApplyPatch(Type patchType, string patchName)
        {
            try
            {
                if (EnableDebugMode.Value) Logger.LogInfo($"Attempting to apply: {patchName}");
                _harmonyInstance.PatchAll(patchType);
                if (EnableDebugMode.Value) Logger.LogInfo($"✓ {patchName} applied successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"✗ {patchName} failed: {ex.Message}");
                if (EnableDebugMode.Value) Logger.LogError($"Full exception for {patchName}: {ex}");
                return false;
            }
        }

        private void SetupConfiguration()
        {
            // Your SetupConfiguration method remains unchanged.
            EnableDebugMode = Config.Bind("Debug", "Enable Debug Logging", false, "Enables detailed debug logging for troubleshooting.");
            EnablePauseOnFocusLoss = Config.Bind("General", "Pause On Focus Loss", true, "Automatically pauses the game when the window loses focus.");
            EnableBgmInfo = Config.Bind("BGM Info", "Enable", true, "Shows the current BGM track name and details on screen.");
            ShowOncePerSession = Config.Bind("BGM Info", "Show Once Per Session", true, "If true, BGM info is shown only once per track per session.");
            EnableMovementMultiplier = Config.Bind("Gameplay", "Enable Movement Speed Multiplier", true, "Enables a multiplier for player movement speed.");
            MovementSpeedMultiplier = Config.Bind("Gameplay", "Movement Speed Multiplier", 1.75f, "The multiplier for movement speed. 1.0 is normal, 2.0 is double.");
            EnableAggroRangeMultiplier = Config.Bind("Gameplay", "Enable Aggro Range Multiplier", false, "Enables a multiplier for enemy detection range. (EXPERIMENTAL, MIGHT CAUSE CRASHES ON BOOT)");
            AggroRangeMultiplier = Config.Bind("Gameplay", "Aggro Range Multiplier", 0.5f, "Multiplier for enemy detection range. 0.5 is half, 2.0 is double.");
        }
    }
}