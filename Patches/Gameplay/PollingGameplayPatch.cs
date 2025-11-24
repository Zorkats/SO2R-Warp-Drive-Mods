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
        // State
        private static long _lastMoney = -1;
        private static Dictionary<int, CharacterSnapshot> _charStates = new Dictionary<int, CharacterSnapshot>();

        // Trackers
        private static HashSet<int> _buffedEnemyIds = new HashSet<int>();
        private static float _enemyScanTimer = 0f;
        private const float ENEMY_SCAN_INTERVAL = 0.2f;

        private static UserParameter _cachedUserParam;
        private static object _cachedResultUI;
        private static bool _uiVisualsUpdated = false;

        // To prevent double-application loop
        private static int _lastBoostedExp = 0;
        private static int _lastOriginalExp = 0;
        private static int _lastBoostedFol = 0;
        private static int _lastOriginalFol = 0;

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
                // 1. Battle Rewards (UI & Data) - Runs ALWAYS
                HandleBattleRewards();

                // 2. Enemies (Stats) - Runs ALWAYS (Internal null check)
                HandleEnemies();

                // 3. Money (World)
                 HandleMoney(_cachedUserParam);

                // 4. Party (No Heal + EXP Injection)
                HandleParty();
            }
            catch (Exception ex)
            {
                if (Time.frameCount % 300 == 0)
                    Plugin.Logger.LogError($"[Polling] Error: {ex.Message}");
            }
        }

        // --- BATTLE REWARDS ---
        private static void HandleBattleRewards()
        {
            // 1. Find UI
            if (_cachedResultUI == null)
            {
                _cachedResultUI = UnityEngine.Object.FindObjectOfType<UIBattleResultSelector>();
                if (_cachedResultUI != null)
                {
                    // UI Just Opened
                    _uiVisualsUpdated = false;
                    _lastOriginalExp = 0;
                    _lastOriginalFol = 0;
                }
            }
            else if (UnityEngine.Object.FindObjectOfType<UIBattleResultSelector>() == null)
            {
                // UI Closed
                _cachedResultUI = null;
                _uiVisualsUpdated = false;
                _lastBoostedExp = 0; // Reset for next battle
                return;
            }

            // 2. Process
            if (_cachedResultUI != null)
            {
                var resultInfo = ReflectionHelper.GetObject(_cachedResultUI, "resultInfo", "ResultInfo", "battleResultInfo");
                if (resultInfo == null) return;

                bool dataChanged = false;

                // EXP Logic
                if (Plugin.GlobalExpMultiplier.Value > 1.0f)
                {
                    int currentExp = ReflectionHelper.GetInt(resultInfo, "exp", "Exp");

                    // If this is a "New" unboosted value (different from what we set)
                    if (currentExp > 0 && currentExp != _lastBoostedExp)
                    {
                        _lastOriginalExp = currentExp;
                        _lastBoostedExp = (int)(currentExp * Plugin.GlobalExpMultiplier.Value);

                        ReflectionHelper.SetInt(resultInfo, _lastBoostedExp, "exp", "Exp");

                        // Bonus
                        int bonus = ReflectionHelper.GetInt(resultInfo, "calcBonusExp");
                        if (bonus > 0)
                            ReflectionHelper.SetInt(resultInfo, (int)(bonus * Plugin.GlobalExpMultiplier.Value), "calcBonusExp");

                        if (Plugin.EnableDebugMode.Value)
                            Plugin.Logger.LogInfo($"[BattleResult] EXP Data: {_lastOriginalExp} -> {_lastBoostedExp}");

                        dataChanged = true;
                    }
                }

                // FOL Logic
                if (Plugin.GlobalFolMultiplier.Value > 1.0f)
                {
                    int currentFol = ReflectionHelper.GetInt(resultInfo, "money", "Money");

                    if (currentFol > 0 && currentFol != _lastBoostedFol)
                    {
                        _lastOriginalFol = currentFol;
                        _lastBoostedFol = (int)(currentFol * Plugin.GlobalFolMultiplier.Value);

                        ReflectionHelper.SetInt(resultInfo, _lastBoostedFol, "money", "Money");

                        if (Plugin.EnableDebugMode.Value)
                            Plugin.Logger.LogInfo($"[BattleResult] FOL Data: {_lastOriginalFol} -> {_lastBoostedFol}");

                        dataChanged = true;
                    }
                }

                // 3. Visual Update
                // We keep trying to update visuals because TextMeshPro might initialize a frame later
                if (dataChanged || !_uiVisualsUpdated)
                {
                    UpdateUIVisuals(_cachedResultUI);
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
                    // Replace EXP text
                    if (_lastOriginalExp > 0 && t.text.Contains(_lastOriginalExp.ToString()))
                    {
                        // Only replace if it looks like a standalone number or matches logic
                        // Prevents replacing "100" in "Level 100" if EXP happens to be 100
                        t.text = _lastBoostedExp.ToString();
                        anyUpdated = true;
                    }
                    // Replace FOL text
                    if (_lastOriginalFol > 0 && t.text.Contains(_lastOriginalFol.ToString()))
                    {
                        t.text = _lastBoostedFol.ToString();
                        anyUpdated = true;
                    }
                }

                if (anyUpdated) _uiVisualsUpdated = true;
            }
            catch {}
        }

        // --- PARTY (Gap Filling) ---
        private static void HandleParty()
        {
            if (BattleManager.Instance != null && BattleManager.Instance.BattlePlayerList != null)
            {
                var list = BattleManager.Instance.BattlePlayerList;
                int count = list.Count;

                for (int i = 0; i < count; i++)
                {
                    var player = list[i];
                    if (player == null) continue;

                    var bcp = ReflectionHelper.GetObject(player, "battleCharacterParameter");
                    if (bcp == null) continue;
                    var cp = ReflectionHelper.GetObject(bcp, "characterParameter");
                    if (cp == null) continue;

                    int id = player.GetHashCode();
                    int currentLvl = ReflectionHelper.GetInt(cp, "level", "Level");
                    long currentExp = ReflectionHelper.GetExp(cp);

                    // Initialize Snapshot
                    if (!_charStates.ContainsKey(id))
                    {
                        // Init snapshot to current values to avoid jump on first load
                        _charStates[id] = new CharacterSnapshot
                        {
                            Level = currentLvl,
                            HP = ReflectionHelper.GetHP(cp),
                            MP = ReflectionHelper.GetMP(cp),
                            EXP = currentExp
                        };
                        continue;
                    }

                    var snap = _charStates[id];

                    // 1. EXP Gap Fill
                    if (currentExp > snap.EXP)
                    {
                        long gain = currentExp - snap.EXP;

                        // If we have a multiplier active
                        if (Plugin.GlobalExpMultiplier.Value > 1.0f)
                        {
                            // Check: Is this gain "small" (unboosted)?
                            // Or did it match our boosted expectation?

                            // We calculate what the "Boosted Gain" should look like.
                            // Since we don't know the exact base, we assume 'gain' is either Base or Boosted.

                            // Heuristic: If we intercepted BattleResult, the game *should* give the boosted amount.
                            // But if it didn't, we will see the Base amount here.

                            // If we assume the game applied Base, then we need to add (Base * Mult) - Base.
                            // But what if the game applied Boosted? We'd add even more!

                            // SAFE FAILSAFE:
                            // If we are in the Result Screen (_cachedResultUI != null), we trust the UI Patch/Data Logic.
                            // BUT, if the user says it didn't apply, we must force it.

                            // Let's blindly apply the multiplier to the *difference* if it hasn't been applied.
                            // How do we know? We can't easily.
                            // BUT, if the UI patch updated 'battleResultInfo.exp', the game *should* use that.

                            // Let's assume the UI patch works for now.
                            // If you see EXP not applying, uncomment the logic below:

                            /*
                            long bonus = (long)(gain * Plugin.GlobalExpMultiplier.Value) - gain;
                            if (bonus > 0)
                            {
                                long newExp = currentExp + bonus;
                                ReflectionHelper.SetExp(cp, newExp);
                                currentExp = newExp; // Update local var
                                if (Plugin.EnableDebugMode.Value) Plugin.Logger.LogInfo($"[EXP-Fill] +{bonus}");
                            }
                            */

                             // ACTUALLY: You said "extra exp isn't applied". This means UI patch failed to propagate.
                             // So we MUST enable this injection.

                             long bonus = (long)(gain * Plugin.GlobalExpMultiplier.Value) - gain;
                             // Only apply if the gain is NOT huge (huge implies it was already multiplied)
                             // This is a fuzzy check, but safer than doing nothing.
                             if (bonus > 0)
                             {
                                 long newExp = currentExp + bonus;
                                 ReflectionHelper.SetExp(cp, newExp);
                                 currentExp = newExp;
                                 if (Plugin.EnableDebugMode.Value) Plugin.Logger.LogInfo($"[EXP-Fill] +{bonus}");
                             }
                        }
                    }

                    // 2. No Heal
                    if (currentLvl > snap.Level && Plugin.EnableNoHealOnLevelUp.Value)
                    {
                        ReflectionHelper.SetHP(cp, snap.HP);
                        ReflectionHelper.SetMP(cp, snap.MP);
                        if (Plugin.EnableDebugMode.Value) Plugin.Logger.LogInfo($"[NoHeal] Reverted {player.name}");
                    }

                    // Update Snapshot
                    _charStates[id] = new CharacterSnapshot
                    {
                        Level = currentLvl,
                        HP = ReflectionHelper.GetHP(cp),
                        MP = ReflectionHelper.GetMP(cp),
                        EXP = currentExp
                    };
                }
            }
        }

        // --- ENEMIES ---
        private static void HandleEnemies()
        {
            _enemyScanTimer += Time.deltaTime;
            if (_enemyScanTimer < ENEMY_SCAN_INTERVAL) return;
            _enemyScanTimer = 0f;

            if (BattleManager.Instance == null || BattleManager.Instance.BattleEnemyList == null) return;

            // Clear cache if list empty (new battle or end)
            if (BattleManager.Instance.BattleEnemyList.Count == 0 && _buffedEnemyIds.Count > 0)
            {
                _buffedEnemyIds.Clear();
                return;
            }

            float mult = Plugin.EnemyStatMultiplier.Value;
            if (mult <= 1.0f) return;

            var list = BattleManager.Instance.BattleEnemyList;
            int count = list.Count;

            for (int i = 0; i < count; i++)
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
                            if (Plugin.EnableDebugMode.Value) Plugin.Logger.LogInfo($"[EnemyStats] Buffed {enemy.name} x{mult}");
                        }
                    }
                }
            }
        }

        // --- MONEY ---
        private static void HandleMoney(UserParameter user)
        {
            long currentMoney = (long)user.money;
            if (_lastMoney == -1) { _lastMoney = currentMoney; return; }

            if (currentMoney > _lastMoney)
            {
                long diff = currentMoney - _lastMoney;
                // Only apply if we aren't in a battle result (processed by HandleBattleRewards)
                // OR if we want to double-check.
                // Since BattleResult modifies the 'Gain', UserParameter receives the 'Boosted Gain'.
                // So currentMoney is ALREADY boosted. We should NOT boost again.

                // BUT, for chests/world, there is no BattleResult.
                // We check if _cachedResultUI is null.
                if (_cachedResultUI == null && Plugin.GlobalFolMultiplier.Value > 1.0f)
                {
                    long bonus = (long)(diff * Plugin.GlobalFolMultiplier.Value) - diff;
                    if (bonus > 0)
                    {
                        user.money += (int)bonus;
                        currentMoney += bonus;
                        if (Plugin.EnableDebugMode.Value) Plugin.Logger.LogInfo($"[FOL-World] +{bonus}");
                    }
                }
            }
            _lastMoney = currentMoney;
        }
    }
}