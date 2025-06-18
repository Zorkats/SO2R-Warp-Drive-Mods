using HarmonyLib;
using SO2R_Warp_Drive_Mods;

// WIP
// This is the namespace of the class we want to patch
namespace Game.UI
{
    // This tells Harmony we are patching the UIDotCharacterFacePresenter class
    [HarmonyPatch(typeof(UIDotCharacterFacePresenter))]
    public static class FacePresenterPatch
    {
        // This specifies that we are patching the "Set" method.
        // The 'Prefix' part means our code will run *before* the game's original code.
        [HarmonyPrefix]
        [HarmonyPatch("Set")]
        public static bool SetPrefix(string prefabPath)
        {

            Plugin.Logger.LogInfo("FacePresenter is trying to set prefabPath: " + prefabPath);

            if (string.IsNullOrEmpty(prefabPath))
            {
                Plugin.Logger.LogInfo("Path is empty, skipping original method to keep portrait on screen.");
                return false;
            }

            return true;
        }
    }
}