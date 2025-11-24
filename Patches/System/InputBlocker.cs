using HarmonyLib;
using Game;
using UnityEngine;
using SO2R_Warp_Drive_Mods;
using System;

namespace SO2R_Warp_Drive_Mods.Patches.System
{
    [HarmonyPatch(typeof(GameInputManager))]
    public static class InputBlocker
    {
        public static bool ShouldBlock()
        {
            return Plugin.IsMenuOpen || Plugin.IsFocusLost;
        }

        [HarmonyPatch(nameof(GameInputManager.IsDown), new Type[] { typeof(GameInputManager.InputAction) })]
        [HarmonyPrefix]
        public static bool Prefix_IsDown(ref bool __result)
        {
            if (ShouldBlock()) { __result = false; return false; }
            return true;
        }

        [HarmonyPatch(nameof(GameInputManager.IsRelease), new Type[] { typeof(GameInputManager.InputAction) })]
        [HarmonyPrefix]
        public static bool Prefix_IsRelease(ref bool __result)
        {
            if (ShouldBlock()) { __result = false; return false; }
            return true;
        }

        [HarmonyPatch(nameof(GameInputManager.IsRepeat), new Type[] { typeof(GameInputManager.InputAction) })]
        [HarmonyPrefix]
        public static bool Prefix_IsRepeat(ref bool __result)
        {
            if (ShouldBlock()) { __result = false; return false; }
            return true;
        }

        [HarmonyPatch(nameof(GameInputManager.IsAnyButtonDown))]
        [HarmonyPrefix]
        public static bool Prefix_IsAnyButtonDown(ref bool __result)
        {
            if (ShouldBlock()) { __result = false; return false; }
            return true;
        }

        [HarmonyPatch(nameof(GameInputManager.GetLeftStick))]
        [HarmonyPrefix]
        public static bool Prefix_GetLeftStick(ref Vector2 __result)
        {
            if (ShouldBlock()) { __result = Vector2.zero; return false; }
            return true;
        }

        [HarmonyPatch(nameof(GameInputManager.GetRightStick))]
        [HarmonyPrefix]
        public static bool Prefix_GetRightStick(ref Vector2 __result)
        {
            if (ShouldBlock()) { __result = Vector2.zero; return false; }
            return true;
        }

        [HarmonyPatch(nameof(GameInputManager.GetDPad))]
        [HarmonyPrefix]
        public static bool Prefix_GetDPad(ref Vector2 __result)
        {
            if (ShouldBlock()) { __result = Vector2.zero; return false; }
            return true;
        }
    }
}

