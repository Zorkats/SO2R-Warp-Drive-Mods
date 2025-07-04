using HarmonyLib;
using Game;
using System;
using System.Reflection;

namespace SO2R_Warp_Drive_Mods.Patches.System
{
    // This patch will help us find the memory address of the affection methods.
    [HarmonyPatch(typeof(GameManager))]
    public static class MethodFinderPatch
    {
        private static bool _friendshipFound = false;
        private static bool _loveFound = false;

        // We use a Prefix patch here because it gives us easy access to __originalMethod.
        // This gives us a reflection object of the method we are patching.
        [HarmonyPatch("SetFriendEmotion")]
        public static void FindFriendship(MethodBase __originalMethod)
        {
            if (_friendshipFound) return;
            
            // Get the memory address (function pointer) of the original method.
            IntPtr address = __originalMethod.MethodHandle.GetFunctionPointer();
            Plugin.Logger.LogInfo("====================================================");
            Plugin.Logger.LogInfo($"METHOD FOUND: SetFriendEmotion is at address: {address.ToString("X")}");
            Plugin.Logger.LogInfo("====================================================");
            _friendshipFound = true;
        }

        [HarmonyPatch("SetLoveEmotion")]
        public static void FindLove(MethodBase __originalMethod)
        {
            if (_loveFound) return;

            IntPtr address = __originalMethod.MethodHandle.GetFunctionPointer();
            Plugin.Logger.LogInfo("====================================================");
            Plugin.Logger.LogInfo($"METHOD FOUND: SetLoveEmotion is at address: {address.ToString("X")}");
            Plugin.Logger.LogInfo("====================================================");
            _loveFound = true;
        }
    }
}