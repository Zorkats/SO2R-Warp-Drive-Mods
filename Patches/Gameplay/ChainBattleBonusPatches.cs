using HarmonyLib;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    /// <summary>
    /// Modifies the bonus data directly within the BonusResult struct.
    /// </summary>
    [HarmonyPatch(typeof(BattleResultInfo.BonusResult))]
    public static class ChainBattleBonusPatches
    {
        [HarmonyPatch(nameof(BattleResultInfo.BonusResult.GetExpBonus))]
        [HarmonyPatch(nameof(BattleResultInfo.BonusResult.GetMoneyBonus))]
        [HarmonyPatch(nameof(BattleResultInfo.BonusResult.GetSpBonus))]
        [HarmonyPatch(nameof(BattleResultInfo.BonusResult.GetBpBonus))]
        [HarmonyPrefix]
        public static void ModifyChainBonus(ref BattleResultInfo.BonusResult __instance)
        {
            if (!Plugin.EnableChainBattleNerf.Value && !Plugin.EnableChainBattleDisable.Value) return;
            
            float originalBonus = __instance.chainBonus;
            float newBonus = originalBonus;

            if (Plugin.EnableChainBattleDisable.Value)
            {
                newBonus = 0f;
            }
            else if (Plugin.EnableChainBattleNerf.Value)
            {
                newBonus = originalBonus * Plugin.ChainBattleBonusMultiplier.Value;
            }
            
            // Only log if a change was actually made.
            if (originalBonus != newBonus)
            {
                Plugin.Logger.LogInfo($"[ChainBattleBonus] Chain bonus value changed from {originalBonus:F2} to {newBonus:F2}.");
            }

            __instance.chainBonus = newBonus;
        }
    }
}