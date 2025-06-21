using HarmonyLib;
using Game;
using UnityEngine;
using System.Collections.Generic;
using TMPro; // Remember to add the reference to Unity.TextMeshPro.dll

namespace SO2R_Warp_Drive_Mods.Patches.UI
{
    public static class FavorabilityGauge_AddNumber_Patch
    {
        private static readonly Dictionary<UICampStatusFavorabilityGaugePresenter, int> _pendingUpdates = new();
        private static GameObject _textTemplate;

        [HarmonyPatch(typeof(UICampStatusFavorabilityGaugePresenter), "Set", typeof(int))]
        public static class CatchValuePatch
        {
            public static void Postfix(UICampStatusFavorabilityGaugePresenter __instance, int favorability)
            {
                if (__instance != null)
                {
                    _pendingUpdates[__instance] = favorability;
                }
            }
        }

        public static void ProcessPendingUpdates()
        {
            if (_pendingUpdates.Count == 0) return;

            var processedKeys = new List<UICampStatusFavorabilityGaugePresenter>();
            foreach (var kvp in _pendingUpdates)
            {
                var instance = kvp.Key;
                var favorability = kvp.Value;

                if (instance != null && instance.gameObject.activeInHierarchy)
                {
                    TextMeshProUGUI numberDisplay = FindOrCreateNumberDisplay(instance);
                    if (numberDisplay != null)
                    {
                        numberDisplay.text = favorability.ToString();
                        numberDisplay.gameObject.SetActive(true);
                    }
                    processedKeys.Add(instance);
                }
            }

            foreach (var key in processedKeys)
            {
                _pendingUpdates.Remove(key);
            }
        }
        
        private static TextMeshProUGUI FindOrCreateNumberDisplay(UICampStatusFavorabilityGaugePresenter parent)
        {
            Transform existingClone = parent.transform.Find("AffectionNumberDisplay");
            if (existingClone != null)
            {
                return existingClone.GetComponent<TextMeshProUGUI>();
            }

            if (_textTemplate == null)
            {
                Plugin.Logger.LogInfo("[TEMPLATE] Searching for 'FullName' object to use as template...");
                // Find the parent object by its name.
                Transform templateParentTransform = FindChildRecursive(parent.transform.root, "FullName");
                
                if (templateParentTransform != null)
                {
                    // Now, get the actual TextMeshPro component from its child.
                    var templateTMP = templateParentTransform.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (templateTMP != null)
                    {
                        _textTemplate = templateTMP.gameObject;
                        Plugin.Logger.LogInfo($"[TEMPLATE] SUCCESS: Found template object '{_textTemplate.name}' to clone!");
                    }
                    else
                    {
                         Plugin.Logger.LogError("[TEMPLATE] FAILED: Found 'FullName' but it has no TextMeshProUGUI child.");
                         return null;
                    }
                }
                else
                {
                    Plugin.Logger.LogError("[TEMPLATE] FAILED: Could not find 'FullName' object anywhere in the hierarchy.");
                    return null;
                }
            }
            
            GameObject clonedObject = Object.Instantiate(_textTemplate, parent.transform, false);
            clonedObject.name = "AffectionNumberDisplay";
            TextMeshProUGUI newClone = clonedObject.GetComponent<TextMeshProUGUI>();

            // Configure the cloned text
            newClone.text = ""; 
            newClone.alignment = TextAlignmentOptions.Center;
            newClone.fontSize = 22;
            newClone.color = Color.white;
            
            newClone.rectTransform.localPosition = new Vector3(-170f, 0, 0); // Tweak this X value to adjust position
            
            return newClone;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                Transform result = FindChildRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}