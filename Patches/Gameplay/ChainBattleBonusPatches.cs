using System;
using HarmonyLib;
using Game;
using System.Reflection;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    public static class ChainBattleBonusPatches
    {
        // Patch to modify chain bonus ratio when it's being set
        [HarmonyPatch(typeof(BattleResultInfo))]
        public static class BattleResultInfo_ChainBonus_Patches
        {
            private static FieldInfo chainBonusRatioField;
            
            // Static constructor to cache the field info
            static BattleResultInfo_ChainBonus_Patches()
            {
                // Get the private chainBonusRatio field
                chainBonusRatioField = typeof(BattleResultInfo).GetField("chainBonusRatio", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                    
                if (chainBonusRatioField == null)
                {
                    Plugin.Logger.LogError("ChainBattleBonus: Could not find chainBonusRatio field!");
                }
            }
            
            // Patch any method that might set chain bonus - we'll use GiveReward as a postfix
            [HarmonyPatch("GiveReward")]
            [HarmonyPrefix]
            public static void GiveReward_Prefix(BattleResultInfo __instance)
            {
                try
                {
                    if (!Plugin.EnableChainBattleNerf.Value) return;
                    if (chainBonusRatioField == null) return;
                    
                    // Get current chain bonus ratio
                    float currentRatio = (float)chainBonusRatioField.GetValue(__instance);
                    
                    if (Plugin.EnableChainBattleDisable.Value)
                    {
                        // Completely disable chain bonuses
                        chainBonusRatioField.SetValue(__instance, 0f);
                        
                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"ChainBattleBonus: Disabled chain bonus (was {currentRatio})");
                        }
                    }
                    else
                    {
                        // Apply multiplier to reduce chain bonus
                        float newRatio = currentRatio * Plugin.ChainBattleBonusMultiplier.Value;
                        chainBonusRatioField.SetValue(__instance, newRatio);
                        
                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"ChainBattleBonus: Modified chain bonus from {currentRatio} to {newRatio}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in ChainBattleBonus GiveReward patch: {ex}");
                }
            }
        }
        
        // Alternative approach: Patch the BonusResult structure's chainBonus field
        [HarmonyPatch(typeof(BattleResultInfo.BonusResult), "GetExpBonus")]
        public static class BonusResult_GetExpBonus_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleResultInfo.BonusResult __instance, ref float __result)
            {
                try
                {
                    if (!Plugin.EnableChainBattleNerf.Value) return;
                    
                    // The total bonus includes chain bonus, so we need to recalculate
                    float expBonus = __instance.expBonus;
                    float chainBonus = __instance.chainBonus;
                    
                    if (Plugin.EnableDebugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"ChainBattleBonus: Original Exp Bonus breakdown - Base: {expBonus}, Chain: {chainBonus}");
                    }
                    
                    if (Plugin.EnableChainBattleDisable.Value)
                    {
                        // Remove chain bonus entirely from the calculation
                        __result = expBonus;
                    }
                    else
                    {
                        // Reduce chain bonus by multiplier
                        float modifiedChainBonus = chainBonus * Plugin.ChainBattleBonusMultiplier.Value;
                        __result = expBonus + modifiedChainBonus;
                    }
                    
                    if (Plugin.EnableDebugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"ChainBattleBonus: Modified total Exp bonus to {__result}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in ChainBattleBonus GetExpBonus patch: {ex}");
                }
            }
        }
        
        [HarmonyPatch(typeof(BattleResultInfo.BonusResult), "GetMoneyBonus")]
        public static class BonusResult_GetMoneyBonus_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(BattleResultInfo.BonusResult __instance, ref float __result)
            {
                try
                {
                    if (!Plugin.EnableChainBattleNerf.Value) return;
                    
                    // The total bonus includes chain bonus, so we need to recalculate
                    float moneyBonus = __instance.moneyBonus;
                    float chainBonus = __instance.chainBonus;
                    
                    if (Plugin.EnableDebugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"ChainBattleBonus: Original Money Bonus breakdown - Base: {moneyBonus}, Chain: {chainBonus}");
                    }
                    
                    if (Plugin.EnableChainBattleDisable.Value)
                    {
                        // Remove chain bonus entirely from the calculation
                        __result = moneyBonus;
                    }
                    else
                    {
                        // Reduce chain bonus by multiplier
                        float modifiedChainBonus = chainBonus * Plugin.ChainBattleBonusMultiplier.Value;
                        __result = moneyBonus + modifiedChainBonus;
                    }
                    
                    if (Plugin.EnableDebugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"ChainBattleBonus: Modified total Money bonus to {__result}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in ChainBattleBonus GetMoneyBonus patch: {ex}");
                }
            }
        }
        
        // Additional patch to modify the displayed chain count or bonus in UI
        [HarmonyPatch(typeof(BattleResultInfo), "CalcBonusExp", MethodType.Getter)]
        public static class BattleResultInfo_CalcBonusExp_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ref int __result)
            {
                try
                {
                    if (!Plugin.EnableChainBattleNerf.Value) return;
                    
                    if (Plugin.EnableChainBattleDisable.Value)
                    {
                        // This might represent the bonus exp from chains, set to 0
                        __result = 0;
                        
                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo("ChainBattleBonus: Set CalcBonusExp to 0 (chain disabled)");
                        }
                    }
                    else
                    {
                        // Reduce by multiplier
                        int originalBonus = __result;
                        __result = (int)(__result * Plugin.ChainBattleBonusMultiplier.Value);
                        
                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"ChainBattleBonus: Modified CalcBonusExp from {originalBonus} to {__result}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in ChainBattleBonus CalcBonusExp patch: {ex}");
                }
            }
        }
        
        [HarmonyPatch(typeof(BattleResultInfo), "CalcBonusMoney", MethodType.Getter)]
        public static class BattleResultInfo_CalcBonusMoney_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ref int __result)
            {
                try
                {
                    if (!Plugin.EnableChainBattleNerf.Value) return;
                    
                    if (Plugin.EnableChainBattleDisable.Value)
                    {
                        // This might represent the bonus money from chains, set to 0
                        __result = 0;
                        
                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo("ChainBattleBonus: Set CalcBonusMoney to 0 (chain disabled)");
                        }
                    }
                    else
                    {
                        // Reduce by multiplier
                        int originalBonus = __result;
                        __result = (int)(__result * Plugin.ChainBattleBonusMultiplier.Value);
                        
                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"ChainBattleBonus: Modified CalcBonusMoney from {originalBonus} to {__result}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in ChainBattleBonus CalcBonusMoney patch: {ex}");
                }
            }
        }
    }
}