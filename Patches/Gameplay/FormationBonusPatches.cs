using System;
using HarmonyLib;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    public static class FormationBonusPatches
    {
        // Patch 1: Reset formation bonuses at the start of each battle
        [HarmonyPatch(typeof(BattleManager), "StartBattle")]
        public static class BattleManager_StartBattle_FormationReset
        {
            [HarmonyPostfix]
            public static void Postfix(BattleManager __instance)
            {
                try
                {
                    if (!Plugin.EnableFormationBonusReset.Value) return;
                    
                    // Reset the bonus gauge when battle starts
                    __instance.ResetBonusGauge(true);
                    
                    if (Plugin.EnableDebugMode.Value)
                    {
                        Plugin.Logger.LogInfo("FormationBonus: Reset bonus gauge at battle start");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in Formation Bonus Reset patch: {ex}");
                }
            }
        }
        
        // Patch 2: Halve the effectiveness of formation bonuses
        [HarmonyPatch(typeof(BattleManager), "GetSphereBonusBuffValueCache")]
        public static class BattleManager_GetSphereBonusBuffValueCache_Halve
        {
            [HarmonyPostfix]
            public static void Postfix(ref float __result)
            {
                try
                {
                    if (!Plugin.EnableFormationBonusHalved.Value) return;
                    
                    // Halve the bonus value
                    float originalValue = __result;
                    __result *= 0.5f;
                    
                    if (Plugin.EnableDebugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"FormationBonus: Halved bonus value from {originalValue} to {__result}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in Formation Bonus Halve patch: {ex}");
                }
            }
        }
        
        // Patch 3: Make formation bonuses harder to acquire by reducing sphere points
        [HarmonyPatch(typeof(BattleManager), "IncreaseSphereBonusPoint")]
        public static class BattleManager_IncreaseSphereBonusPoint_Harder
        {
            [HarmonyPrefix]
            public static void Prefix(ref int value)
            {
                try
                {
                    if (!Plugin.EnableFormationBonusHarder.Value) return;
                    
                    // Apply the point multiplier
                    int originalValue = value;
                    value = (int)(value * Plugin.FormationBonusPointMultiplier.Value);
                    
                    // Ensure at least 1 point if original was positive
                    if (originalValue > 0 && value <= 0)
                    {
                        value = 1;
                    }
                    
                    if (Plugin.EnableDebugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"FormationBonus: Modified sphere points from {originalValue} to {value}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in Formation Bonus Harder patch: {ex}");
                }
            }
        }
        
        // Patch 4: Completely disable formation bonuses
        [HarmonyPatch(typeof(BattleManager), "OnGetBonusSphere")]
        public static class BattleManager_OnGetBonusSphere_Disable
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                try
                {
                    if (!Plugin.EnableFormationBonusDisable.Value) return true;
                    
                    // Return false to skip the original method entirely
                    if (Plugin.EnableDebugMode.Value)
                    {
                        Plugin.Logger.LogInfo("FormationBonus: Blocked sphere collection (system disabled)");
                    }
                    
                    return false;
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in Formation Bonus Disable patch: {ex}");
                    return true; // Let the original method run on error
                }
            }
        }
        
        // Additional patch to prevent bonus gauge from functioning when disabled
        [HarmonyPatch(typeof(BattleManager), "GetSphereBonusBuffValueCache")]
        public static class BattleManager_GetSphereBonusBuffValueCache_Disable
        {
            [HarmonyPostfix]
            public static void Postfix(ref float __result)
            {
                try
                {
                    if (!Plugin.EnableFormationBonusDisable.Value) return;
                    
                    // Return 0 for all bonus values when system is disabled
                    __result = 0f;
                    
                    if (Plugin.EnableDebugMode.Value)
                    {
                        Plugin.Logger.LogInfo("FormationBonus: Returned 0 bonus value (system disabled)");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in Formation Bonus Disable Value patch: {ex}");
                }
            }
        }
        
        // Additional patch to prevent visual gauge updates when disabled
        [HarmonyPatch(typeof(BattleManager), "GetBattleSphereBonusCurrentLevelRatio")]
        public static class BattleManager_GetBattleSphereBonusCurrentLevelRatio_Disable
        {
            [HarmonyPostfix]
            public static void Postfix(ref float __result)
            {
                try
                {
                    if (!Plugin.EnableFormationBonusDisable.Value) return;
                    
                    // Return 0 to prevent gauge from showing progress
                    __result = 0f;
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in Formation Bonus Gauge Ratio patch: {ex}");
                }
            }
        }
    }
}