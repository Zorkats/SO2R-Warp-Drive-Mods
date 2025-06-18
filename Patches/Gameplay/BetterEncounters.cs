using System;
using HarmonyLib;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    [HarmonyPatch(typeof(FieldAIEnemyDiscoveryBehavior), "Initialize")]
    public static class ProximityPatch
    {
        public static void Postfix(AIParameter<FieldCharacter> aiParameter)
        {
            try
            {
                if (!Plugin.EnableAggroRangeMultiplier.Value) return;

                // Step 1: Get the 'aiSearcher' object from AIParameter
                var searcherField = AccessTools.Field(typeof(AIParameter<FieldCharacter>), "aiSearcher");
                var aiSearcher = searcherField.GetValue(aiParameter);
                if (aiSearcher == null) {
                    Plugin.Logger.LogError("Could not get 'aiSearcher'. Patch failed.");
                    return;
                }

                // Step 2: Get the 'aiSenseParameter' object from AISearcher
                var senseParamField = AccessTools.Field(aiSearcher.GetType(), "aiSenseParameter");
                var aiSenseParameter = senseParamField.GetValue(aiSearcher);
                if (aiSenseParameter == null) {
                    Plugin.Logger.LogError("Could not get 'aiSenseParameter'. Patch failed.");
                    return;
                }

                // Step 3: Find and modify the radius field within AISenseParameter
                // You must decompile 'Game.AISenseParameter' to find the real field name.
                // Let's assume the field is named "ProximityRadius" for this example.
                var radiusField = AccessTools.Field(aiSenseParameter.GetType(), "ProximityRadius");

                if (radiusField != null)
                {
                    var originalRadius = (float)radiusField.GetValue(aiSenseParameter);

                    // Also patch the main vision distance here to keep everything in one place!
                    var visionDistanceField = AccessTools.Field(aiSenseParameter.GetType(), "VisionDistance");
                    var originalVision = (float)visionDistanceField.GetValue(aiSenseParameter);

                    // Apply multiplier to both values
                    var newRadius = originalRadius * Plugin.AggroRangeMultiplier.Value;
                    var newVision = originalVision * Plugin.AggroRangeMultiplier.Value;

                    radiusField.SetValue(aiSenseParameter, newRadius);
                    visionDistanceField.SetValue(aiSenseParameter, newVision);

                    Plugin.Logger.LogInfo($"Patched Vision: {originalVision} -> {newVision}. Patched Proximity: {originalRadius} -> {newRadius}");

                    // Optional: You can now remove your old 'EncounterRatePatch' since this
                    // patch handles both vision and proximity, making your mod cleaner.
                }
                else
                {
                    Plugin.Logger.LogWarning("Could not find radius/vision fields in 'AISenseParameter'. Patch may be outdated.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Exception in ProximityPatch: {ex}");
            }
        }
    }
}