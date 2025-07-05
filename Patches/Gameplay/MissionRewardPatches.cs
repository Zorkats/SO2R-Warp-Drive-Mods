using System;
using System.Collections.Generic;
using HarmonyLib;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    public static class MissionRewardPatches
    {
        // We'll need to identify Guild/Challenge missions somehow
        // Options:
        // 1. By mission message text containing "Guild" or "Challenge"
        // 2. By mission ID ranges (if they follow a pattern)
        // 3. By reward amounts (Guild/Challenge tend to give more)
        // 4. Make it configurable so users can specify which missions to nerf
        
        // For now, let's use a more general approach that affects ALL missions
        // but with a separate toggle for it
        
        // Patch to modify rewards when they are retrieved from ParameterManager
        [HarmonyPatch(typeof(ParameterManager), "GetRewardParameterList")]
        public static class ParameterManager_GetRewardParameterList_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(int rewardID, ref List<ConstRewardParameter> __result)
            {
                try
                {
                    if (!Plugin.EnableMissionRewardNerf.Value) return;
                    if (__result == null || __result.Count == 0) return;
                    
                    // Check if we should nerf all missions or try to detect high-value ones
                    bool shouldModify = false;
                    
                    if (Plugin.NerfAllMissionRewards.Value)
                    {
                        // Nerf all mission rewards
                        shouldModify = true;
                    }
                    else
                    {
                        // Try to detect high-value missions by their rewards
                        // Guild/Challenge missions typically give much more than regular missions
                        foreach (var reward in __result)
                        {
                            // Check if this looks like a high-value mission reward
                            if ((reward.rewardType == RewardType.FOL && reward.count >= Plugin.HighValueMoneyThreshold.Value) ||
                                (reward.rewardType == RewardType.ITEM && reward.count >= Plugin.HighValueItemThreshold.Value))
                            {
                                shouldModify = true;
                                
                                if (Plugin.EnableDebugMode.Value)
                                {
                                    Plugin.Logger.LogInfo($"MissionReward: Detected high-value reward - Type: {reward.rewardType}, Count: {reward.count}");
                                }
                                break;
                            }
                        }
                    }
                    
                    if (!shouldModify) return;
                    
                    // Create modified copies of the rewards
                    var modifiedRewards = new List<ConstRewardParameter>();
                    
                    foreach (var reward in __result)
                    {
                        // Create a copy of the reward to modify
                        var modifiedReward = new ConstRewardParameter();
                        
                        // Copy all fields
                        modifiedReward.rewardID = reward.rewardID;
                        modifiedReward.rewardType = reward.rewardType;
                        modifiedReward.value = reward.value;
                        modifiedReward.factorID = reward.factorID;
                        
                        // Modify the count based on reward type
                        int originalCount = reward.count;
                        
                        if (reward.rewardType == RewardType.FOL || 
                            reward.rewardType == RewardType.ITEM ||
                            reward.rewardType == RewardType.SP ||
                            reward.rewardType == RewardType.BP)
                        {
                            // Apply the multiplier
                            modifiedReward.count = Math.Max(1, (int)(originalCount * Plugin.MissionRewardMultiplier.Value));
                            
                            if (Plugin.EnableDebugMode.Value)
                            {
                                Plugin.Logger.LogInfo($"MissionReward: Modified {reward.rewardType} reward from {originalCount} to {modifiedReward.count}");
                            }
                        }
                        else
                        {
                            // Don't modify other reward types (like unlocks)
                            modifiedReward.count = originalCount;
                        }
                        
                        modifiedRewards.Add(modifiedReward);
                    }
                    
                    // Replace the result with our modified list
                    __result = modifiedRewards;
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in MissionReward GetRewardParameterList patch: {ex}");
                }
            }
        }
        
        // Additional logging patch to help identify mission types
        [HarmonyPatch(typeof(UserParameter), "GiveMissionReward")]
        public static class UserParameter_GiveMissionReward_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(MissionID missionID)
            {
                try
                {
                    if (!Plugin.EnableDebugMode.Value) return;
                    
                    // Log mission details to help identify Guild/Challenge missions
                    var missionParam = ParameterManager.Instance?.GetMissionParameter(missionID);
                    if (missionParam != null)
                    {
                        Plugin.Logger.LogInfo($"MissionReward: Completing mission - ID: {missionID}, Category: {missionParam.missionCategory}, MessageID: {missionParam.missionMessageID}");
                        
                        // Try to get the mission name from messages
                        var missionName = ParameterManager.Instance?.GetQuestMessage(missionParam.missionMessageID);
                        if (!string.IsNullOrEmpty(missionName))
                        {
                            Plugin.Logger.LogInfo($"MissionReward: Mission name: {missionName}");
                            
                            // Check if name contains Guild or Challenge
                            if (missionName.Contains("Guild") || missionName.Contains("Challenge") || 
                                missionName.Contains("ギルド") || missionName.Contains("チャレンジ")) // Japanese text
                            {
                                Plugin.Logger.LogInfo("MissionReward: This appears to be a Guild or Challenge mission!");
                            }
                        }
                        
                        // Log reward info
                        var rewards = ParameterManager.Instance?.GetRewardParameterList(missionParam.rewardID);
                        if (rewards != null)
                        {
                            foreach (var reward in rewards)
                            {
                                Plugin.Logger.LogInfo($"  Reward: Type={reward.rewardType}, Value={reward.value}, Count={reward.count}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in MissionReward logging: {ex}");
                }
            }
        }
    }
}