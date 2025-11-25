using System;
using HarmonyLib;
using Game;
using Il2CppSystem.Collections.Generic;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    public static class MissionRewardPatches
    {
        /// <summary>
        /// Patch to modify rewards when they are retrieved from ParameterManager.
        /// Note: Uses Il2CppSystem.Collections.Generic.List, not System.Collections.Generic.List
        /// </summary>
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

                    bool shouldModify = false;

                    if (Plugin.NerfAllMissionRewards.Value)
                    {
                        shouldModify = true;
                    }
                    else
                    {
                        // Try to detect high-value missions by their rewards
                        for (int i = 0; i < __result.Count; i++)
                        {
                            var reward = __result[i];
                            if ((reward.rewardType == RewardType.FOL && reward.count >= Plugin.HighValueMoneyThreshold.Value) ||
                                (reward.rewardType == RewardType.ITEM && reward.count >= Plugin.HighValueItemThreshold.Value))
                            {
                                shouldModify = true;

                                if (Plugin.EnableDebugMode.Value)
                                {
                                    Plugin.Logger.LogInfo($"[MissionReward] Detected high-value reward - Type: {reward.rewardType}, Count: {reward.count}");
                                }
                                break;
                            }
                        }
                    }

                    if (!shouldModify) return;

                    // Modify rewards in place (IL2CPP objects)
                    for (int i = 0; i < __result.Count; i++)
                    {
                        var reward = __result[i];
                        int originalCount = reward.count;

                        if (reward.rewardType == RewardType.FOL ||
                            reward.rewardType == RewardType.ITEM ||
                            reward.rewardType == RewardType.SP ||
                            reward.rewardType == RewardType.BP)
                        {
                            // Apply the multiplier
                            int newCount = Math.Max(1, (int)(originalCount * Plugin.MissionRewardMultiplier.Value));
                            reward.count = newCount;

                            if (Plugin.EnableDebugMode.Value)
                            {
                                Plugin.Logger.LogInfo($"[MissionReward] Modified {reward.rewardType} reward from {originalCount} to {newCount}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[MissionReward] GetRewardParameterList error: {ex.Message}");
                }
            }
        }
    }
}