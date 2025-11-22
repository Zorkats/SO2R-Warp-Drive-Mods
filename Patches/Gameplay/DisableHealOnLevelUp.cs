using HarmonyLib;
using Game;
using System;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    /// <summary>
    /// This static class holds a temporary flag that is only true
    /// while the IncreaseExperience method is running.
    /// </summary>
    public static class LevelUpState
    {
        public static bool IsProcessingLevelUp = false;
    }

    /// <summary>
    /// FIX: Changed target to GameManager.IncreaseExperience.
    /// This method exists in GameManager.cs and handles XP calculation.
    /// </summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.IncreaseExperience))]
    public static class ExperiencePatch
    {
        // Before the method runs, set the flag to true.
        [HarmonyPrefix]
        public static void Prefix()
        {
            LevelUpState.IsProcessingLevelUp = true;
        }

        // After the method finishes (even if it errors), ensure the flag is reset.
        [HarmonyFinalizer]
        public static void Finalizer()
        {
            LevelUpState.IsProcessingLevelUp = false;
        }
    }

    /// <summary>
    /// This patch uses the temporary flag to block heals only during a level-up.
    /// </summary>
    public static class NoHealOnLevelUpPatch
    {
        // Helper to check config and state
        private static bool ShouldBlockHeal()
        {
            if (Plugin.EnableNoHealOnLevelUp.Value && LevelUpState.IsProcessingLevelUp)
            {
                // Plugin.Logger.LogInfo("[NoHeal] Heal blocked during level up.");
                return false; // Skip the original heal method
            }
            return true; // Allow the original heal method
        }

        // Patch 1: RecoverAll(bool isResurrection)
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.RecoverAll))]
        [HarmonyPrefix]
        public static bool Prefix_RecoverAll()
        {
            return ShouldBlockHeal();
        }

        // Patch 2: RecoverHitPoint(int recoverValue, bool isResurrection)
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.RecoverHitPoint), new Type[] { typeof(int), typeof(bool) })]
        [HarmonyPrefix]
        public static bool Prefix_RecoverHitPoint_Int()
        {
            return ShouldBlockHeal();
        }

        // Patch 3: RecoverHitPoint(float recoverRate, bool isResurrection)
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.RecoverHitPoint), new Type[] { typeof(float), typeof(bool) })]
        [HarmonyPrefix]
        public static bool Prefix_RecoverHitPoint_Float()
        {
            return ShouldBlockHeal();
        }

        // Patch 4: RecoverMentalPoint(int recoverValue)
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.RecoverMentalPoint), new Type[] { typeof(int) })]
        [HarmonyPrefix]
        public static bool Prefix_RecoverMentalPoint_Int()
        {
            return ShouldBlockHeal();
        }

        // Patch 5: RecoverMentalPoint(float recoverRate)
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.RecoverMentalPoint), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        public static bool Prefix_RecoverMentalPoint_Float()
        {
            return ShouldBlockHeal();
        }
    }
}