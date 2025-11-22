using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using SO2R_Warp_Drive_Mods.Patches.System;
using SO2R_Warp_Drive_Mods.Patches.UI;

namespace SO2R_Warp_Drive_Mods
{
    [BepInPlugin("com.zorkats.so2r_qol", "SO2R QoL Patches", "1.0.2")]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance { get; private set; }
        internal static ManualLogSource Logger = null!;
        public static Harmony _harmonyInstance = null!;

        // Flags to track game state
        public static bool IsGameInitialized = false;
        public static bool IsPatchesApplied = false;

        // --- Configuration Entries ---
        internal static ConfigEntry<bool> EnablePauseOnFocusLoss,
            EnableBgmInfo,
            ShowOncePerSession,
            EnableMovementMultiplier,
            EnableDebugMode,
            EnableNoHealOnLevelUp;

        internal static ConfigEntry<bool> EnableFormationBonusReset,
            EnableFormationBonusHalved,
            EnableFormationBonusHarder,
            EnableFormationBonusDisable;

        internal static ConfigEntry<bool> EnableChainBattleNerf, EnableChainBattleDisable;
        internal static ConfigEntry<bool> EnableMissionRewardNerf, NerfAllMissionRewards;

        internal static ConfigEntry<float> FormationBonusPointMultiplier,
            ChainBattleBonusMultiplier,
            MissionRewardMultiplier;

        internal static ConfigEntry<int> HighValueMoneyThreshold, HighValueItemThreshold;
        internal static bool IsBattleActive = false;
        internal static ConfigEntry<float> MovementSpeedMultiplier;

        public override void Load()
        {
            Instance = this;
            Logger = base.Log;
            Logger.LogInfo("Starting SO2R Warp Drive Plugins (Refactored)...");

            SetupConfiguration();

            if (EnableBgmInfo.Value)
            {
                BgmNameLoader.Load();
            }

            // --- PHASE 1: Core Harmony Instance ---
            _harmonyInstance = new Harmony("com.zorkats.so2r_qol");

            // --- PHASE 2: Apply LifeCycle Patch Immediately ---
            // This patch hooks into the game's boot process to safely load everything else.
            try
            {
                _harmonyInstance.PatchAll(typeof(LifeCyclePatch));
                Logger.LogInfo("LifeCycle patches applied. Waiting for game initialization...");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"CRITICAL: Failed to apply LifeCyclePatch. Mod will not function. {ex}");
            }

