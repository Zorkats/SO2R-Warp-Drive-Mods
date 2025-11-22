// --- OnUpdate.cs ---
using HarmonyLib;
using Game;
using SO2R_Warp_Drive_Mods.Patches.UI;
using UnityEngine;

namespace SO2R_Warp_Drive_Mods.Patches.System
{
    [HarmonyPatch(typeof(GameManager), "OnUpdate")]
    public static class GameManager_OnUpdate_CombinedPatch
    {
        private const float PATCH_DELAY_SECONDS = 3.0f;
        private static float _timeSinceLoad = 0f;
        static void Postfix()
        {
            
            if (Plugin.EnableBgmInfo.Value)
            {
                UI.BgmCaptionPatch.Postfix();
            }
            
            // First, handle the delayed patching logic.
            if (!Plugin.DelayedPatchesApplied)
            {
                _timeSinceLoad += Time.deltaTime;
                if (_timeSinceLoad > PATCH_DELAY_SECONDS)
                {
                    Plugin.Logger.LogInfo($"Calling ApplyDelayedPatches after {PATCH_DELAY_SECONDS} seconds.");
                    // --- CHANGE THIS LINE ---
                    // Now we call the method on our globally accessible static instance.
                    Plugin.Instance.ApplyDelayedPatches();
                    Plugin.DelayedPatchesApplied = true;
                }
            }

            // After the delay has passed and patches are applied, run the normal update logic.
            if (Plugin.DelayedPatchesApplied)
            {
                UI.AffectionEditor.Update();
                PauseOnFocusLossPatch.Postfix(); 
                FavorabilityGauge_AddNumber_Patch.ProcessPendingUpdates();
            }
            
            // Update the runtime configuration manager to check for F9 key
            RuntimeConfigManager.Update();
        }
    }
}
