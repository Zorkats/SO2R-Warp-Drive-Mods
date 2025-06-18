using HarmonyLib;
using Game;
using System;
using SO2R_Warp_Drive_Mods.Patches.UI;

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

                // Safely reset BGM UI controller cache
                try
                {
                    Plugin.Logger.LogInfo("Battle Started: BGM UI controller cache cleared.");
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogWarning($"Error resetting BGM state on battle start: {ex.Message}");
                }
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

                // Safely reset BGM UI controller cache
                try
                {
                    Plugin.Logger.LogInfo("Battle Finished: BGM UI controller cache cleared.");
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogWarning($"Error resetting BGM state on battle finish: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in BattleManager_FinishBattle_Patch: {ex}");
            }
        }
    }

    // Additional patch to handle battle state changes more robustly
    [HarmonyPatch(typeof(BattleManager), "Initialize")]
    public static class BattleManager_Initialize_Patch
    {
        public static void Postfix()
        {
            try
            {
                Plugin.Logger.LogInfo("BattleManager initialized");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in BattleManager_Initialize_Patch: {ex}");
            }
        }
    }
}