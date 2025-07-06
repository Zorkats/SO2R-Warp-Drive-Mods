using System;
using System.IO;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace SO2R_Warp_Drive_Mods
{
    public static class RuntimeConfigManager
    {
        private static bool _isVisible = false;
        private static GameObject _window;
        private static GameObject _contentPanel;
        private static FileSystemWatcher _configWatcher;
        private static float _lastReloadTime = 0f;
        private const float RELOAD_COOLDOWN = 1f;
        private static Dictionary<string, GameObject> _toggles = new Dictionary<string, GameObject>();
        private static Dictionary<string, GameObject> _sliders = new Dictionary<string, GameObject>();
        private static Dictionary<string, TextMeshProUGUI> _sliderLabels = new Dictionary<string, TextMeshProUGUI>();
        private static int _selectedIndex = 0;
        private static List<string> _optionKeys = new List<string>();
        
        // Pagination
        private static int _currentPage = 0;
        private static int _totalPages = 3;
        private static TextMeshProUGUI _pageIndicator;
        private static List<GameObject> _pageContents = new List<GameObject>();
        
        public static void Initialize()
        {
            try
            {
                // Set up file watcher for config changes
                var configFile = Path.Combine(Paths.ConfigPath, "com.zorkats.so2r_qol.cfg");
                var configDir = Path.GetDirectoryName(configFile);
                
                _configWatcher = new FileSystemWatcher(configDir);
                _configWatcher.Filter = Path.GetFileName(configFile);
                _configWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                _configWatcher.Changed += OnConfigFileChanged;
                _configWatcher.EnableRaisingEvents = true;
                
                Plugin.Logger.LogInfo("Runtime configuration manager initialized");
                Plugin.Logger.LogInfo("Press F9 in-game to open the configuration menu");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Failed to initialize runtime config manager: {ex}");
            }
        }
        
        private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Prevent multiple reloads
                if (Time.time - _lastReloadTime < RELOAD_COOLDOWN) return;
                _lastReloadTime = Time.time;
                
                // Reload the configuration
                Plugin.Instance.Config.Reload();
                Plugin.Logger.LogInfo("Configuration reloaded from file at runtime!");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error reloading config: {ex}");
            }
        }
        
        public static void Update()
        {
            try
            {
                // Check for menu toggle
                if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    _isVisible = !_isVisible;
                    Plugin.Logger.LogInfo($"Runtime Config toggled. Visible: {_isVisible}");
                    
                    if (_isVisible && _window == null)
                    {
                        CreateConfigWindow();
                    }
                    
                    if (_window != null)
                    {
                        _window.SetActive(_isVisible);
                        
                        if (_isVisible)
                        {
                            UpdateUIValues();
                            ShowPage(_currentPage);
                            Time.timeScale = 0f;
                            Cursor.visible = true;
                            Cursor.lockState = CursorLockMode.None;
                        }
                        else
                        {
                            Time.timeScale = 1f;
                        }
                    }
                }
                
                // Handle input when window is visible
                if (_isVisible && _window != null && Keyboard.current != null)
                {
                    HandleInput();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in RuntimeConfigManager.Update: {ex}");
            }
        }
        
        private static void HandleInput()
        {
            var kb = Keyboard.current;
            
            // Page navigation
            if (kb.pageDownKey.wasPressedThisFrame || kb.rightBracketKey.wasPressedThisFrame)
            {
                NextPage();
            }
            if (kb.pageUpKey.wasPressedThisFrame || kb.leftBracketKey.wasPressedThisFrame)
            {
                PreviousPage();
            }
            
            // Number keys for toggles
            if (kb.digit1Key.wasPressedThisFrame) ToggleOption("PauseOnFocusLoss");
            if (kb.digit2Key.wasPressedThisFrame) ToggleOption("BgmInfo");
            if (kb.digit3Key.wasPressedThisFrame) ToggleOption("ShowOncePerSession");
            if (kb.digit4Key.wasPressedThisFrame) ToggleOption("MovementMultiplier");
            if (kb.digit5Key.wasPressedThisFrame) ToggleOption("NoHealOnLevelUp");
            if (kb.digit6Key.wasPressedThisFrame) ToggleOption("AggroRangeMultiplier");
            if (kb.digit7Key.wasPressedThisFrame) ToggleOption("FormationBonusReset");
            if (kb.digit8Key.wasPressedThisFrame) ToggleOption("FormationBonusHalved");
            if (kb.digit9Key.wasPressedThisFrame) ToggleOption("FormationBonusHarder");
            if (kb.digit0Key.wasPressedThisFrame) ToggleOption("FormationBonusDisable");
            if (kb.qKey.wasPressedThisFrame) ToggleOption("ChainBattleNerf");
            if (kb.wKey.wasPressedThisFrame) ToggleOption("ChainBattleDisable");
            if (kb.eKey.wasPressedThisFrame) ToggleOption("MissionRewardNerf");
            if (kb.rKey.wasPressedThisFrame) ToggleOption("NerfAllMissions");
            if (kb.tKey.wasPressedThisFrame) ToggleOption("DebugMode");
            
            // Arrow keys for sliders - only if we have sliders
            if (_optionKeys.Count > 0)
            {
                if (kb.leftArrowKey.wasPressedThisFrame) AdjustSlider(-0.1f);
                if (kb.rightArrowKey.wasPressedThisFrame) AdjustSlider(0.1f);
                if (kb.upArrowKey.wasPressedThisFrame) SelectPreviousSlider();
                if (kb.downArrowKey.wasPressedThisFrame) SelectNextSlider();
            }
        }
        
        private static void NextPage()
        {
            _currentPage = (_currentPage + 1) % _totalPages;
            ShowPage(_currentPage);
        }
        
        private static void PreviousPage()
        {
            _currentPage = (_currentPage - 1 + _totalPages) % _totalPages;
            ShowPage(_currentPage);
        }
        
        private static void ShowPage(int pageIndex)
        {
            // Hide all pages
            foreach (var page in _pageContents)
            {
                if (page != null) page.SetActive(false);
            }
            
            // Show current page
            if (pageIndex >= 0 && pageIndex < _pageContents.Count && _pageContents[pageIndex] != null)
            {
                _pageContents[pageIndex].SetActive(true);
            }
            
            // Update page indicator
            if (_pageIndicator != null)
            {
                _pageIndicator.text = $"Page {_currentPage + 1} / {_totalPages} - Use [Page Up/Down] or [ ] to navigate";
            }
            
            // Reset selected slider index when changing pages
            _selectedIndex = 0;
            _optionKeys.Clear();
            
            // Rebuild option keys for current page sliders
            if (pageIndex >= 0 && pageIndex < _pageContents.Count && _pageContents[pageIndex] != null)
            {
                foreach (var kvp in _sliders)
                {
                    if (kvp.Value != null && kvp.Value.transform.IsChildOf(_pageContents[pageIndex].transform))
                    {
                        _optionKeys.Add(kvp.Key);
                    }
                }
            }
        }
        
        private static void ToggleOption(string key)
        {
            switch (key)
            {
                case "PauseOnFocusLoss": Plugin.EnablePauseOnFocusLoss.Value = !Plugin.EnablePauseOnFocusLoss.Value; break;
                case "BgmInfo": Plugin.EnableBgmInfo.Value = !Plugin.EnableBgmInfo.Value; break;
                case "ShowOncePerSession": Plugin.ShowOncePerSession.Value = !Plugin.ShowOncePerSession.Value; break;
                case "MovementMultiplier": Plugin.EnableMovementMultiplier.Value = !Plugin.EnableMovementMultiplier.Value; break;
                case "NoHealOnLevelUp": Plugin.EnableNoHealOnLevelUp.Value = !Plugin.EnableNoHealOnLevelUp.Value; break;
                case "AggroRangeMultiplier": Plugin.EnableAggroRangeMultiplier.Value = !Plugin.EnableAggroRangeMultiplier.Value; break;
                case "FormationBonusReset": Plugin.EnableFormationBonusReset.Value = !Plugin.EnableFormationBonusReset.Value; break;
                case "FormationBonusHalved": Plugin.EnableFormationBonusHalved.Value = !Plugin.EnableFormationBonusHalved.Value; break;
                case "FormationBonusHarder": Plugin.EnableFormationBonusHarder.Value = !Plugin.EnableFormationBonusHarder.Value; break;
                case "FormationBonusDisable": Plugin.EnableFormationBonusDisable.Value = !Plugin.EnableFormationBonusDisable.Value; break;
                case "ChainBattleNerf": Plugin.EnableChainBattleNerf.Value = !Plugin.EnableChainBattleNerf.Value; break;
                case "ChainBattleDisable": Plugin.EnableChainBattleDisable.Value = !Plugin.EnableChainBattleDisable.Value; break;
                case "MissionRewardNerf": Plugin.EnableMissionRewardNerf.Value = !Plugin.EnableMissionRewardNerf.Value; break;
                case "NerfAllMissions": Plugin.NerfAllMissionRewards.Value = !Plugin.NerfAllMissionRewards.Value; break;
                case "DebugMode": Plugin.EnableDebugMode.Value = !Plugin.EnableDebugMode.Value; break;
            }
            
            // Update toggle visual
            if (_toggles.ContainsKey(key))
            {
                var toggle = _toggles[key].GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.isOn = GetToggleValue(key);
                }
            }
            
            UpdateUIValues();
        }
        
        private static bool GetToggleValue(string key)
        {
            switch (key)
            {
                case "PauseOnFocusLoss": return Plugin.EnablePauseOnFocusLoss.Value;
                case "BgmInfo": return Plugin.EnableBgmInfo.Value;
                case "ShowOncePerSession": return Plugin.ShowOncePerSession.Value;
                case "MovementMultiplier": return Plugin.EnableMovementMultiplier.Value;
                case "NoHealOnLevelUp": return Plugin.EnableNoHealOnLevelUp.Value;
                case "AggroRangeMultiplier": return Plugin.EnableAggroRangeMultiplier.Value;
                case "FormationBonusReset": return Plugin.EnableFormationBonusReset.Value;
                case "FormationBonusHalved": return Plugin.EnableFormationBonusHalved.Value;
                case "FormationBonusHarder": return Plugin.EnableFormationBonusHarder.Value;
                case "FormationBonusDisable": return Plugin.EnableFormationBonusDisable.Value;
                case "ChainBattleNerf": return Plugin.EnableChainBattleNerf.Value;
                case "ChainBattleDisable": return Plugin.EnableChainBattleDisable.Value;
                case "MissionRewardNerf": return Plugin.EnableMissionRewardNerf.Value;
                case "NerfAllMissions": return Plugin.NerfAllMissionRewards.Value;
                case "DebugMode": return Plugin.EnableDebugMode.Value;
                default: return false;
            }
        }
        
        private static void SelectPreviousSlider()
        {
            if (_optionKeys.Count == 0) return;
            _selectedIndex = (_selectedIndex - 1 + _optionKeys.Count) % _optionKeys.Count;
            HighlightSelectedSlider();
        }
        
        private static void SelectNextSlider()
        {
            if (_optionKeys.Count == 0) return;
            _selectedIndex = (_selectedIndex + 1) % _optionKeys.Count;
            HighlightSelectedSlider();
        }
        
        private static void HighlightSelectedSlider()
        {
            // Update visual feedback for selected slider
            for (int i = 0; i < _optionKeys.Count; i++)
            {
                var key = _optionKeys[i];
                if (_sliderLabels.ContainsKey(key))
                {
                    _sliderLabels[key].color = (i == _selectedIndex) ? Color.yellow : Color.white;
                }
            }
        }
        
        private static void AdjustSlider(float delta)
        {
            if (_selectedIndex >= 0 && _selectedIndex < _optionKeys.Count)
            {
                var key = _optionKeys[_selectedIndex];
                
                switch (key)
                {
                    case "MovementSpeed":
                        Plugin.MovementSpeedMultiplier.Value = Mathf.Clamp(Plugin.MovementSpeedMultiplier.Value + delta, 0.5f, 3.0f);
                        UpdateSliderVisual(key, Plugin.MovementSpeedMultiplier.Value, 0.5f, 3.0f);
                        break;
                    case "AggroRange":
                        Plugin.AggroRangeMultiplier.Value = Mathf.Clamp(Plugin.AggroRangeMultiplier.Value + delta, 0.1f, 2.0f);
                        UpdateSliderVisual(key, Plugin.AggroRangeMultiplier.Value, 0.1f, 2.0f);
                        break;
                    case "FormationBonusPoint":
                        Plugin.FormationBonusPointMultiplier.Value = Mathf.Clamp(Plugin.FormationBonusPointMultiplier.Value + delta, 0.1f, 1.0f);
                        UpdateSliderVisual(key, Plugin.FormationBonusPointMultiplier.Value, 0.1f, 1.0f);
                        break;
                    case "ChainBattleBonus":
                        Plugin.ChainBattleBonusMultiplier.Value = Mathf.Clamp(Plugin.ChainBattleBonusMultiplier.Value + delta, 0.0f, 1.0f);
                        UpdateSliderVisual(key, Plugin.ChainBattleBonusMultiplier.Value, 0.0f, 1.0f);
                        break;
                    case "MissionReward":
                        Plugin.MissionRewardMultiplier.Value = Mathf.Clamp(Plugin.MissionRewardMultiplier.Value + delta, 0.1f, 1.0f);
                        UpdateSliderVisual(key, Plugin.MissionRewardMultiplier.Value, 0.1f, 1.0f);
                        break;
                }
            }
        }
        
        private static void UpdateSliderVisual(string key, float value, float min, float max)
        {
            if (_sliders.ContainsKey(key))
            {
                var slider = _sliders[key].GetComponent<Slider>();
                if (slider != null)
                {
                    slider.value = value;
                }
                
                if (_sliderLabels.ContainsKey(key))
                {
                    var label = GetSliderLabel(key);
                    _sliderLabels[key].text = $"{label}: {value:F2}x";
                }
            }
        }
        
        private static string GetSliderLabel(string key)
        {
            switch (key)
            {
                case "MovementSpeed": return "Movement Speed";
                case "AggroRange": return "Aggro Range";
                case "FormationBonusPoint": return "Point Gain";
                case "ChainBattleBonus": return "Chain Bonus";
                case "MissionReward": return "Reward Multiplier";
                default: return key;
            }
        }
        
        private static void CreateConfigWindow()
        {
            try
            {
                // Clear existing collections
                _toggles.Clear();
                _sliders.Clear();
                _sliderLabels.Clear();
                _optionKeys.Clear();
                _pageContents.Clear();
                _selectedIndex = 0;
                _currentPage = 0;
                
                // Create window
                _window = new GameObject("RuntimeConfigWindow");
                var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    Plugin.Logger.LogError("Could not find a Canvas to attach the config window to!");
                    return;
                }
                _window.transform.SetParent(canvas.transform, false);
                
                // Background
                var background = _window.AddComponent<Image>();
                background.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
                
                var rect = _window.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(900, 700);
                rect.anchoredPosition = Vector2.zero;
                
                // Title
                var titleObject = new GameObject("ConfigTitle");
                titleObject.transform.SetParent(_window.transform, false);
                
                var titleText = titleObject.AddComponent<TextMeshProUGUI>();
                titleText.text = "SO2R QoL Patches - Runtime Configuration";
                titleText.fontSize = 28;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = Color.white;
                
                var titleRect = titleObject.GetComponent<RectTransform>();
                titleRect.sizeDelta = new Vector2(900, 50);
                titleRect.anchoredPosition = new Vector2(0, 320);
                
                // Instructions
                var instructionObject = new GameObject("Instructions");
                instructionObject.transform.SetParent(_window.transform, false);
                
                var instructionText = instructionObject.AddComponent<TextMeshProUGUI>();
                instructionText.text = "Press F9 to close. Use number keys to toggle options, arrow keys for sliders.";
                instructionText.fontSize = 16;
                instructionText.alignment = TextAlignmentOptions.Center;
                instructionText.color = new Color(0.8f, 0.8f, 0.8f);
                
                var instructionRect = instructionObject.GetComponent<RectTransform>();
                instructionRect.sizeDelta = new Vector2(900, 30);
                instructionRect.anchoredPosition = new Vector2(0, 280);
                
                // Page indicator
                var pageIndicatorObj = new GameObject("PageIndicator");
                pageIndicatorObj.transform.SetParent(_window.transform, false);
                
                _pageIndicator = pageIndicatorObj.AddComponent<TextMeshProUGUI>();
                _pageIndicator.fontSize = 18;
                _pageIndicator.alignment = TextAlignmentOptions.Center;
                _pageIndicator.color = Color.yellow;
                
                var pageRect = pageIndicatorObj.GetComponent<RectTransform>();
                pageRect.sizeDelta = new Vector2(900, 30);
                pageRect.anchoredPosition = new Vector2(0, -320);
                
                // Create content area
                _contentPanel = new GameObject("ContentArea");
                _contentPanel.transform.SetParent(_window.transform, false);
                
                var contentRect = _contentPanel.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 0);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.sizeDelta = new Vector2(-50, -180);
                contentRect.anchoredPosition = new Vector2(0, -20);
                
                // Add background to content area
                var contentBg = _contentPanel.AddComponent<Image>();
                contentBg.color = new Color(0.05f, 0.05f, 0.05f, 0.5f);
                
                // Create pages
                CreateConfigPages();
                
                Plugin.Logger.LogInfo("Runtime config window created successfully with pagination.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in CreateConfigWindow: {ex}");
            }
        }
        
        private static void CreateConfigPages()
        {
            // Page 1: General + QoL
            var page1 = CreatePage("Page1");
            float yPos = -20;
            
            CreateSectionHeader("General Settings", yPos, page1);
            yPos -= 40;
            CreateToggle("Pause On Focus Loss", "PauseOnFocusLoss", Plugin.EnablePauseOnFocusLoss, yPos, page1);
            yPos -= 40;
            
            CreateSectionHeader("Quality of Life", yPos, page1);
            yPos -= 40;
            CreateToggle("Enable BGM Info Display", "BgmInfo", Plugin.EnableBgmInfo, yPos, page1);
            yPos -= 35;
            CreateToggle("Show BGM Once Per Session", "ShowOncePerSession", Plugin.ShowOncePerSession, yPos, page1);
            yPos -= 35;
            CreateToggle("Enable Movement Speed Multiplier", "MovementMultiplier", Plugin.EnableMovementMultiplier, yPos, page1);
            yPos -= 35;
            CreateSlider("Movement Speed", "MovementSpeed", Plugin.MovementSpeedMultiplier, 0.5f, 3.0f, yPos, page1);
            yPos -= 50;
            
            CreateSectionHeader("Difficulty - General", yPos, page1);
            yPos -= 40;
            CreateToggle("Remove Full Heal on Level Up", "NoHealOnLevelUp", Plugin.EnableNoHealOnLevelUp, yPos, page1);
            yPos -= 35;
            CreateToggle("Enable Aggro Range Multiplier (EXPERIMENTAL)", "AggroRangeMultiplier", Plugin.EnableAggroRangeMultiplier, yPos, page1);
            yPos -= 35;
            CreateSlider("Aggro Range", "AggroRange", Plugin.AggroRangeMultiplier, 0.1f, 2.0f, yPos, page1);
            
            // Page 2: Formation + Chain
            var page2 = CreatePage("Page2");
            yPos = -20;
            
            CreateSectionHeader("Difficulty - Formation Bonuses", yPos, page2);
            yPos -= 40;
            CreateToggle("Reset Every Battle", "FormationBonusReset", Plugin.EnableFormationBonusReset, yPos, page2);
            yPos -= 35;
            CreateToggle("Halve Bonus Effects", "FormationBonusHalved", Plugin.EnableFormationBonusHalved, yPos, page2);
            yPos -= 35;
            CreateToggle("Harder to Acquire", "FormationBonusHarder", Plugin.EnableFormationBonusHarder, yPos, page2);
            yPos -= 35;
            CreateSlider("Point Gain", "FormationBonusPoint", Plugin.FormationBonusPointMultiplier, 0.1f, 1.0f, yPos, page2);
            yPos -= 45;
            CreateToggle("Disable Completely", "FormationBonusDisable", Plugin.EnableFormationBonusDisable, yPos, page2);
            yPos -= 40;
            
            CreateSectionHeader("Difficulty - Chain Battles", yPos, page2);
            yPos -= 40;
            CreateToggle("Reduce Chain Bonuses", "ChainBattleNerf", Plugin.EnableChainBattleNerf, yPos, page2);
            yPos -= 35;
            CreateSlider("Chain Bonus", "ChainBattleBonus", Plugin.ChainBattleBonusMultiplier, 0.0f, 1.0f, yPos, page2);
            yPos -= 45;
            CreateToggle("Disable Chain Bonuses Completely", "ChainBattleDisable", Plugin.EnableChainBattleDisable, yPos, page2);
            
            // Page 3: Mission + Debug
            var page3 = CreatePage("Page3");
            yPos = -20;
            
            CreateSectionHeader("Difficulty - Mission Rewards", yPos, page3);
            yPos -= 40;
            CreateToggle("Reduce Mission Rewards", "MissionRewardNerf", Plugin.EnableMissionRewardNerf, yPos, page3);
            yPos -= 35;
            CreateToggle("Nerf ALL Missions", "NerfAllMissions", Plugin.NerfAllMissionRewards, yPos, page3);
            yPos -= 35;
            CreateSlider("Reward Multiplier", "MissionReward", Plugin.MissionRewardMultiplier, 0.1f, 1.0f, yPos, page3);
            yPos -= 50;
            
            CreateSectionHeader("Debug", yPos, page3);
            yPos -= 40;
            CreateToggle("Enable Debug Logging", "DebugMode", Plugin.EnableDebugMode, yPos, page3);
            
            // Show first page
            ShowPage(0);
        }
        
        private static GameObject CreatePage(string name)
        {
            var page = new GameObject(name);
            page.transform.SetParent(_contentPanel.transform, false);
            
            var rect = page.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            
            _pageContents.Add(page);
            page.SetActive(false);
            
            return page;
        }
        
        private static void CreateSectionHeader(string text, float yPos, GameObject parent)
        {
            try
            {
                var headerObj = new GameObject($"Header_{text}");
                headerObj.transform.SetParent(parent.transform, false);
                
                var headerRect = headerObj.AddComponent<RectTransform>();
                headerRect.sizeDelta = new Vector2(800, 30);
                headerRect.anchoredPosition = new Vector2(0, yPos);
                
                var headerText = headerObj.AddComponent<TextMeshProUGUI>();
                headerText.text = $"━━━ {text} ━━━";
                headerText.fontSize = 20;
                headerText.alignment = TextAlignmentOptions.Center;
                headerText.color = Color.white;
                
                // Add text outline for better visibility
                headerText.fontStyle = FontStyles.Bold;
                headerText.outlineWidth = 0.2f;
                headerText.outlineColor = Color.black;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in CreateSectionHeader for {text}: {ex}");
            }
        }
        
        private static void CreateToggle(string label, string key, ConfigEntry<bool> config, float yPos, GameObject parent)
        {
            try
            {
                var toggleObj = new GameObject($"Toggle_{key}");
                toggleObj.transform.SetParent(parent.transform, false);
                
                // Add RectTransform component first
                var toggleRect = toggleObj.AddComponent<RectTransform>();
                toggleRect.sizeDelta = new Vector2(800, 30);
                toggleRect.anchoredPosition = new Vector2(0, yPos);
                
                // Toggle component
                var toggle = toggleObj.AddComponent<Toggle>();
                
                // Background
                var bgObj = new GameObject("Background");
                bgObj.transform.SetParent(toggleObj.transform, false);
                var bgImage = bgObj.AddComponent<Image>();
                bgImage.color = new Color(0.3f, 0.3f, 0.3f);
                var bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.sizeDelta = new Vector2(30, 30);
                bgRect.anchoredPosition = new Vector2(-380, 0);
                
                // Checkmark
                var checkObj = new GameObject("Checkmark");
                checkObj.transform.SetParent(bgObj.transform, false);
                var checkImage = checkObj.AddComponent<Image>();
                checkImage.color = Color.green;
                var checkRect = checkObj.GetComponent<RectTransform>();
                checkRect.sizeDelta = new Vector2(20, 20);
                checkRect.anchoredPosition = Vector2.zero;
                
                toggle.targetGraphic = bgImage;
                toggle.graphic = checkImage;
                toggle.isOn = config.Value;
                toggle.interactable = false;
                
                // Label
                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(toggleObj.transform, false);
                var labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(700, 30);
                labelRect.anchoredPosition = new Vector2(40, 0);
                
                var labelText = labelObj.AddComponent<TextMeshProUGUI>();
                string keyHint = GetKeyHint(key);
                labelText.text = $"{keyHint} {label}";
                labelText.fontSize = 16;
                labelText.color = Color.white;
                labelText.alignment = TextAlignmentOptions.Left;
                labelText.outlineWidth = 0.2f;
                labelText.outlineColor = Color.black;
                
                _toggles[key] = toggleObj;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in CreateToggle for {key}: {ex}");
            }
        }
        
        private static string GetKeyHint(string key)
        {
            switch (key)
            {
                case "PauseOnFocusLoss": return "[1]";
                case "BgmInfo": return "[2]";
                case "ShowOncePerSession": return "[3]";
                case "MovementMultiplier": return "[4]";
                case "NoHealOnLevelUp": return "[5]";
                case "AggroRangeMultiplier": return "[6]";
                case "FormationBonusReset": return "[7]";
                case "FormationBonusHalved": return "[8]";
                case "FormationBonusHarder": return "[9]";
                case "FormationBonusDisable": return "[0]";
                case "ChainBattleNerf": return "[Q]";
                case "ChainBattleDisable": return "[W]";
                case "MissionRewardNerf": return "[E]";
                case "NerfAllMissions": return "[R]";
                case "DebugMode": return "[T]";
                default: return "";
            }
        }
        
        private static void CreateSlider(string label, string key, ConfigEntry<float> config, float min, float max, float yPos, GameObject parent)
        {
            try
            {
                var sliderObj = new GameObject($"Slider_{key}");
                sliderObj.transform.SetParent(parent.transform, false);
                
                var sliderRect = sliderObj.AddComponent<RectTransform>();
                sliderRect.sizeDelta = new Vector2(800, 40);
                sliderRect.anchoredPosition = new Vector2(0, yPos);
                
                // Label
                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(sliderObj.transform, false);
                var labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(300, 30);
                labelRect.anchoredPosition = new Vector2(-250, 0);
                
                var labelText = labelObj.AddComponent<TextMeshProUGUI>();
                labelText.text = $"{label}: {config.Value:F2}x";
                labelText.fontSize = 16;
                labelText.color = Color.white;
                labelText.alignment = TextAlignmentOptions.Left;
                labelText.outlineWidth = 0.2f;
                labelText.outlineColor = Color.black;
                
                // Slider
                var slider = sliderObj.AddComponent<Slider>();
                
                // Background
                var bgObj = new GameObject("Background");
                bgObj.transform.SetParent(sliderObj.transform, false);
                var bgImage = bgObj.AddComponent<Image>();
                bgImage.color = new Color(0.3f, 0.3f, 0.3f);
                var bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.sizeDelta = new Vector2(400, 10);
                bgRect.anchoredPosition = new Vector2(150, 0);
                
                // Fill Area
                var fillAreaObj = new GameObject("Fill Area");
                fillAreaObj.transform.SetParent(bgObj.transform, false);
                var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = new Vector2(0, 0);
                fillAreaRect.anchorMax = new Vector2(1, 1);
                fillAreaRect.sizeDelta = Vector2.zero;
                fillAreaRect.anchoredPosition = Vector2.zero;
                
                // Fill
                var fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(fillAreaObj.transform, false);
                var fillRect = fillObj.AddComponent<RectTransform>();
                fillRect.anchorMin = new Vector2(0, 0);
                fillRect.anchorMax = new Vector2(0, 1);
                fillRect.pivot = new Vector2(0, 0.5f);
                fillRect.sizeDelta = new Vector2(10, 0);
                fillRect.anchoredPosition = new Vector2(0, 0);
                var fillImage = fillObj.AddComponent<Image>();
                fillImage.color = new Color(0.2f, 0.7f, 0.2f);
                
                // Handle Slide Area
                var handleAreaObj = new GameObject("Handle Slide Area");
                handleAreaObj.transform.SetParent(bgObj.transform, false);
                var handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
                handleAreaRect.anchorMin = new Vector2(0, 0);
                handleAreaRect.anchorMax = new Vector2(1, 1);
                handleAreaRect.sizeDelta = new Vector2(-20, 0);
                handleAreaRect.anchoredPosition = Vector2.zero;
                
                // Handle
                var handleObj = new GameObject("Handle");
                handleObj.transform.SetParent(handleAreaObj.transform, false);
                var handleRect = handleObj.AddComponent<RectTransform>();
                handleRect.sizeDelta = new Vector2(20, 20);
                handleRect.anchoredPosition = Vector2.zero;
                var handleImage = handleObj.AddComponent<Image>();
                handleImage.color = Color.white;
                
                slider.fillRect = fillRect;
                slider.handleRect = handleRect;
                slider.targetGraphic = handleImage;
                slider.direction = Slider.Direction.LeftToRight;
                slider.minValue = min;
                slider.maxValue = max;
                slider.value = config.Value;
                slider.interactable = false;
                
                _sliders[key] = sliderObj;
                _sliderLabels[key] = labelText;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in CreateSlider for {key}: {ex}");
            }
        }
        
        private static void UpdateUIValues()
        {
            // Update slider visibility based on toggle states
            if (_sliders.ContainsKey("MovementSpeed"))
                _sliders["MovementSpeed"].SetActive(Plugin.EnableMovementMultiplier.Value);
            
            if (_sliders.ContainsKey("AggroRange"))
                _sliders["AggroRange"].SetActive(Plugin.EnableAggroRangeMultiplier.Value);
            
            if (_sliders.ContainsKey("FormationBonusPoint"))
                _sliders["FormationBonusPoint"].SetActive(Plugin.EnableFormationBonusHarder.Value);
            
            if (_sliders.ContainsKey("ChainBattleBonus"))
                _sliders["ChainBattleBonus"].SetActive(Plugin.EnableChainBattleNerf.Value);
            
            if (_sliders.ContainsKey("MissionReward"))
                _sliders["MissionReward"].SetActive(Plugin.EnableMissionRewardNerf.Value);
        }
        
        public static void Cleanup()
        {
            _configWatcher?.Dispose();
            if (_window != null)
            {
                UnityEngine.Object.Destroy(_window);
                _window = null;
            }
            _toggles.Clear();
            _sliders.Clear();
            _sliderLabels.Clear();
            _optionKeys.Clear();
            _pageContents.Clear();
        }
    }
}