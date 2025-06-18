using HarmonyLib;
using Game;
using System;
using SO2R_Warp_Drive_Mods.Patches.UI; // Add this using statement

namespace SO2R_Warp_Drive_Mods.Patches.System
{
    [HarmonyPatch(typeof(BattleManager), "StartBattle", new Type[0])]
    public static class BattleManager_StartBattle_Patch
    {
        public static void Postfix()
        {
            Plugin.IsBattleActive = true;
            BgmCaptionPatch._ctrl = null!; // Clear the cached UI controller
            Plugin.Logger.LogInfo("Battle Started: BGM UI controller cache cleared.");
        }
    }

    [HarmonyPatch(typeof(BattleManager), "FinishBattle")]
    public static class BattleManager_FinishBattle_Patch
    {
        public static void Postfix()
        {
            Plugin.IsBattleActive = false;
            BgmCaptionPatch._ctrl = null!; // Clear the cached UI controller
            Plugin.Logger.LogInfo("Battle Finished: BGM UI controller cache cleared.");
        }
    }
}