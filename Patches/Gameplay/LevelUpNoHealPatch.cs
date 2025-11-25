using System;
using System.Collections.Generic;
using UnityEngine;
using Game;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    /// <summary>
    /// Prevents full HP/MP restoration on level up using polling.
    /// Uses ReflectionHelper for IL2CPP compatibility (same as working code).
    /// </summary>
    public static class LevelUpNoHealPatch
    {
        private static Dictionary<int, CharacterSnapshot> _charStates = new Dictionary<int, CharacterSnapshot>();

        private struct CharacterSnapshot
        {
            public int Level;
            public int HP;
            public int MP;
        }

        /// <summary>
        /// Call this from the main Update loop.
        /// </summary>
        public static void Update()
        {
            if (!Plugin.EnableNoHealOnLevelUp.Value) return;
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.BattlePlayerList == null) return;

            try
            {
                var list = BattleManager.Instance.BattlePlayerList;
                int count = list.Count;

                for (int i = 0; i < count; i++)
                {
                    var player = list[i];
                    if (player == null) continue;

                    // Use ReflectionHelper like the working code
                    var bcp = ReflectionHelper.GetObject(player, "battleCharacterParameter");
                    if (bcp == null) continue;

                    var cp = ReflectionHelper.GetObject(bcp, "characterParameter");
                    if (cp == null) continue;

                    int id = player.GetHashCode();

                    int currentLvl = ReflectionHelper.GetLevel(cp);
                    int currentHp = ReflectionHelper.GetHP(cp);
                    int currentMp = ReflectionHelper.GetMP(cp);

                    // Initialize tracking for new characters
                    if (!_charStates.ContainsKey(id))
                    {
                        _charStates[id] = new CharacterSnapshot
                        {
                            Level = currentLvl,
                            HP = currentHp,
                            MP = currentMp
                        };

                        if (Plugin.EnableDebugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[NoHeal] Tracking {player.name}: Lvl:{currentLvl} HP:{currentHp} MP:{currentMp}");
                        }
                        continue;
                    }

                    var snap = _charStates[id];

                    // Check for level up
                    if (currentLvl > snap.Level)
                    {
                        // Level increased - revert HP/MP to pre-level-up values
                        ReflectionHelper.SetHP(cp, snap.HP);
                        ReflectionHelper.SetMP(cp, snap.MP);

                        // Always log when reverting (important user feedback)
                        Plugin.Logger.LogInfo($"[NoHeal] {player.name} Lvl {snap.Level}->{currentLvl}: Reverted HP:{currentHp}->{snap.HP} MP:{currentMp}->{snap.MP}");

                        // Update current values for snapshot
                        currentHp = snap.HP;
                        currentMp = snap.MP;
                    }

                    // Update snapshot for next frame
                    _charStates[id] = new CharacterSnapshot
                    {
                        Level = currentLvl,
                        HP = currentHp,
                        MP = currentMp
                    };
                }
            }
            catch (Exception ex)
            {
                if (Time.frameCount % 300 == 0)
                {
                    Plugin.Logger.LogError($"[NoHeal] Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clear tracked characters when battle ends or scene changes.
        /// </summary>
        public static void ClearCache()
        {
            _charStates.Clear();
        }
    }
}