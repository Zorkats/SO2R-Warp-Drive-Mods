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
        internal static ConfigEntry<bool> EnablePauseOnFocusLoss, EnableBgmInfo, ShowOncePerSession, EnableMovementMultiplier, EnableAggroRangeMultiplier, EnableDebugMode, EnableNoHealOnLevelUp;
        internal static ConfigEntry<bool> EnableFormationBonusReset, EnableFormationBonusHalved, EnableFormationBonusHarder, EnableFormationBonusDisable;
        internal static ConfigEntry<bool> EnableChainBattleNerf, EnableChainBattleDisable;
        internal static ConfigEntry<bool> EnableMissionRewardNerf, NerfAllMissionRewards;
        internal static ConfigEntry<float> MovementSpeedMultiplier, AggroRangeMultiplier, FormationBonusPointMultiplier, ChainBattleBonusMultiplier, MissionRewardMultiplier;
        internal static ConfigEntry<int> HighValueMoneyThreshold, HighValueItemThreshold;
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
            if (EnableMovementMultiplier.Value)
            {
                TryApplyPatch(typeof(Patches.Gameplay.PlayerMoveSpeed_Patch), "Player Move Speed Patch");
                TryApplyPatch(typeof(Patches.Gameplay.FollowerMoveSpeed_Patch), "Follower Move Speed Patch");
                TryApplyPatch(typeof(Patches.Gameplay.UniversalWalkSpeed_Patch), "Universal Walk Speed Patch");
            }

            
            // Initialize runtime configuration manager
            RuntimeConfigManager.Initialize();
            
            Logger.LogInfo("Configuration loaded. Other patches will be applied after a delay.");
        }
        
        public override bool Unload()
        {
            try
            {
                // Cleanup runtime config manager
                RuntimeConfigManager.Cleanup();
                
                // Unpatch all Harmony patches
                _harmonyInstance?.UnpatchSelf();
                
                Logger.LogInfo("SO2R QoL Patches unloaded successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during unload: {ex}");
                return false;
            }
        }

        // --- PHASE 2: This method will be called by our update patch after the delay. ---
        public void ApplyDelayedPatches()
        {
            try
            {
                Logger.LogInfo("Applying delayed patches...");
                int successCount = 0;
                int failCount = 0;
                
                if (TryApplyPatch(typeof(Patches.System.MethodFinderPatch), "Method Finder Spy")) successCount++; else failCount++;
                
                if (TryApplyPatch(typeof(Patches.UI.FavorabilityGauge_AddNumber_Patch.CatchValuePatch), "Affection Value Catcher")) successCount++; else failCount++;
                
                // Add the new No Heal on Level Up patch
                if (EnableNoHealOnLevelUp.Value)
                {
                    if (TryApplyPatch(typeof(Patches.Gameplay.NoHealOnLevelUp_Patch), "No Heal on Level Up Patch")) successCount++; else failCount++;
                }
                
                // Add Formation Bonus patches if any are enabled
                if (EnableFormationBonusReset.Value || EnableFormationBonusHalved.Value || 
                    EnableFormationBonusHarder.Value || EnableFormationBonusDisable.Value)
                {
                    if (TryApplyPatch(typeof(Patches.Gameplay.FormationBonusPatches), "Formation Bonus Modifications")) successCount++; else failCount++;
                }
                
                // Add Chain Battle patches if enabled
                if (EnableChainBattleNerf.Value || EnableChainBattleDisable.Value)
                {
                    if (TryApplyPatch(typeof(Patches.Gameplay.ChainBattleBonusPatches), "Chain Battle Bonus Modifications")) successCount++; else failCount++;
                }
                
                // Add Mission Reward patches if enabled
                if (EnableMissionRewardNerf.Value)
                {
                    if (TryApplyPatch(typeof(Patches.Gameplay.MissionRewardPatches), "Mission Reward Modifications")) successCount++; else failCount++;
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
                _harmonyInstance.PatchAll(patchType);
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
            // Debug
            EnableDebugMode = Config.Bind("Debug", "Enable Debug Logging", false, "Enables detailed debug logging for troubleshooting.");
            
            // General
            EnablePauseOnFocusLoss = Config.Bind("General", "Pause On Focus Loss", true, "Automatically pauses the game when the window loses focus.");
            
            // BGM Info
            EnableBgmInfo = Config.Bind("BGM Info", "Enable", true, "Shows the current BGM track name and details on screen.");
            ShowOncePerSession = Config.Bind("BGM Info", "Show Once Per Session", true, "If true, BGM info is shown only once per track per session.");
            
            // Gameplay
            EnableMovementMultiplier = Config.Bind("Gameplay", "Enable Movement Speed Multiplier", true, "Enables a multiplier for player movement speed.");
            //MovementSpeedMultiplier = Config.Bind("Gameplay", "Movement Speed Multiplier", 1.75f, "The multiplier for movement speed. 1.0 is normal, 2.0 is double.");
            
            // Difficulty Options
            EnableNoHealOnLevelUp = Config.Bind("Difficulty", "Remove Full Heal on Level Up", false, "Disables the full HP/MP restoration that occurs when characters level up, making the game more challenging.");
            
            // Formation Bonus Modifications
            EnableFormationBonusReset = Config.Bind("Difficulty - Formation Bonuses", "Reset Every Battle", false, "Resets formation bonuses at the start of each battle.");
            EnableFormationBonusHalved = Config.Bind("Difficulty - Formation Bonuses", "Halve Bonus Effects", false, "Reduces all formation bonus effects by 50%.");
            EnableFormationBonusHarder = Config.Bind("Difficulty - Formation Bonuses", "Harder to Acquire", false, "Makes formation bonuses require more spheres to level up.");
            FormationBonusPointMultiplier = Config.Bind("Difficulty - Formation Bonuses", "Point Gain Multiplier", 0.5f, "Multiplier for sphere bonus points gained. 0.5 = half points, 2.0 = double points. Only applies if 'Harder to Acquire' is enabled.");
            EnableFormationBonusDisable = Config.Bind("Difficulty - Formation Bonuses", "Disable Completely", false, "Completely disables the formation bonus system.");
            
            // Chain Battle Modifications
            EnableChainBattleNerf = Config.Bind("Difficulty - Chain Battles", "Reduce Chain Bonuses", false, "Reduces the EXP and Fol bonuses from chain battles.");
            ChainBattleBonusMultiplier = Config.Bind("Difficulty - Chain Battles", "Chain Bonus Multiplier", 0.25f, "Multiplier for chain battle bonuses. 0.25 = 25% of normal, 0 = no bonus. Only applies if 'Reduce Chain Bonuses' is enabled.");
            EnableChainBattleDisable = Config.Bind("Difficulty - Chain Battles", "Disable Chain Bonuses", false, "Completely removes all bonuses from chain battles (overrides multiplier).");
            
            // Mission Reward Modifications
            EnableMissionRewardNerf = Config.Bind("Difficulty - Mission Rewards", "Reduce Mission Rewards", false, "Reduces rewards from missions.");
            MissionRewardMultiplier = Config.Bind("Difficulty - Mission Rewards", "Reward Multiplier", 0.5f, "Multiplier for mission rewards. 0.5 = half rewards, 0.25 = quarter rewards.");
            NerfAllMissionRewards = Config.Bind("Difficulty - Mission Rewards", "Nerf All Missions", false, "If true, reduces rewards from ALL missions. If false, only reduces high-value missions.");
            HighValueMoneyThreshold = Config.Bind("Difficulty - Mission Rewards", "High Value Money Threshold", 5000, "Missions giving this much Fol or more are considered high-value (Guild/Challenge missions).");
            HighValueItemThreshold = Config.Bind("Difficulty - Mission Rewards", "High Value Item Threshold", 10, "Missions giving this many items or more are considered high-value (Guild/Challenge missions).");
        }
    }
}