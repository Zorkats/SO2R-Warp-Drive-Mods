using System;
using HarmonyLib;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    /// <summary>
    /// Modifies battle rewards (EXP, FOL) BEFORE they are distributed.
    /// This ensures both the actual rewards AND the displayed values are multiplied.
    /// </summary>
    public static class BattleRewardsPatch
    {
        // Track whether we've already modified a given BattleResultInfo to prevent double-application
        private static int _lastModifiedResultHash = 0;

        /// <summary>
        /// Patch GiveReward with PREFIX to modify values BEFORE they are distributed.
        /// This is the critical fix - we must modify BEFORE the method processes the data.
        /// </summary>
        [HarmonyPatch(typeof(BattleResultInfo), nameof(BattleResultInfo.GiveReward))]
        public static class BattleResultInfo_GiveReward_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(BattleResultInfo __instance)
            {
                if (__instance == null) return;

                try
                {
                    int currentHash = __instance.GetHashCode();

                    // Prevent double-application
                    if (currentHash == _lastModifiedResultHash)
                    {
                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo("[BattleRewards] GiveReward - Already modified, skipping");
                        }
                        return;
                    }

                    bool modified = false;

                    // Apply EXP multiplier
                    if (Plugin.GlobalExpMultiplier.Value != 1.0f && __instance.exp > 0)
                    {
                        int originalExp = __instance.exp;
                        int newExp = (int)(originalExp * Plugin.GlobalExpMultiplier.Value);
                        __instance.exp = newExp;

                        // Also scale bonus exp
                        if (__instance.calcBonusExp > 0)
                        {
                            __instance.calcBonusExp = (int)(__instance.calcBonusExp * Plugin.GlobalExpMultiplier.Value);
                        }

                        modified = true;

                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[BattleRewards] EXP: {originalExp} -> {newExp} (x{Plugin.GlobalExpMultiplier.Value})");
                        }
                    }

                    // Apply FOL multiplier
                    if (Plugin.GlobalFolMultiplier.Value != 1.0f && __instance.money > 0)
                    {
                        int originalMoney = __instance.money;
                        int newMoney = (int)(originalMoney * Plugin.GlobalFolMultiplier.Value);
                        __instance.money = newMoney;

                        // Also scale bonus money
                        if (__instance.calcBonusMoney > 0)
                        {
                            __instance.calcBonusMoney = (int)(__instance.calcBonusMoney * Plugin.GlobalFolMultiplier.Value);
                        }

                        modified = true;

                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[BattleRewards] FOL: {originalMoney} -> {newMoney} (x{Plugin.GlobalFolMultiplier.Value})");
                        }
                    }

                    if (modified)
                    {
                        _lastModifiedResultHash = currentHash;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[BattleRewards] Error in GiveReward Prefix: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Also patch UIBattleResultSelector.Set to ensure UI shows correct values.
        /// This runs after GiveReward in most cases, so values should already be modified.
        /// But we keep this as a safety net in case Set is called independently.
        /// </summary>
        [HarmonyPatch(typeof(UIBattleResultSelector), nameof(UIBattleResultSelector.Set))]
        public static class UIBattleResultSelector_Set_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(BattleResultInfo resultInfo)
            {
                if (resultInfo == null) return;

                try
                {
                    int resultHash = resultInfo.GetHashCode();

                    // If already modified by GiveReward patch, skip
                    if (resultHash == _lastModifiedResultHash)
                    {
                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo("[BattleRewards] UI Set - Already modified by GiveReward");
                        }
                        return;
                    }

                    // Apply multipliers if not yet applied (edge case)
                    bool modified = false;

                    if (Plugin.GlobalExpMultiplier.Value != 1.0f && resultInfo.exp > 0)
                    {
                        int originalExp = resultInfo.exp;
                        resultInfo.exp = (int)(originalExp * Plugin.GlobalExpMultiplier.Value);
                        if (resultInfo.calcBonusExp > 0)
                        {
                            resultInfo.calcBonusExp = (int)(resultInfo.calcBonusExp * Plugin.GlobalExpMultiplier.Value);
                        }
                        modified = true;

                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[BattleRewards] UI EXP: {originalExp} -> {resultInfo.exp}");
                        }
                    }

                    if (Plugin.GlobalFolMultiplier.Value != 1.0f && resultInfo.money > 0)
                    {
                        int originalMoney = resultInfo.money;
                        resultInfo.money = (int)(originalMoney * Plugin.GlobalFolMultiplier.Value);
                        if (resultInfo.calcBonusMoney > 0)
                        {
                            resultInfo.calcBonusMoney = (int)(resultInfo.calcBonusMoney * Plugin.GlobalFolMultiplier.Value);
                        }
                        modified = true;

                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[BattleRewards] UI FOL: {originalMoney} -> {resultInfo.money}");
                        }
                    }

                    if (modified)
                    {
                        _lastModifiedResultHash = resultHash;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[BattleRewards] Error in UI Set Prefix: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Reset tracking state when battle ends or new battle starts.
        /// </summary>
        public static void ResetState()
        {
            _lastModifiedResultHash = 0;
        }
    }
}