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
    [BepInPlugin("com.zorkats.so2r_qol", "SO2R QoL Patches", "1.0.9")]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance { get; private set; }
        internal static ManualLogSource Logger = null!;
        public static Harmony _harmonyInstance = null!;

        public static bool IsPatchesApplied = false;
        public static bool IsBattleActive = false;
        public static bool IsMenuOpen = false;
        public static bool IsFocusLost = false;

        // --- Config (Keep existing) ---
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
            Logger.LogInfo("Starting SO2R Warp Drive Plugins (v1.0.9 GodLogger)...");

            SetupConfiguration();
            if (EnableBgmInfo.Value) BgmNameLoader.Load();

            _harmonyInstance = new Harmony("com.zorkats.so2r_qol");

            try
            {
                _harmonyInstance.PatchAll(typeof(LifeCyclePatch));
                _harmonyInstance.PatchAll(typeof(InputBlocker));
                _harmonyInstance.PatchAll(typeof(BattleStatsPatch));

            }
            catch (System.Exception ex) { Logger.LogError($"System Patch Failed: {ex}"); }

            try { _harmonyInstance.PatchAll(typeof(FavorabilityGauge_AddNumber_Patch)); }
            catch (System.Exception ex) { Logger.LogError($"Favorability Number Patch Failed: {ex}"); }

            if (EnableBgmInfo.Value)
            {
                try { _harmonyInstance.PatchAll(typeof(BgmCaptionPatch)); }
                catch (System.Exception ex) { Logger.LogError($"BGM Patch Failed: {ex}"); }
            }

            RuntimeConfigManager.Initialize();
        }

        public override bool Unload()
        {
            RuntimeConfigManager.Cleanup();
            _harmonyInstance?.UnpatchSelf();
            return true;
        }

        public void ApplyGameplayPatches()
        {
            if (IsPatchesApplied) return;
            Logger.LogInfo("Applying Gameplay patches...");

            try { _harmonyInstance.PatchAll(typeof(MethodFinderPatch)); } catch {}

            if (EnableMovementMultiplier.Value)
            {
                try { _harmonyInstance.PatchAll(typeof(Patches.Gameplay.PlayerMoveSpeed_Patch)); } catch {}
                try { _harmonyInstance.PatchAll(typeof(Patches.Gameplay.FollowerMoveSpeed_Patch)); } catch {}
                try { _harmonyInstance.PatchAll(typeof(Patches.Gameplay.UniversalWalkSpeed_Patch)); } catch {}
            }

            IsPatchesApplied = true;
        }

        private void SetupConfiguration()
        {
            // (Same as before)
            EnableDebugMode = Config.Bind("Debug", "Enable Debug Logging", true, "Enables detailed debug logging.");
            EnablePauseOnFocusLoss = Config.Bind("General", "Pause On Focus Loss", true, "Automatically pauses the game when window loses focus.");
            EnableBgmInfo = Config.Bind("BGM Info", "Enable", true, "Shows the current BGM track name.");
            ShowOncePerSession = Config.Bind("BGM Info", "Show Once Per Session", true, "Show BGM info only once per track.");
            EnableMovementMultiplier = Config.Bind("Gameplay", "Enable Movement Speed Multiplier", true, "Enables speed multiplier.");
            MovementSpeedMultiplier = Config.Bind("Gameplay", "Movement Speed Multiplier", 2f, "1.0 is normal, 2.0 is double.");
            EnableNoHealOnLevelUp = Config.Bind("Difficulty", "Remove Full Heal on Level Up", false, "Disables HP/MP restoration on level up.");
            GlobalExpMultiplier = Config.Bind("Difficulty", "Global EXP Multiplier", 1.0f, "Multiplies all EXP gained.");
            GlobalFolMultiplier = Config.Bind("Difficulty", "Global Fol Multiplier", 1.0f, "Multiplies all Money gained.");
            EnemyStatMultiplier = Config.Bind("Difficulty", "Enemy Stat Multiplier", 1.0f, "Multiplies Enemy HP/ATK/DEF.");
            EnableFormationBonusReset = Config.Bind("Difficulty - Formation Bonuses", "Reset Every Battle", false, "Resets bonuses at battle start.");
            EnableFormationBonusHalved = Config.Bind("Difficulty - Formation Bonuses", "Halve Bonus Effects", false, "Reduces effects by 50%.");
            EnableFormationBonusHarder = Config.Bind("Difficulty - Formation Bonuses", "Harder to Acquire", false, "More spheres needed.");
            FormationBonusPointMultiplier = Config.Bind("Difficulty - Formation Bonuses", "Point Gain Multiplier", 0.5f, "Multiplier for sphere points.");
            EnableFormationBonusDisable = Config.Bind("Difficulty - Formation Bonuses", "Disable Completely", false, "Disables formation bonuses.");
            EnableChainBattleNerf = Config.Bind("Difficulty - Chain Battles", "Reduce Chain Bonuses", false, "Reduces EXP/Fol from chains.");
            ChainBattleBonusMultiplier = Config.Bind("Difficulty - Chain Battles", "Chain Bonus Multiplier", 0.25f, "Multiplier for chain bonuses.");
            EnableChainBattleDisable = Config.Bind("Difficulty - Chain Battles", "Disable Chain Bonuses", false, "Removes chain bonuses.");
            EnableMissionRewardNerf = Config.Bind("Difficulty - Mission Rewards", "Reduce Mission Rewards", false, "Reduces mission rewards.");
            MissionRewardMultiplier = Config.Bind("Difficulty - Mission Rewards", "Reward Multiplier", 0.5f, "Multiplier for rewards.");
            NerfAllMissionRewards = Config.Bind("Difficulty - Mission Rewards", "Nerf All Missions", false, "Affects all missions.");
            HighValueMoneyThreshold = Config.Bind("Difficulty - Mission Rewards", "High Value Money Threshold", 5000, "Threshold for 'high value' Fol.");
            HighValueItemThreshold = Config.Bind("Difficulty - Mission Rewards", "High Value Item Threshold", 10, "Threshold for 'high value' Items.");
        }
    }
}