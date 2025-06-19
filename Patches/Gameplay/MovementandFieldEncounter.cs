using System;
using HarmonyLib;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    [HarmonyPatch(typeof(Game.FieldCharacter), "GetMoveSpeed")]
    public static class FieldCharacter_GetMoveSpeed_Patch
    {
        public static void Postfix(ref float __result)
        {
            try
            {
                if (Plugin.EnableMovementMultiplier.Value)
                {
                    __result *= Plugin.MovementSpeedMultiplier.Value;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Exception in FieldCharacter_GetMoveSpeed_Patch: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Game.FieldPlayer), "GetMoveSpeed", new Type[] { typeof(bool) })]
    public static class FieldPlayer_GetMoveSpeed_Patch
    {
        public static void Postfix(ref float __result)
        {
            try
            {
                if (Plugin.EnableMovementMultiplier.Value)
                {
                    __result *= Plugin.MovementSpeedMultiplier.Value;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Exception in FieldCharacter_GetMoveSpeed_Patch: {ex}");
            }
        }
    }
}