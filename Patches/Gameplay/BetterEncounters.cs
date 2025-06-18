using System;
using HarmonyLib;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    [HarmonyPatch(typeof(FieldAIEnemyDiscoveryBehavior), "Initialize")]
    public static class ProximityPatch
    {
        private static bool _patchInitialized = false;
        private static int _failureCount = 0;
        private const int MAX_FAILURES = 5;

        public static void Postfix(AIParameter<FieldCharacter> aiParameter)
        {
            try
            {
                if (!Plugin.EnableAggroRangeMultiplier.Value) return;

                // If we've failed too many times, stop trying
                if (_failureCount >= MAX_FAILURES)
                {
                    if (!_patchInitialized)
                    {
                        Plugin.Logger.LogWarning($"ProximityPatch disabled after {MAX_FAILURES} failures");
                        _patchInitialized = true; // Prevent spam
                    }
                    return;
                }

                // Validate input parameter
                if (aiParameter == null)
                {
                    Plugin.Logger.LogWarning("AIParameter is null, skipping patch");
                    _failureCount++;
                    return;
                }


                // Step 1: Get the 'aiSearcher' object from AIParameter with better error handling
                var searcherField = AccessTools.Field(typeof(AIParameter<FieldCharacter>), "aiSearcher");
                if (searcherField == null)
                {
                    Plugin.Logger.LogError("Could not find 'aiSearcher' field. Game may have updated.");
                    _failureCount++;
                    return;
                }

                var aiSearcher = searcherField.GetValue(aiParameter);
                if (aiSearcher == null)
                {
                    Plugin.Logger.LogWarning("'aiSearcher' field is null, skipping patch");
                    return; // Don't count as failure, might be normal during initialization
                }

                // Step 2: Get the 'aiSenseParameter' object from AISearcher
                var senseParamField = AccessTools.Field(aiSearcher.GetType(), "aiSenseParameter");
                if (senseParamField == null)
                {
                    Plugin.Logger.LogError($"Could not find 'aiSenseParameter' field in type {aiSearcher.GetType().Name}");
                    _failureCount++;
                    return;
                }

                var aiSenseParameter = senseParamField.GetValue(aiSearcher);
                if (aiSenseParameter == null)
                {
                    Plugin.Logger.LogWarning("'aiSenseParameter' is null, skipping patch");
                    return; // Don't count as failure
                }

                // Step 3: Find and modify the radius field within AISenseParameter
                var radiusField = AccessTools.Field(aiSenseParameter.GetType(), "ProximityRadius");
                var visionDistanceField = AccessTools.Field(aiSenseParameter.GetType(), "VisionDistance");

                if (radiusField == null || visionDistanceField == null)
                {
                    // Try alternative field names in case the game updated
                    var allFields = AccessTools.GetDeclaredFields(aiSenseParameter.GetType());
                    Plugin.Logger.LogWarning($"Could not find expected fields in {aiSenseParameter.GetType().Name}. Available fields:");

                    foreach (var field in allFields)
                    {
                        Plugin.Logger.LogInfo($"  - {field.Name} ({field.FieldType.Name})");
                    }

                    // Try to find fields by type instead of name
                    foreach (var field in allFields)
                    {
                        if (field.FieldType == typeof(float))
                        {
                            var fieldName = field.Name.ToLower();
                            if (fieldName.Contains("radius") || fieldName.Contains("proximity"))
                            {
                                radiusField = field;
                                Plugin.Logger.LogInfo($"Found potential radius field: {field.Name}");
                            }
                            else if (fieldName.Contains("vision") || fieldName.Contains("distance") || fieldName.Contains("range"))
                            {
                                visionDistanceField = field;
                                Plugin.Logger.LogInfo($"Found potential vision field: {field.Name}");
                            }
                        }
                    }
                }

                if (radiusField != null && visionDistanceField != null)
                {
                    try
                    {
                        var originalRadius = (float)radiusField.GetValue(aiSenseParameter);
                        var originalVision = (float)visionDistanceField.GetValue(aiSenseParameter);

                        // Validate the original values are reasonable
                        if (originalRadius < 0 || originalRadius > 1000 || originalVision < 0 || originalVision > 1000)
                        {
                            Plugin.Logger.LogWarning($"Suspicious original values - Radius: {originalRadius}, Vision: {originalVision}. Skipping patch.");
                            return;
                        }

                        // Apply multiplier to both values
                        var newRadius = originalRadius * Plugin.AggroRangeMultiplier.Value;
                        var newVision = originalVision * Plugin.AggroRangeMultiplier.Value;

                        radiusField.SetValue(aiSenseParameter, newRadius);
                        visionDistanceField.SetValue(aiSenseParameter, newVision);

                        // Only log success on first patch or every 10th successful patch to reduce spam
                        if (!_patchInitialized || (_failureCount == 0 && UnityEngine.Random.Range(0, 10) == 0))
                        {
                            Plugin.Logger.LogInfo($"Patched Vision: {originalVision:F2} -> {newVision:F2}, Proximity: {originalRadius:F2} -> {newRadius:F2}");
                        }

                        _patchInitialized = true;
                        _failureCount = 0; // Reset failure count on success
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"Error setting field values: {ex.Message}");
                        _failureCount++;
                    }
                }
                else
                {
                    if (!_patchInitialized) // Only log this once
                    {
                        Plugin.Logger.LogError("Could not find radius/vision fields in 'AISenseParameter'. Patch may be outdated or game structure changed.");
                        _patchInitialized = true;
                    }
                    _failureCount++;
                }
            }
            catch (InvalidCastException ex)
            {
                Plugin.Logger.LogError($"Type casting error in ProximityPatch - game structure may have changed: {ex.Message}");
                _failureCount++;
            }
            catch (global::System.Reflection.TargetException ex)
            {
                Plugin.Logger.LogError($"Reflection target error in ProximityPatch: {ex.Message}");
                _failureCount++;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Unexpected exception in ProximityPatch: {ex}");
                _failureCount++;
            }
        }

        // Method to reset patch state if needed
        public static void ResetPatchState()
        {
            _patchInitialized = false;
            _failureCount = 0;
            Plugin.Logger.LogInfo("ProximityPatch state reset");
        }
    }
}