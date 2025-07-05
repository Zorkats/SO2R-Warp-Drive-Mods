using System;
using System.Collections.Generic;
using HarmonyLib;
using Game;
using System.Reflection;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    [HarmonyPatch(typeof(GameManager), "IncreaseExperience")]
    public static class NoHealOnLevelUp_Patch
    {
        // Storage for character HP/MP values before level up
        private static Dictionary<int, (int hp, int mp)> _savedStats = new Dictionary<int, (int, int)>();
        
        // Prefix: Runs BEFORE the original method (before RecoverAll is called)
        [HarmonyPrefix]
        public static void Prefix()
        {
            try
            {
                // Only proceed if the feature is enabled
                if (!Plugin.EnableNoHealOnLevelUp.Value) return;
                
                // Clear previous saved values
                _savedStats.Clear();
                
                // Get the UserParameter through ParameterManager
                var userParam = ParameterManager.Instance?.UserParameter;
                if (userParam == null)
                {
                    Plugin.Logger.LogWarning("NoHealOnLevelUp: UserParameter is null");
                    return;
                }
                
                // We need to use reflection to access the methods since UserParameter isn't decompiled
                var userParamType = userParam.GetType();
                
                // Look for a method like GetPlayerParameter(int) or GetPlayerParameter(PlayerID)
                MethodInfo getPlayerMethod = null;
                
                // First try to find method that takes PlayerID
                getPlayerMethod = userParamType.GetMethod("GetPlayerParameter", new Type[] { typeof(PlayerID) });
                
                // If not found, try with int parameter
                if (getPlayerMethod == null)
                {
                    getPlayerMethod = userParamType.GetMethod("GetPlayerParameter", new Type[] { typeof(int) });
                }
                
                if (getPlayerMethod == null)
                {
                    Plugin.Logger.LogError("NoHealOnLevelUp: Could not find GetPlayerParameter method");
                    return;
                }
                
                // Try to save stats for all possible party members
                // We'll try multiple approaches to cover different scenarios
                
                // Approach 1: Try common PlayerIDs
                PlayerID[] commonPlayerIds = new PlayerID[] 
                {
                    PlayerID.CLAUDE,
                    PlayerID.RENA,
                    PlayerID.CELINE,
                    PlayerID.ASHTON,
                    PlayerID.OPERA,
                    PlayerID.ERNEST,
                    PlayerID.BOWMAN,
                    PlayerID.DIAS,
                    PlayerID.LEON,
                    PlayerID.PRECIS,
                    PlayerID.CHISATO,
                    PlayerID.NOEL,
                    PlayerID.WELCH
                };
                
                foreach (var playerId in commonPlayerIds)
                {
                    try
                    {
                        object playerParam = null;
                        
                        // Try to get the player parameter
                        if (getPlayerMethod.GetParameters()[0].ParameterType == typeof(PlayerID))
                        {
                            playerParam = getPlayerMethod.Invoke(userParam, new object[] { playerId });
                        }
                        else
                        {
                            playerParam = getPlayerMethod.Invoke(userParam, new object[] { (int)playerId });
                        }
                        
                        if (playerParam == null) continue;
                        
                        // Now try to get hitPoint and mentalPoint from the player parameter
                        var playerParamType = playerParam.GetType();
                        
                        // Try different possible property/field names
                        int currentHp = 0;
                        int currentMp = 0;
                        bool foundStats = false;
                        
                        // Try properties first
                        var hpProperty = playerParamType.GetProperty("hitPoint") ?? 
                                        playerParamType.GetProperty("HitPoint") ?? 
                                        playerParamType.GetProperty("CurrentHp") ??
                                        playerParamType.GetProperty("currentHp");
                                        
                        var mpProperty = playerParamType.GetProperty("mentalPoint") ?? 
                                        playerParamType.GetProperty("MentalPoint") ?? 
                                        playerParamType.GetProperty("CurrentMp") ??
                                        playerParamType.GetProperty("currentMp");
                        
                        if (hpProperty != null && mpProperty != null)
                        {
                            currentHp = Convert.ToInt32(hpProperty.GetValue(playerParam));
                            currentMp = Convert.ToInt32(mpProperty.GetValue(playerParam));
                            foundStats = true;
                        }
                        else
                        {
                            // Try fields if properties didn't work
                            var hpField = playerParamType.GetField("hitPoint") ?? 
                                         playerParamType.GetField("HitPoint") ?? 
                                         playerParamType.GetField("currentHp");
                                         
                            var mpField = playerParamType.GetField("mentalPoint") ?? 
                                         playerParamType.GetField("MentalPoint") ?? 
                                         playerParamType.GetField("currentMp");
                            
                            if (hpField != null && mpField != null)
                            {
                                currentHp = Convert.ToInt32(hpField.GetValue(playerParam));
                                currentMp = Convert.ToInt32(mpField.GetValue(playerParam));
                                foundStats = true;
                            }
                        }
                        
                        if (foundStats && currentHp > 0) // Only save if character is active (HP > 0)
                        {
                            _savedStats[(int)playerId] = (currentHp, currentMp);
                            
                            if (Plugin.EnableDebugMode.Value)
                            {
                                Plugin.Logger.LogInfo($"NoHealOnLevelUp: Saved stats for {playerId} - HP: {currentHp}, MP: {currentMp}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Silently continue - not all characters may be in the party
                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogDebug($"NoHealOnLevelUp: Could not get stats for {playerId}: {ex.Message}");
                        }
                    }
                }
                
                if (_savedStats.Count == 0)
                {
                    Plugin.Logger.LogWarning("NoHealOnLevelUp: No character stats were saved - the patch may not work correctly");
                }
                else
                {
                    Plugin.Logger.LogInfo($"NoHealOnLevelUp: Successfully saved stats for {_savedStats.Count} characters");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"NoHealOnLevelUp Prefix error: {ex}");
            }
        }
        
        // Postfix: Runs AFTER the original method (after RecoverAll has been called)
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                // Only proceed if the feature is enabled
                if (!Plugin.EnableNoHealOnLevelUp.Value) return;
                
                // Check if we have any saved stats
                if (_savedStats.Count == 0) return;
                
                // Get the UserParameter through ParameterManager
                var userParam = ParameterManager.Instance?.UserParameter;
                if (userParam == null)
                {
                    Plugin.Logger.LogWarning("NoHealOnLevelUp: UserParameter is null in postfix");
                    return;
                }
                
                var userParamType = userParam.GetType();
                
                // Find the GetPlayerParameter method again
                MethodInfo getPlayerMethod = userParamType.GetMethod("GetPlayerParameter", new Type[] { typeof(PlayerID) }) ??
                                            userParamType.GetMethod("GetPlayerParameter", new Type[] { typeof(int) });
                
                if (getPlayerMethod == null)
                {
                    Plugin.Logger.LogError("NoHealOnLevelUp: Could not find GetPlayerParameter method in postfix");
                    return;
                }
                
                // Restore HP/MP for each saved character
                int restoredCount = 0;
                foreach (var kvp in _savedStats)
                {
                    int playerId = kvp.Key;
                    var (savedHp, savedMp) = kvp.Value;
                    
                    try
                    {
                        object playerParam = null;
                        
                        // Get the player parameter
                        if (getPlayerMethod.GetParameters()[0].ParameterType == typeof(PlayerID))
                        {
                            playerParam = getPlayerMethod.Invoke(userParam, new object[] { (PlayerID)playerId });
                        }
                        else
                        {
                            playerParam = getPlayerMethod.Invoke(userParam, new object[] { playerId });
                        }
                        
                        if (playerParam == null) continue;
                        
                        var playerParamType = playerParam.GetType();
                        
                        // Get max values to ensure we don't exceed them
                        int maxHp = 0;
                        int maxMp = 0;
                        
                        // Try to get max values
                        var maxHpProp = playerParamType.GetProperty("maxHitPoint") ?? 
                                       playerParamType.GetProperty("MaxHitPoint") ?? 
                                       playerParamType.GetProperty("MaxHp");
                        var maxMpProp = playerParamType.GetProperty("maxMentalPoint") ?? 
                                       playerParamType.GetProperty("MaxMentalPoint") ?? 
                                       playerParamType.GetProperty("MaxMp");
                        
                        if (maxHpProp != null && maxMpProp != null)
                        {
                            maxHp = Convert.ToInt32(maxHpProp.GetValue(playerParam));
                            maxMp = Convert.ToInt32(maxMpProp.GetValue(playerParam));
                        }
                        
                        // Now restore the saved values
                        bool restored = false;
                        
                        // Try properties first
                        var hpProperty = playerParamType.GetProperty("hitPoint") ?? 
                                        playerParamType.GetProperty("HitPoint") ?? 
                                        playerParamType.GetProperty("CurrentHp") ??
                                        playerParamType.GetProperty("currentHp");
                                        
                        var mpProperty = playerParamType.GetProperty("mentalPoint") ?? 
                                        playerParamType.GetProperty("MentalPoint") ?? 
                                        playerParamType.GetProperty("CurrentMp") ??
                                        playerParamType.GetProperty("currentMp");
                        
                        if (hpProperty != null && mpProperty != null && hpProperty.CanWrite && mpProperty.CanWrite)
                        {
                            int restoredHp = (maxHp > 0) ? Math.Min(savedHp, maxHp) : savedHp;
                            int restoredMp = (maxMp > 0) ? Math.Min(savedMp, maxMp) : savedMp;
                            
                            hpProperty.SetValue(playerParam, restoredHp);
                            mpProperty.SetValue(playerParam, restoredMp);
                            restored = true;
                            
                            if (Plugin.EnableDebugMode.Value)
                            {
                                Plugin.Logger.LogInfo($"NoHealOnLevelUp: Restored stats for PlayerID {playerId} - HP: {restoredHp}/{maxHp}, MP: {restoredMp}/{maxMp}");
                            }
                        }
                        else
                        {
                            // Try fields if properties didn't work
                            var hpField = playerParamType.GetField("hitPoint") ?? 
                                         playerParamType.GetField("HitPoint") ?? 
                                         playerParamType.GetField("currentHp");
                                         
                            var mpField = playerParamType.GetField("mentalPoint") ?? 
                                         playerParamType.GetField("MentalPoint") ?? 
                                         playerParamType.GetField("currentMp");
                            
                            if (hpField != null && mpField != null)
                            {
                                int restoredHp = (maxHp > 0) ? Math.Min(savedHp, maxHp) : savedHp;
                                int restoredMp = (maxMp > 0) ? Math.Min(savedMp, maxMp) : savedMp;
                                
                                hpField.SetValue(playerParam, restoredHp);
                                mpField.SetValue(playerParam, restoredMp);
                                restored = true;
                                
                                if (Plugin.EnableDebugMode.Value)
                                {
                                    Plugin.Logger.LogInfo($"NoHealOnLevelUp: Restored stats for PlayerID {playerId} - HP: {restoredHp}/{maxHp}, MP: {restoredMp}/{maxMp}");
                                }
                            }
                        }
                        
                        if (restored) restoredCount++;
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogWarning($"NoHealOnLevelUp: Failed to restore stats for PlayerID {playerId}: {ex.Message}");
                    }
                }
                
                Plugin.Logger.LogInfo($"NoHealOnLevelUp: Successfully restored stats for {restoredCount}/{_savedStats.Count} characters");
                
                // Clear saved stats after restoration
                _savedStats.Clear();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"NoHealOnLevelUp Postfix error: {ex}");
            }
        }
    }
}