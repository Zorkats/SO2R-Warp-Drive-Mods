using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using SO2R_Warp_Drive_Mods.Patches.System;
using SO2R_Warp_Drive_Mods.Patches.UI;
using SO2R_Warp_Drive_Mods.Patches.Debug;
using SO2R_Warp_Drive_Mods.Patches.Gameplay;

namespace SO2R_Warp_Drive_Mods
{
    [BepInPlugin("com.zorkats.so2r_qol", "SO2R QoL Patches", "1.1.0")]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance { get; private set; }
        internal static ManualLogSource Logger = null!;
        public static Harmony _harmonyInstance = null!;

        public static bool IsPatchesApplied = false;
        public static bool IsBattleActive = false;
        public static bool IsMenuOpen = false;
        public static bool IsFocusLost = false;

        // --- Config Entries ---
        internal static ConfigEntry<bool> EnablePauseOnFocusLoss, EnableBgmInfo, ShowOncePerSession, EnableMovementMultiplier, EnableDebugMode, EnableNoHealOnLevelUp;
        internal static ConfigEntry<bool> EnableFormationBonusReset, EnableFormationBonusHalved, EnableFormationBonusHarder, EnableFormationBonusDisable;
        internal static ConfigEntry<bool> EnableChainBattleNerf, EnableChainBattleDisable, EnableMissionRewardNerf, NerfAllMissionRewards;
        internal static ConfigEntry<float> FormationBonusPointMultiplier, ChainBattleBonusMultiplier, MissionRewardMultiplier, MovementSpeedMultiplier;
        internal static ConfigEntry<float> GlobalExpMultiplier, GlobalFolMultiplier, EnemyStatMultiplier;
        internal static ConfigEntry<int> HighValueMoneyThreshold, HighValueItemThreshold;

