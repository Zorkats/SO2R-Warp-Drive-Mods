using HarmonyLib;
using Game;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    public static class PollingGameplayPatch
    {
        // --- STATE ---
        private static long _lastMoney = -1;
        private static Dictionary<int, CharacterSnapshot> _charStates = new Dictionary<int, CharacterSnapshot>();

        // Trackers
        private static HashSet<int> _buffedEnemyIds = new HashSet<int>();
        private static float _enemyScanTimer = 0f;
        private const float ENEMY_SCAN_INTERVAL = 0.2f;

        // Reward State
        private static bool _rewardsApplied = false;
        private static UserParameter _cachedUserParam;
        private static object _cachedResultUI;
        private static bool _uiVisualsUpdated = false;

        // Visual Trackers
        private static int _lastOriginalExp = 0;
        private static int _lastBoostedExp = 0;
        private static int _lastOriginalFol = 0;
        private static int _lastBoostedFol = 0;

        // No Heal Lockdown
        // ID -> Duration Remaining
        private static Dictionary<int, float> _hpLockdownTimers = new Dictionary<int, float>();
        private static Dictionary<int, (int hp, int mp)> _hpLockdownValues = new Dictionary<int, (int hp, int mp)>();

        struct CharacterSnapshot
        {
            public int Level;
            public int HP;
            public int MP;
            public long EXP;
        }

        public static void Update()
        {
            if (GameManager.Instance == null) return;

            try
            {
                // 1. Battle Logic
                if (Plugin.IsBattleActive)
                {
                    HandleBattleRewards();
                    HandleEnemies();
                }
                else
                {
                    // Reset Battle State
                    if (_buffedEnemyIds.Count > 0) _buffedEnemyIds.Clear();

                    // Reset UI State only if UI is truly gone
                    if (_cachedResultUI != null && UnityEngine.Object.FindObjectOfType<UIBattleResultSelector>() == null)
                    {
                        _cachedResultUI = null;
                        _rewardsApplied = false;
                        _uiVisualsUpdated = false;
                    }
                }

                // 2. Money (World)
                if (_cachedUserParam == null) _cachedUserParam = FindUserParameter();
                if (_cachedUserParam != null) HandleMoney(_cachedUserParam);

                // 3. Party (No Heal Logic + Lockdown)
                HandleParty();
            }
            catch (Exception ex)
            {
                if (Time.frameCount % 300 == 0)
                    Plugin.Logger.LogError($"[Polling] Error: {ex.Message}");
            }
        }

        // --- BATTLE REWARDS & UI ---
        private static void HandleBattleRewards()
        {
            // 1. Find UI
            if (_cachedResultUI == null)
                _cachedResultUI = UnityEngine.Object.FindObjectOfType<UIBattleResultSelector>();

            if (_cachedResultUI != null)
            {
                // Get Data Object
                var resultInfo = ReflectionHelper.GetObject(_cachedResultUI, "resultInfo", "ResultInfo", "battleResultInfo");

                if (resultInfo != null)
                {
                    bool dataChanged = false;

                    // --- EXP Logic ---
                    if (Plugin.GlobalExpMultiplier.Value > 1.0f)
                    {
                        int exp = ReflectionHelper.GetInt(resultInfo, "exp", "Exp");
                        // Detect NEW unboosted value
                        if (exp > 0 && exp != _lastBoostedExp)
                        {
                            _lastOriginalExp = exp;
                            _lastBoostedExp = (int)(exp * Plugin.GlobalExpMultiplier.Value);

                            ReflectionHelper.SetInt(resultInfo, _lastBoostedExp, "exp", "Exp");

                            // Boost Display Bonus too
                            int bonus = ReflectionHelper.GetInt(resultInfo, "calcBonusExp");
                            if (bonus > 0)
                                ReflectionHelper.SetInt(resultInfo, (int)(bonus * Plugin.GlobalExpMultiplier.Value), "calcBonusExp");

                            if (Plugin.EnableDebugMode.Value)
                                Plugin.Logger.LogInfo($"[BattleResult] EXP Boosted: {_lastOriginalExp} -> {_lastBoostedExp}");

                            dataChanged = true;
                        }
                    }

                    // --- FOL Logic ---
                    if (Plugin.GlobalFolMultiplier.Value > 1.0f)
                    {
                        int fol = ReflectionHelper.GetInt(resultInfo, "money", "Money");
                        if (fol > 0 && fol != _lastBoostedFol)
                        {
                            _lastOriginalFol = fol;
                            _lastBoostedFol = (int)(fol * Plugin.GlobalFolMultiplier.Value);

                            ReflectionHelper.SetInt(resultInfo, _lastBoostedFol, "money", "Money");

                            if (Plugin.EnableDebugMode.Value)
                                Plugin.Logger.LogInfo($"[BattleResult] FOL Boosted: {_lastOriginalFol} -> {_lastBoostedFol}");

                            dataChanged = true;
                        }
                    }

                    // 3. Update Visuals (If data changed OR if we haven't updated UI yet)
                    if (dataChanged || !_uiVisualsUpdated || Time.frameCount % 30 == 0) // Periodically refresh to fight resets
                    {
                        UpdateUIVisuals(_cachedResultUI);
                        _rewardsApplied = true;
                    }
                }
            }
        }

        private static void UpdateUIVisuals(object selector)
        {
            try
            {
                var texts = (selector as Component)?.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts == null) return;

                bool anyUpdated = false;
                foreach (var t in texts)
                {
                    string parentName = t.transform.parent != null ? t.transform.parent.name : "";
                    string objName = t.name;

                    // 1. EXP Update (Strict Filtering)
                    // Must be in an object related to Exp
                    if (_lastBoostedExp > 0)
                    {
                        bool isExpLabel = parentName.IndexOf("Exp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          objName.IndexOf("Exp", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isExpLabel && t.text == _lastOriginalExp.ToString())
                        {
                            t.text = _lastBoostedExp.ToString();
                            anyUpdated = true;
                        }
                    }

                    // 2. FOL Update (Strict Filtering)
                    // Must be in an object related to Fol/Money
                    if (_lastBoostedFol > 0)
                    {
                        bool isFolLabel = parentName.IndexOf("Fol", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          parentName.IndexOf("Money", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          objName.IndexOf("Fol", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isFolLabel && t.text == _lastOriginalFol.ToString())
                        {
                            t.text = _lastBoostedFol.ToString();
                            anyUpdated = true;
                        }
                    }
                }
                if (anyUpdated) _uiVisualsUpdated = true;
            }
            catch {}
        }

        // --- PARTY (No Heal & Failsafes) ---
        private static void HandleParty()
        {
            if (BattleManager.Instance == null || BattleManager.Instance.BattlePlayerList == null) return;

            var list = BattleManager.Instance.BattlePlayerList;
            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    var player = list[i];
                    if (player == null) continue;

                    var bcp = ReflectionHelper.GetObject(player, "battleCharacterParameter");
                    if (bcp == null) continue;
                    var cp = ReflectionHelper.GetObject(bcp, "characterParameter");
                    if (cp == null) continue;

                    int id = player.GetHashCode();
                    int currentLvl = ReflectionHelper.GetInt(cp, "level", "Level");
                    int currentHp = ReflectionHelper.GetHP(cp);
                    int currentMp = ReflectionHelper.GetMP(cp);
                    long currentExp = ReflectionHelper.GetExp(cp);

                    // Init Snapshot
                    if (!_charStates.ContainsKey(id))
                    {
                        _charStates[id] = new CharacterSnapshot { Level = currentLvl, HP = currentHp, MP = currentMp, EXP = currentExp };
                        continue;
                    }

                    var snap = _charStates[id];

                    // --- NO HEAL LOGIC (With Lockdown) ---
                    if (currentLvl > snap.Level)
                    {
                        if (Plugin.EnableNoHealOnLevelUp.Value)
                        {
                            ReflectionHelper.SetHP(cp, snap.HP);
                            ReflectionHelper.SetMP(cp, snap.MP);
                            currentHp = snap.HP;
                            currentMp = snap.MP;

                            if (Plugin.EnableDebugMode.Value)
                                Plugin.Logger.LogInfo($"[NoHeal] {player.name} Level Up! HP Reverted: {snap.HP}");
                        }
                    }

                    _charStates[id] = new CharacterSnapshot { Level = currentLvl, HP = currentHp, MP = currentMp, EXP = currentExp };


                    // --- EXP Failsafe ---
                    // If we aren't in Battle Result, check for direct EXP gains
                    if (currentExp > snap.EXP)
                    {
                        long gain = currentExp - snap.EXP;
                        if (Plugin.GlobalExpMultiplier.Value > 1.0f)
                        {
                            // If gain is small (unboosted) and we aren't currently applying a Result UI boost
                            if (!_rewardsApplied)
                            {
                                long bonus = (long)(gain * Plugin.GlobalExpMultiplier.Value) - gain;
                                if (bonus > 0)
                                {
                                    long newExp = currentExp + bonus;
                                    ReflectionHelper.SetExp(cp, newExp);
                                    currentExp = newExp;
                                    if (Plugin.EnableDebugMode.Value)
                                        Plugin.Logger.LogInfo($"[EXP-Inject] +{bonus}");
                                }
                            }
                        }
                    }

                    _charStates[id] = new CharacterSnapshot { Level = currentLvl, HP = currentHp, MP = currentMp, EXP = currentExp };
                }
                catch {}
            }
        }

        // --- ENEMIES ---
        private static void HandleEnemies()
        {
            _enemyScanTimer += Time.deltaTime;
            if (_enemyScanTimer < ENEMY_SCAN_INTERVAL) return;
            _enemyScanTimer = 0f;

            if (BattleManager.Instance.BattleEnemyList == null) return;

            float mult = Plugin.EnemyStatMultiplier.Value;
            if (mult <= 1.0f) return;

            var list = BattleManager.Instance.BattleEnemyList;
            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    var enemy = list[i];
                    if (enemy == null) continue;
                    int id = enemy.GetHashCode();

                    if (!_buffedEnemyIds.Contains(id))
                    {
                        var bcp = ReflectionHelper.GetObject(enemy, "battleCharacterParameter");
                        if (bcp != null)
                        {
                            var cp = ReflectionHelper.GetObject(bcp, "characterParameter");
                            if (cp != null)
                            {
                                ReflectionHelper.ApplyStatMultiplier(cp, mult);
                                _buffedEnemyIds.Add(id);

                                if (Plugin.EnableDebugMode.Value)
                                    Plugin.Logger.LogInfo($"[EnemyStats] Buffed {enemy.name} x{mult}");
                            }
                        }
                    }
                }
                catch {}
            }
        }

        // --- MONEY ---
        private static UserParameter FindUserParameter()
        {
            try
            {
                var gm = GameManager.Instance;
                return ReflectionHelper.GetObject(gm, "UserParameter", "userParameter") as UserParameter;
            }
            catch {}
            return null;
        }

        private static void HandleMoney(UserParameter user)
        {
            long currentMoney = ReflectionHelper.GetLong(user, "money", "Money");
            if (_lastMoney == -1) { _lastMoney = currentMoney; return; }

            // Only track world money if NOT in battle result
            if (!_rewardsApplied && currentMoney > _lastMoney)
            {
                long diff = currentMoney - _lastMoney;
                if (Plugin.GlobalFolMultiplier.Value > 1.0f)
                {
                    long bonus = (long)(diff * Plugin.GlobalFolMultiplier.Value) - diff;
                    if (bonus > 0)
                    {
                        long newTotal = currentMoney + bonus;
                        ReflectionHelper.SetLong(user, newTotal, "money", "Money");
                        currentMoney = newTotal;
                        if (Plugin.EnableDebugMode.Value) Plugin.Logger.LogInfo($"[FOL-World] +{bonus}");
                    }
                }
            }
            _lastMoney = currentMoney;
        }
    }
}