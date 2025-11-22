using HarmonyLib;
using Game;

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
    /// This patch wraps the IncreaseExperience method to set our flag.
    /// This is a safe, non-battle-related hook.
    /// </summary>
    [HarmonyPatch(typeof(BattleResultInfo), nameof(BattleResultInfo.BattleResultCharacterData.increaseExp))]
    public static class ExperiencePatch
    {
        // Before the method runs, set the flag to true.
        [HarmonyPrefix]
        public static void SetLevelUpFlag()
        {
            Plugin.Logger.LogInfo("[NoHealOnLevelUp] IncreaseExperience started, setting level-up flag.");
            LevelUpState.IsProcessingLevelUp = true;
        }

        // After the method finishes (even if it errors), ensure the flag is reset.
        [HarmonyFinalizer]
        public static void UnsetLevelUpFlag()
        {
            Plugin.Logger.LogInfo("[NoHealOnLevelUp] IncreaseExperience finished, clearing level-up flag.");
            LevelUpState.IsProcessingLevelUp = false;
        }
    }

    /// <summary>
    /// This patch now uses the temporary flag to block heals only during a level-up.
    /// It is completely independent of the battle system and BattleResultInfo.
    /// </summary>
    [HarmonyPatch(typeof(GameManager), nameof(BattleResultInfo.BattleResultCharacterData.preLevel))]
    [HarmonyPatch(typeof(GameManager), nameof(BattleResultInfo.BattleResultCharacterData.levelUpCount))]
    [HarmonyPatch(typeof(GameManager), nameof(BattleResultInfo.BattleResultCharacterData.recoverHitPoint))]
    [HarmonyPatch(typeof(GameManager), nameof(BattleResultInfo.BattleResultCharacterData.recoverMentalPoint))]
    public static class NoHealOnLevelUpPatch
    {
        [HarmonyPrefix]
        public static bool PreventHeal()
        {
            if (!Plugin.EnableNoHealOnLevelUp.Value) return true;

            // ONLY block the heal if our flag is true.
            if (LevelUpState.IsProcessingLevelUp)
            {
                Plugin.Logger.LogInfo("[NoHealOnLevelUp] RecoverAll was called during level-up process. Blocking heal.");
                return false; // This blocks the heal.
            }

            // Otherwise, allow the heal to happen (for inns, items, etc.).
            return true;
        }
    }
}