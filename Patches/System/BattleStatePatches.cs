using HarmonyLib;
using Game;
using System;

namespace SO2R_Warp_Drive_Mods.Patches.System
{
    [HarmonyPatch(typeof(BattleManager), "StartBattle", new Type[0])]
    public static class BattleManager_StartBattle_Patch
    {
        public static void Postfix()
        {
            try
            {
                Plugin.IsBattleActive = true;
                Plugin.Logger.LogInfo("Battle Started: BGM UI controller cache cleared.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in BattleManager_StartBattle_Patch: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(BattleManager), "FinishBattle")]
    public static class BattleManager_FinishBattle_Patch
    {
        public static void Postfix()
        {
            try
            {
                Plugin.IsBattleActive = false;
                Plugin.Logger.LogInfo("Battle Finished: BGM UI controller cache cleared.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in BattleManager_FinishBattle_Patch: {ex}");
            }
        }
    }
}