        public override void Load()
        {
            Instance = this;
            Logger = base.Log;
            Logger.LogInfo("Starting SO2R Warp Drive Plugins (v1.1.0 - Clean Patches)...");

            SetupConfiguration();
            if (EnableBgmInfo.Value) BgmNameLoader.Load();

            _harmonyInstance = new Harmony("com.zorkats.so2r_qol");

            // === CORE SYSTEM PATCHES (Always Applied) ===
            try
            {
                _harmonyInstance.PatchAll(typeof(LifeCyclePatch));
                _harmonyInstance.PatchAll(typeof(InputBlocker));
                Logger.LogInfo("Core system patches applied.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"System Patch Failed: {ex}");
            }

            // === GAMEPLAY PATCHES (Applied Early) ===
            // Note: LevelUpNoHealPatch is now polling-based, called from LifeCyclePatch.Update()
            // No Harmony registration needed for it.

            try
            {
                // Battle Rewards (EXP/FOL multiplier) - Intercepts UI and GiveReward
                _harmonyInstance.PatchAll(typeof(BattleRewardsPatch.UIBattleResultSelector_Set_Patch));
                _harmonyInstance.PatchAll(typeof(BattleRewardsPatch.BattleResultInfo_GiveReward_Patch));
                Logger.LogInfo("BattleRewardsPatch applied.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"BattleRewardsPatch Failed: {ex}");
            }

            try
            {
                // World money (non-battle) - Keep this for chests/quests
                _harmonyInstance.PatchAll(typeof(BattleStatsPatch));
                Logger.LogInfo("BattleStatsPatch (world money) applied.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"BattleStatsPatch Failed: {ex}");
            }

            // === UI PATCHES ===
            try
            {
                _harmonyInstance.PatchAll(typeof(FavorabilityGauge_AddNumber_Patch));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Favorability Number Patch Failed: {ex}");
            }

            if (EnableBgmInfo.Value)
            {
                try
                {
                    _harmonyInstance.PatchAll(typeof(BgmCaptionPatch));
                }
                catch (Exception ex)
                {
                    Logger.LogError($"BGM Patch Failed: {ex}");
                }
            }

            // === FORMATION/CHAIN BATTLE PATCHES ===
            try
            {
                _harmonyInstance.PatchAll(typeof(FormationBonusPatches.BattleManager_StartBattle_FormationReset));
                _harmonyInstance.PatchAll(typeof(FormationBonusPatches.BattleManager_GetSphereBonusBuffValueCache_Halve));
                _harmonyInstance.PatchAll(typeof(FormationBonusPatches.BattleManager_IncreaseSphereBonusPoint_Harder));
                _harmonyInstance.PatchAll(typeof(FormationBonusPatches.BattleManager_OnGetBonusSphere_Disable));
                _harmonyInstance.PatchAll(typeof(FormationBonusPatches.BattleManager_GetSphereBonusBuffValueCache_Disable));
                _harmonyInstance.PatchAll(typeof(FormationBonusPatches.BattleManager_GetBattleSphereBonusCurrentLevelRatio_Disable));
                Logger.LogInfo("FormationBonusPatches applied.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"FormationBonusPatches Failed: {ex}");
            }

            try
            {
                _harmonyInstance.PatchAll(typeof(ChainBattleBonusPatches));
                Logger.LogInfo("ChainBattleBonusPatches applied.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"ChainBattleBonusPatches Failed: {ex}");
            }

            try
            {
                _harmonyInstance.PatchAll(typeof(MissionRewardPatches.ParameterManager_GetRewardParameterList_Patch));
                Logger.LogInfo("MissionRewardPatches applied.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"MissionRewardPatches Failed: {ex}");
            }

            RuntimeConfigManager.Initialize();
            Logger.LogInfo("Plugin initialization complete.");
        }

        public override bool Unload()
        {
            RuntimeConfigManager.Cleanup();
            _harmonyInstance?.UnpatchSelf();
            return true;
        }

        /// <summary>
        /// Called by LifeCyclePatch when the game is fully initialized.
        /// Used for patches that need game systems to be ready.
        /// </summary>
        public void ApplyGameplayPatches()
        {
            if (IsPatchesApplied) return;
            Logger.LogInfo("Applying late gameplay patches...");

            try
            {
                _harmonyInstance.PatchAll(typeof(MethodFinderPatch));
            }
            catch { }

            // Movement speed patches
            if (EnableMovementMultiplier.Value)
            {
                try
                {
                    _harmonyInstance.PatchAll(typeof(PlayerMoveSpeed_Patch));
                    _harmonyInstance.PatchAll(typeof(FollowerMoveSpeed_Patch));
                    _harmonyInstance.PatchAll(typeof(UniversalWalkSpeed_Patch));
                    Logger.LogInfo("Movement speed patches applied.");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Movement patches failed: {ex}");
                }
            }

            IsPatchesApplied = true;
        }

        private void SetupConfiguration()
        {
            // Debug
            EnableDebugMode = Config.Bind("Debug", "Enable Debug Logging", false, "Enables detailed debug logging.");

            // General
            EnablePauseOnFocusLoss = Config.Bind("General", "Pause On Focus Loss", true, "Automatically pauses the game when window loses focus.");

            // BGM Info
            EnableBgmInfo = Config.Bind("BGM Info", "Enable", true, "Shows the current BGM track name.");
            ShowOncePerSession = Config.Bind("BGM Info", "Show Once Per Session", true, "Show BGM info only once per track.");

            // Movement
            EnableMovementMultiplier = Config.Bind("Gameplay", "Enable Movement Speed Multiplier", true, "Enables speed multiplier.");
            MovementSpeedMultiplier = Config.Bind("Gameplay", "Movement Speed Multiplier", 2f, "1.0 is normal, 2.0 is double.");

            // Difficulty - Core
            EnableNoHealOnLevelUp = Config.Bind("Difficulty", "Remove Full Heal on Level Up", false, "Prevents HP/MP restoration when leveling up.");
            GlobalExpMultiplier = Config.Bind("Difficulty", "Global EXP Multiplier", 1.0f, "Multiplies all EXP gained. Values > 1 increase EXP, < 1 decrease.");
            GlobalFolMultiplier = Config.Bind("Difficulty", "Global Fol Multiplier", 1.0f, "Multiplies all Money gained. Values > 1 increase money, < 1 decrease.");
            EnemyStatMultiplier = Config.Bind("Difficulty", "Enemy Stat Multiplier", 1.0f, "Multiplies Enemy HP/ATK/DEF. Values > 1 make enemies stronger.");

            // Difficulty - Formation Bonuses
            EnableFormationBonusReset = Config.Bind("Difficulty - Formation Bonuses", "Reset Every Battle", false, "Resets formation bonuses at battle start.");
            EnableFormationBonusHalved = Config.Bind("Difficulty - Formation Bonuses", "Halve Bonus Effects", false, "Reduces formation bonus effects by 50%.");
            EnableFormationBonusHarder = Config.Bind("Difficulty - Formation Bonuses", "Harder to Acquire", false, "More spheres needed to gain bonuses.");
            FormationBonusPointMultiplier = Config.Bind("Difficulty - Formation Bonuses", "Point Gain Multiplier", 0.5f, "Multiplier for sphere points when 'Harder to Acquire' is enabled.");
            EnableFormationBonusDisable = Config.Bind("Difficulty - Formation Bonuses", "Disable Completely", false, "Completely disables formation bonuses.");

            // Difficulty - Chain Battles
            EnableChainBattleNerf = Config.Bind("Difficulty - Chain Battles", "Reduce Chain Bonuses", false, "Reduces EXP/Fol from chain battles.");
            ChainBattleBonusMultiplier = Config.Bind("Difficulty - Chain Battles", "Chain Bonus Multiplier", 0.25f, "Multiplier for chain bonuses when reduced.");
            EnableChainBattleDisable = Config.Bind("Difficulty - Chain Battles", "Disable Chain Bonuses", false, "Completely removes chain bonuses.");

            // Difficulty - Mission Rewards
            EnableMissionRewardNerf = Config.Bind("Difficulty - Mission Rewards", "Reduce Mission Rewards", false, "Reduces rewards from missions.");
            MissionRewardMultiplier = Config.Bind("Difficulty - Mission Rewards", "Reward Multiplier", 0.5f, "Multiplier for mission rewards.");
            NerfAllMissionRewards = Config.Bind("Difficulty - Mission Rewards", "Nerf All Missions", false, "Affects all missions, not just high-value ones.");
            HighValueMoneyThreshold = Config.Bind("Difficulty - Mission Rewards", "High Value Money Threshold", 5000, "Threshold for detecting 'high value' Fol rewards.");
            HighValueItemThreshold = Config.Bind("Difficulty - Mission Rewards", "High Value Item Threshold", 10, "Threshold for detecting 'high value' Item rewards.");
        }
    }
}