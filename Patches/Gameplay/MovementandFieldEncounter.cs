using System;
using HarmonyLib;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    [HarmonyPatch(typeof(Game.FieldCharacter), "GetWalkSpeed")]
    public static class FieldCharacter_GetWalkSpeed_Patch
    {
        public static void Postfix(ref float __result)
        {
            try // Safety block
            {
                if (Plugin.EnableMovementMultiplier.Value)
                {
                    __result *= Plugin.MovementSpeedMultiplier.Value;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Exception in FieldCharacter_GetWalkSpeed_Patch: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Game.FieldPlayer), "GetMoveSpeed")]
    public static class FieldPlayer_GetMoveSpeed_Patch
    {
        public static void Postfix(ref float __result)
        {
            try // Safety block
            {
                if (Plugin.EnableMovementMultiplier.Value)
                {
                    __result *= Plugin.MovementSpeedMultiplier.Value;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Exception in FieldPlayer_GetMoveSpeed_Patch: {ex}");
            }
        }
    }
}