using HarmonyLib;
using Game;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Reflection;
using SO2R_Warp_Drive_Mods.Patches.Gameplay;

namespace SO2R_Warp_Drive_Mods.Patches.Debug
{
    public static class DeepStateLogger
    {
        private static long _lastMoney = -1;
        private static Dictionary<int, string> _lastPartyStats = new Dictionary<int, string>();
        private static HashSet<int> _loggedEnemies = new HashSet<int>();
        private static bool _hasLoggedBattleStart = false;

        public static void Update()
        {
            // --- INPUT TRIGGERS ---
            if (Keyboard.current != null)
            {
                // F11: Dump ALL Class Names
                if (Keyboard.current.f11Key.wasPressedThisFrame)
                {
                    ObjectDumper.DumpAllGameTypes();
                }

                // F12: Deep Dump of GameManager State
                if (Keyboard.current.f12Key.wasPressedThisFrame)
                {
                    if (GameManager.Instance != null)
                    {
                        Plugin.Logger.LogWarning(">>> STARTING GOD MODE DUMP <<<");
                        ObjectDumper.DumpRecursive(GameManager.Instance, "GameManager.Instance", 2);
                        if (BattleManager.Instance != null)
                            ObjectDumper.DumpRecursive(BattleManager.Instance, "BattleManager.Instance", 2);
                        Plugin.Logger.LogWarning(">>> DUMP COMPLETE <<<");
                    }
                }
            }

            if (GameManager.Instance == null) return;

            try
            {
                TraceMoney();
                TraceParty();

                if (Plugin.IsBattleActive)
                {
                    if (!_hasLoggedBattleStart)
                    {
                        Plugin.Logger.LogInfo("[TRACE] --- BATTLE STARTED ---");
                        _hasLoggedBattleStart = true;
                    }
                    TraceEnemies();
                }
                else
                {
                    if (_hasLoggedBattleStart)
                    {
                        Plugin.Logger.LogInfo("[TRACE] --- BATTLE ENDED ---");
                        _hasLoggedBattleStart = false;
                        _loggedEnemies.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                if (Time.frameCount % 300 == 0)
                    Plugin.Logger.LogError($"[DeepStateLogger] Error: {ex.Message}");
            }
        }

        private static void TraceMoney()
        {
            var userParam = ReflectionHelper.GetObject(GameManager.Instance, "UserParameter") as UserParameter;
            if (userParam == null) return;

            long currentMoney = (long)userParam.money;
            if (_lastMoney != -1 && currentMoney != _lastMoney)
            {
                Plugin.Logger.LogInfo($"[TRACE] Money: {_lastMoney} -> {currentMoney}");
            }
            _lastMoney = currentMoney;
        }

        private static void TraceParty()
        {
            if (BattleManager.Instance == null || BattleManager.Instance.BattlePlayerList == null) return;

            var list = BattleManager.Instance.BattlePlayerList;
            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                var player = list[i];
                if (player == null) continue;

                int id = player.GetHashCode();

                var bcp = ReflectionHelper.GetObject(player, "battleCharacterParameter");
                if (bcp == null) continue;
                var cp = ReflectionHelper.GetObject(bcp, "characterParameter");
                if (cp == null) continue;

                int hp = ReflectionHelper.GetHP(cp);
                int mp = ReflectionHelper.GetMP(cp);
                long exp = ReflectionHelper.GetLong(cp, "exp", "Exp", "Experience");
                int lvl = ReflectionHelper.GetInt(cp, "level", "Level");

                string currentStats = $"Lvl:{lvl} HP:{hp} MP:{mp} EXP:{exp}";

                if (!_lastPartyStats.ContainsKey(id))
                {
                    _lastPartyStats[id] = currentStats;
                    Plugin.Logger.LogInfo($"[TRACE] Tracking {player.name}: {currentStats}");
                }
                else if (_lastPartyStats[id] != currentStats)
                {
                    Plugin.Logger.LogInfo($"[TRACE] {player.name}: {_lastPartyStats[id]} -> {currentStats}");
                    _lastPartyStats[id] = currentStats;
                }
            }
        }

        private static void TraceEnemies()
        {
            if (BattleManager.Instance == null || BattleManager.Instance.BattleEnemyList == null) return;

            var list = BattleManager.Instance.BattleEnemyList;
            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                var enemy = list[i];
                if (enemy == null) continue;
                int id = enemy.GetHashCode();

                if (!_loggedEnemies.Contains(id))
                {
                    _loggedEnemies.Add(id);

                    var bcp = ReflectionHelper.GetObject(enemy, "battleCharacterParameter");
                    if (bcp != null)
                    {
                        var cp = ReflectionHelper.GetObject(bcp, "characterParameter");
                        if (cp != null)
                        {
                            int hp = ReflectionHelper.GetHP(cp);
                            int atk = ReflectionHelper.GetInt(cp, "atk", "str", "attack", "Power");
                            Plugin.Logger.LogInfo($"[TRACE] Enemy: {enemy.name} | HP: {hp} | ATK: {atk}");
                        }
                    }
                }
            }
        }
    }
}