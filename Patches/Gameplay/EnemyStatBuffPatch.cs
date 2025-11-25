using System;
using System.Collections.Generic;
using UnityEngine;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    /// <summary>
    /// Buffs enemy stats when they spawn in battle.
    /// Uses polling because enemies can spawn at any time during battle.
    /// </summary>
    public static class EnemyStatBuffPatch
    {
        private static HashSet<int> _buffedEnemyIds = new HashSet<int>();
        private static float _scanTimer = 0f;
        private const float SCAN_INTERVAL = 0.25f;

        public static void Update()
        {
            // Only process if multiplier is > 1
            if (Plugin.EnemyStatMultiplier.Value <= 1.0f) return;

            // Only process during battle
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.BattleEnemyList == null) return;

            // Throttle scanning
            _scanTimer += Time.deltaTime;
            if (_scanTimer < SCAN_INTERVAL) return;
            _scanTimer = 0f;

            try
            {
                var enemyList = BattleManager.Instance.BattleEnemyList;
                int count = enemyList.Count;
                float multiplier = Plugin.EnemyStatMultiplier.Value;

                for (int i = 0; i < count; i++)
                {
                    var enemy = enemyList[i];
                    if (enemy == null) continue;

                    int id = enemy.GetHashCode();
                    if (_buffedEnemyIds.Contains(id)) continue;

                    // Get the character parameter
                    var bcp = enemy.BattleCharacterParameter;
                    if (bcp == null) continue;

                    var cp = bcp.CharacterParameter;
                    if (cp == null) continue;

                    // Apply stat multipliers
                    ApplyStatMultiplier(cp, multiplier);
                    _buffedEnemyIds.Add(id);

                    if (Plugin.EnableDebugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"[EnemyBuff] Buffed {enemy.name} x{multiplier}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (Time.frameCount % 300 == 0)
                {
                    Plugin.Logger.LogError($"[EnemyBuff] Error: {ex.Message}");
                }
            }
        }

        private static void ApplyStatMultiplier(CharacterParameter cp, float multiplier)
        {
            try
            {
                // HP
                int maxHp = cp.HitPointMax;
                if (maxHp > 0)
                {
                    int newMaxHp = (int)(maxHp * multiplier);
                    cp.HitPointMax = newMaxHp;
                    cp.HitPoint = newMaxHp; // Set current HP to new max
                }

                // MP
                int maxMp = cp.MentalPointMax;
                if (maxMp > 0)
                {
                    int newMaxMp = (int)(maxMp * multiplier);
                    cp.MentalPointMax = newMaxMp;
                    cp.MentalPoint = newMaxMp;
                }

                // Offensive stats
                int str = cp.Strength;
                if (str > 0) cp.Strength = (int)(str * multiplier);

                int power = cp.Power;
                if (power > 0) cp.Power = (int)(power * multiplier);

                int intel = cp.Intelligence;
                if (intel > 0) cp.Intelligence = (int)(intel * multiplier);

                // Defensive stats
                int con = cp.Constitution;
                if (con > 0) cp.Constitution = (int)(con * multiplier);

                int guts = cp.Guts;
                if (guts > 0) cp.Guts = (int)(guts * multiplier);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[EnemyBuff] ApplyStatMultiplier error: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear tracked enemies when battle ends or scene changes.
        /// </summary>
        public static void ClearCache()
        {
            _buffedEnemyIds.Clear();
            _scanTimer = 0f;
        }
    }
}