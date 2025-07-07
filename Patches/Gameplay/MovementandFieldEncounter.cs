using System;

using HarmonyLib;

using Game;



namespace SO2R_Warp_Drive_Mods.Patches.Gameplay

{

    // Patch 1: Targets ONLY the Player's overridden run speed method.

    [HarmonyPatch(typeof(FieldPlayer), "GetMoveSpeed", new Type[] { typeof(bool) })]

    public static class PlayerMoveSpeed_Patch

    {

        public static void Postfix(ref float __result)

        {

            try

            {

                if (Plugin.EnableMovementMultiplier.Value)

                {

                    // Apply the standard multiplier to the player.

                    __result *= Plugin.MovementSpeedMultiplier.Value;

                }

            }

            catch (Exception ex)

            {

                Plugin.Logger.LogError($"Exception in PlayerMoveSpeed_Patch: {ex}");

            }

        }

    }



    // Patch 2: Targets the base run speed method for all other characters.

    [HarmonyPatch(typeof(FieldCharacter), "GetMoveSpeed", new Type[] { typeof(bool) })]

    public static class FollowerMoveSpeed_Patch

    {

        public static void Postfix(FieldCharacter __instance, ref float __result)

        {

            try

            {

                // We only want to affect follower NPCs with this patch.

                if (__instance.FieldCharacterType == FieldCharacterType.FollowNpc)

                {

                    if (Plugin.EnableMovementMultiplier.Value)

                    {
                        // Give followers a boost to keep up. Tweak 1.2f if needed.
                        __result *= (Plugin.MovementSpeedMultiplier.Value * 2f);
                    }

                }

            }

            catch (Exception ex)

            {

                Plugin.Logger.LogError($"Exception in FollowerMoveSpeed_Patch: {ex}");

            }

        }

    }



    // Patch 3: Targets the universal walk speed method. FieldPlayer does not override this,

    // so this single patch will affect both the player and followers.

    [HarmonyPatch(typeof(FieldCharacter), "GetWalkSpeed")]

    public static class UniversalWalkSpeed_Patch

    {

        public static void Postfix(FieldCharacter __instance, ref float __result)

        {

            try

            {

                if (!Plugin.EnableMovementMultiplier.Value) return;



                if (__instance.FieldCharacterType == FieldCharacterType.FollowNpc)

                {

                    // Give followers a boost to their walk speed.

                    __result *= (Plugin.MovementSpeedMultiplier.Value * 1.2f);

                }

                else

                {

                    // Player and other characters get the standard multiplier.

                    __result *= Plugin.MovementSpeedMultiplier.Value;

                }

            }

            catch (Exception ex)

            {

                Plugin.Logger.LogError($"Exception in UniversalWalkSpeed_Patch: {ex}");

            }

        }
    }
}