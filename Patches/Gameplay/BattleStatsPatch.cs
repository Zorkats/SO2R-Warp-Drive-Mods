using HarmonyLib;
using Game;
using UnityEngine;
using System;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    public static class BattleStatsPatch
    {
        // Hook IncreaseMoney for World Events (Chests/Quests)
        // We skip this if in battle, letting the BattleResultGetter handle it
        [HarmonyPatch(typeof(GameManager), "IncreaseMoney")]
        [HarmonyPrefix]
        public static void IncreaseMoney_Prefix(ref int __0)
        {
            if (Plugin.IsBattleActive) return;

            if (Plugin.GlobalFolMultiplier.Value != 1.0f && __0 > 0)
            {
                int old = __0;
                __0 = (int)(__0 * Plugin.GlobalFolMultiplier.Value);
                if (Plugin.EnableDebugMode.Value)
                    Plugin.Logger.LogInfo($"[FOL-GameManager] {old} -> {__0}");
            }
        }
    }
}