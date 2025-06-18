using System;
using HarmonyLib;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.System
{
    [HarmonyPatch(typeof(GameManager), "OnUpdate")]
    public static class GameManager_OnUpdate_CombinedPatch
    {
        // Important: We call the Postfix methods from your other classes here.
        static void Postfix()
        {
            // By calling your other patch logic from here, you control the execution order
            // and only apply one patch directly to the game method.

            PauseOnFocusLossPatch.Postfix(); // Logic for pausing on focus loss
            UI.BgmCaptionPatch.Postfix();    // Logic for the BGM Info display
        }
    }
}