            // Initialize config manager (but don't create UI yet)
            RuntimeConfigManager.Initialize();
        }

        public override bool Unload()
        {
            RuntimeConfigManager.Cleanup();
            _harmonyInstance?.UnpatchSelf();
            return true;
        }

        /// <summary>
        /// Called by LifeCyclePatch when the game is actually ready.
        /// </summary>
        public void ApplyGameplayPatches()
        {
            if (IsPatchesApplied) return;

            Logger.LogInfo("Game initialized. Applying gameplay patches now...");

            // Apply BGM Patch
            if (EnableBgmInfo.Value)
            {
                // We patch the method itself, not the Update loop
                 _harmonyInstance.PatchAll(typeof(BgmCaptionPatch));
            }

            // Apply System Patches
            _harmonyInstance.PatchAll(typeof(MethodFinderPatch));
            _harmonyInstance.PatchAll(typeof(FavorabilityGauge_AddNumber_Patch.CatchValuePatch));

            if (EnablePauseOnFocusLoss.Value)
            {
                // Note: PauseOnFocusLoss is handled via Unity's OnApplicationFocus,
                // usually simpler to keep as a MonoBehaviour component or check in LifeCycle update
                // For now, we will hook it into the LifeCycle update loop for safety.
            }

            // Apply Movement Patches
            if (EnableMovementMultiplier.Value)
            {
                _harmonyInstance.PatchAll(typeof(Patches.Gameplay.PlayerMoveSpeed_Patch));
                _harmonyInstance.PatchAll(typeof(Patches.Gameplay.FollowerMoveSpeed_Patch));
                _harmonyInstance.PatchAll(typeof(Patches.Gameplay.UniversalWalkSpeed_Patch));
            }

            // Apply Difficulty Patches
            if (EnableNoHealOnLevelUp.Value)
            {
                _harmonyInstance.PatchAll(typeof(Patches.Gameplay.NoHealOnLevelUpPatch));
                _harmonyInstance.PatchAll(typeof(Patches.Gameplay.ExperiencePatch));
            }

            if (EnableFormationBonusReset.Value || EnableFormationBonusHalved.Value ||
                EnableFormationBonusHarder.Value || EnableFormationBonusDisable.Value)
            {
                _harmonyInstance.PatchAll(typeof(Patches.Gameplay.FormationBonusPatches));
            }

            if (EnableChainBattleNerf.Value || EnableChainBattleDisable.Value)
            {
                _harmonyInstance.PatchAll(typeof(Patches.Gameplay.ChainBattleBonusPatches));
            }

            if (EnableMissionRewardNerf.Value)
            {
                _harmonyInstance.PatchAll(typeof(Patches.Gameplay.MissionRewardPatches));
            }

            IsPatchesApplied = true;
            Logger.LogInfo("All gameplay patches applied successfully.");
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

            // Gameplay
            EnableMovementMultiplier = Config.Bind("Gameplay", "Enable Movement Speed Multiplier", true, "Enables speed multiplier.");
            MovementSpeedMultiplier = Config.Bind("Gameplay", "Movement Speed Multiplier", 2f, "1.0 is normal, 2.0 is double.");

            // Difficulty
            EnableNoHealOnLevelUp = Config.Bind("Difficulty", "Remove Full Heal on Level Up", false, "Disables HP/MP restoration on level up.");

            // Formation Bonus
            EnableFormationBonusReset = Config.Bind("Difficulty - Formation Bonuses", "Reset Every Battle", false, "Resets bonuses at battle start.");
            EnableFormationBonusHalved = Config.Bind("Difficulty - Formation Bonuses", "Halve Bonus Effects", false, "Reduces effects by 50%.");
            EnableFormationBonusHarder = Config.Bind("Difficulty - Formation Bonuses", "Harder to Acquire", false, "More spheres needed.");
            FormationBonusPointMultiplier = Config.Bind("Difficulty - Formation Bonuses", "Point Gain Multiplier", 0.5f, "Multiplier for sphere points.");
            EnableFormationBonusDisable = Config.Bind("Difficulty - Formation Bonuses", "Disable Completely", false, "Disables formation bonuses.");

            // Chain Battle
            EnableChainBattleNerf = Config.Bind("Difficulty - Chain Battles", "Reduce Chain Bonuses", false, "Reduces EXP/Fol from chains.");
            ChainBattleBonusMultiplier = Config.Bind("Difficulty - Chain Battles", "Chain Bonus Multiplier", 0.25f, "Multiplier for chain bonuses.");
            EnableChainBattleDisable = Config.Bind("Difficulty - Chain Battles", "Disable Chain Bonuses", false, "Removes chain bonuses.");

            // Mission Rewards
            EnableMissionRewardNerf = Config.Bind("Difficulty - Mission Rewards", "Reduce Mission Rewards", false, "Reduces mission rewards.");
            MissionRewardMultiplier = Config.Bind("Difficulty - Mission Rewards", "Reward Multiplier", 0.5f, "Multiplier for rewards.");
            NerfAllMissionRewards = Config.Bind("Difficulty - Mission Rewards", "Nerf All Missions", false, "Affects all missions, not just high-value ones.");
            HighValueMoneyThreshold = Config.Bind("Difficulty - Mission Rewards", "High Value Money Threshold", 5000, "Threshold for 'high value' Fol.");
            HighValueItemThreshold = Config.Bind("Difficulty - Mission Rewards", "High Value Item Threshold", 10, "Threshold for 'high value' Items.");
        }
    }
}