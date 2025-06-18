using System;
using HarmonyLib;
using Game;
using UnityEngine;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    // Instead of patching the problematic FieldAIEnemyDiscoveryBehavior,
    // let's try patching at a higher level that's more likely to work

    // Alternative approach 1: Patch the FieldCharacter movement directly
    [HarmonyPatch(typeof(FieldCharacter))]
    public static class SafeProximityPatch
    {
        private static bool _initialized = false;

        // Try to patch a method that's more likely to exist and be accessible
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(FieldCharacter __instance)
        {
            try
            {
                if (!Plugin.EnableAggroRangeMultiplier.Value) return;
                if (__instance == null) return;

                // This is a much safer approach - we don't try to modify internal AI fields
                // Instead, we can potentially modify the character's behavior indirectly

                // Log successful patch application (only once)
                if (!_initialized)
                {
                    Plugin.Logger.LogInfo("Safe proximity patch applied successfully to FieldCharacter.Update");
                    _initialized = true;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in SafeProximityPatch: {ex.Message}");
            }
        }
    }

    // Alternative approach 2: Try patching at the component level
    [HarmonyPatch(typeof(MonoBehaviour), "Start")]
    public static class ComponentProximityPatch
    {
        private static bool _hasLoggedDiscovery = false;

        public static void Postfix(MonoBehaviour __instance)
        {
            try
            {
                if (!Plugin.EnableAggroRangeMultiplier.Value) return;
                if (__instance == null) return;

                // Check if this is an AI-related component
                var typeName = __instance.GetType().Name;
                if (typeName.Contains("AI") && typeName.Contains("Discovery"))
                {
                    if (!_hasLoggedDiscovery)
                    {
                        Plugin.Logger.LogInfo($"Found AI Discovery component: {typeName}");
                        _hasLoggedDiscovery = true;
                    }

                    // Try to access the component's fields safely
                    TryModifyAIComponent(__instance);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogDebug($"ComponentProximityPatch (non-critical): {ex.Message}");
            }
        }

        private static void TryModifyAIComponent(MonoBehaviour component)
        {
            try
            {
                // Get all fields of the component
                var fields = component.GetType().GetFields(
                    global::System.Reflection.BindingFlags.Public |
                    global::System.Reflection.BindingFlags.NonPublic |
                    global::System.Reflection.BindingFlags.Instance
                );

                foreach (var field in fields)
                {
                    // Look for float fields that might be ranges/distances
                    if (field.FieldType == typeof(float))
                    {
                        var fieldName = field.Name.ToLower();
                        if (fieldName.Contains("range") || fieldName.Contains("distance") ||
                            fieldName.Contains("radius") || fieldName.Contains("vision") ||
                            fieldName.Contains("proximity") || fieldName.Contains("eye"))
                        {
                            try
                            {
                                var originalValue = (float)field.GetValue(component);
                                if (originalValue > 0 && originalValue < 100) // Reasonable range
                                {
                                    var newValue = originalValue * Plugin.AggroRangeMultiplier.Value;
                                    field.SetValue(component, newValue);
                                    Plugin.Logger.LogInfo($"Modified {field.Name}: {originalValue:F2} -> {newValue:F2}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Plugin.Logger.LogDebug($"Failed to modify field {field.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogDebug($"TryModifyAIComponent failed: {ex.Message}");
            }
        }
    }

    // Alternative approach 3: Runtime field scanning and modification
    public static class RuntimeProximityPatcher
    {
        private static bool _scanComplete = false;
        private static float _lastScanTime = 0f;
        private const float SCAN_INTERVAL = 5f; // Scan every 5 seconds

        public static void Update()
        {
            try
            {
                if (!Plugin.EnableAggroRangeMultiplier.Value) return;
                if (_scanComplete) return;

                float currentTime = Time.time;
                if (currentTime - _lastScanTime < SCAN_INTERVAL) return;
                _lastScanTime = currentTime;

                // Find all active AI components in the scene
                ScanForAIComponents();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"RuntimeProximityPatcher error: {ex.Message}");
            }
        }

        private static void ScanForAIComponents()
        {
            try
            {
                // Find all MonoBehaviour components that might be AI-related
                var allComponents = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                int modifiedCount = 0;

                foreach (var component in allComponents)
                {
                    if (component == null) continue;

                    var typeName = component.GetType().Name;
                    if (typeName.Contains("AI") && (typeName.Contains("Discovery") || typeName.Contains("Enemy")))
                    {
                        if (TryModifyAIFields(component))
                        {
                            modifiedCount++;
                        }
                    }
                }

                if (modifiedCount > 0)
                {
                    Plugin.Logger.LogInfo($"Runtime scan modified {modifiedCount} AI components");
                    _scanComplete = true; // Stop scanning once we've found and modified components
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"AI component scan failed: {ex.Message}");
            }
        }

        private static bool TryModifyAIFields(MonoBehaviour component)
        {
            try
            {
                var fields = component.GetType().GetFields(
                    global::System.Reflection.BindingFlags.Public |
                    global::System.Reflection.BindingFlags.NonPublic |
                    global::System.Reflection.BindingFlags.Instance
                );

                bool modified = false;
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(float))
                    {
                        var fieldName = field.Name.ToLower();
                        if (IsProximityField(fieldName))
                        {
                            try
                            {
                                var value = (float)field.GetValue(component);
                                if (value > 0 && value < 200) // Reasonable proximity range
                                {
                                    var newValue = value * Plugin.AggroRangeMultiplier.Value;
                                    field.SetValue(component, newValue);
                                    Plugin.Logger.LogInfo($"Runtime modified {component.GetType().Name}.{field.Name}: {value:F2} -> {newValue:F2}");
                                    modified = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Plugin.Logger.LogDebug($"Failed to modify runtime field {field.Name}: {ex.Message}");
                            }
                        }
                    }
                }

                return modified;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogDebug($"TryModifyAIFields failed: {ex.Message}");
                return false;
            }
        }

        private static bool IsProximityField(string fieldName)
        {
            return fieldName.Contains("range") || fieldName.Contains("distance") ||
                   fieldName.Contains("radius") || fieldName.Contains("vision") ||
                   fieldName.Contains("proximity") || fieldName.Contains("eye") ||
                   fieldName.Contains("detect") || fieldName.Contains("sense");
        }

        public static void Reset()
        {
            _scanComplete = false;
            _lastScanTime = 0f;
        }
    }
}