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
        private static TMP_FontAsset _uiFont;

        public static void Initialize()
        {
            try
            {
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
                if (Time.time - _lastReloadTime < RELOAD_COOLDOWN) return;
                _lastReloadTime = Time.time;
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

            if (kb.pageDownKey.wasPressedThisFrame || kb.rightBracketKey.wasPressedThisFrame) PreviousPage();
            if (kb.pageUpKey.wasPressedThisFrame || kb.leftBracketKey.wasPressedThisFrame) NextPage();

            if (kb.digit1Key.wasPressedThisFrame) ToggleOption("PauseOnFocusLoss");
            if (kb.digit2Key.wasPressedThisFrame) ToggleOption("BgmInfo");
            if (kb.digit3Key.wasPressedThisFrame) ToggleOption("ShowOncePerSession");
            if (kb.digit4Key.wasPressedThisFrame) ToggleOption("MovementMultiplier");
            if (kb.digit5Key.wasPressedThisFrame) ToggleOption("NoHealOnLevelUp");
            if (kb.digit7Key.wasPressedThisFrame) ToggleOption("FormationBonusReset");
            if (kb.digit8Key.wasPressedThisFrame) ToggleOption("FormationBonusHalved");
            if (kb.digit9Key.wasPressedThisFrame) ToggleOption("FormationBonusHarder");
            if (kb.digit0Key.wasPressedThisFrame) ToggleOption("FormationBonusDisable");
            if (kb.qKey.wasPressedThisFrame) ToggleOption("ChainBattleNerf");
            if (kb.wKey.wasPressedThisFrame) ToggleOption("ChainBattleDisable");
            if (kb.eKey.wasPressedThisFrame) ToggleOption("MissionRewardNerf");
            if (kb.rKey.wasPressedThisFrame) ToggleOption("NerfAllMissions");
            if (kb.tKey.wasPressedThisFrame) ToggleOption("DebugMode");

            if (_optionKeys.Count > 0)
            {
                if (kb.leftArrowKey.wasPressedThisFrame) AdjustSlider(-0.05f);
                if (kb.rightArrowKey.wasPressedThisFrame) AdjustSlider(0.05f);
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
            foreach (var page in _pageContents)
            {
                if (page != null) page.SetActive(false);
            }

            if (pageIndex >= 0 && pageIndex < _pageContents.Count && _pageContents[pageIndex] != null)
            {
                _pageContents[pageIndex].SetActive(true);
            }

            if (_pageIndicator != null)
            {
                _pageIndicator.text = $"Page {_currentPage + 1} / {_totalPages} - Use [Page Up/Down] or [ ] to navigate";
            }

            _selectedIndex = 0;
            _optionKeys.Clear();

            if (pageIndex >= 0 && pageIndex < _pageContents.Count && _pageContents[pageIndex] != null)
            {
                foreach (var kvp in _sliders)
                {
                    if (kvp.Value != null && kvp.Value.activeInHierarchy && kvp.Value.transform.IsChildOf(_pageContents[pageIndex].transform))
                    {
                        _optionKeys.Add(kvp.Key);
                    }
                }
            }
            HighlightSelectedSlider();
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
                        UpdateSliderVisual(key, Plugin.MovementSpeedMultiplier.Value);
                        break;
                    case "FormationBonusPoint":
                        Plugin.FormationBonusPointMultiplier.Value = Mathf.Clamp(Plugin.FormationBonusPointMultiplier.Value + delta, 0.1f, 1.0f);
                        UpdateSliderVisual(key, Plugin.FormationBonusPointMultiplier.Value);
                        break;
                    case "ChainBattleBonus":
                        Plugin.ChainBattleBonusMultiplier.Value = Mathf.Clamp(Plugin.ChainBattleBonusMultiplier.Value + delta, 0.0f, 1.0f);
                        UpdateSliderVisual(key, Plugin.ChainBattleBonusMultiplier.Value);
                        break;
                    case "MissionReward":
                        Plugin.MissionRewardMultiplier.Value = Mathf.Clamp(Plugin.MissionRewardMultiplier.Value + delta, 0.1f, 1.0f);
                        UpdateSliderVisual(key, Plugin.MissionRewardMultiplier.Value);
                        break;
                }
            }
        }

        private static void UpdateSliderVisual(string key, float value)
        {
            if (_sliders.ContainsKey(key) && _sliders[key] != null)
            {
                _sliders[key].GetComponentInChildren<Slider>().value = value;
                _sliderLabels[key].text = $"{GetSliderLabel(key)}: {value:F2}x";
            }
        }

        private static string GetSliderLabel(string key)
        {
            switch (key)
            {
                case "MovementSpeed": return "Movement Speed";
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
                if (_uiFont == null)
                {
                    var existingText = UnityEngine.Object.FindObjectOfType<TextMeshProUGUI>();
                    if (existingText != null && existingText.font != null)
                    {
                        _uiFont = existingText.font;
                        Plugin.Logger.LogInfo($"Successfully cached UI Font: {_uiFont.name}");
                    }
                    else
                    {
                        Plugin.Logger.LogError("Could not find an active TextMeshProUGUI object to source a font from. UI cannot be created.");
                        return;
                    }
                }

                _toggles.Clear();
                _sliders.Clear();
                _sliderLabels.Clear();
                _optionKeys.Clear();
                _pageContents.Clear();
                _selectedIndex = 0;
                _currentPage = 0;

                _window = new GameObject("RuntimeConfigWindow");
                var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    Plugin.Logger.LogError("Could not find a Canvas to attach the config window to!");
                    return;
                }
                _window.transform.SetParent(canvas.transform, false);

                var background = _window.AddComponent<Image>();
                background.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

                var rect = _window.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(900, 700);
                rect.anchoredPosition = Vector2.zero;

                var titleObject = new GameObject("ConfigTitle");
                titleObject.transform.SetParent(_window.transform, false);
                var titleText = titleObject.AddComponent<TextMeshProUGUI>();
                titleText.font = _uiFont;
                titleText.text = "SO2R QoL Patches - Runtime Configuration";
                titleText.fontSize = 28;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = Color.white;
                var titleRect = titleObject.GetComponent<RectTransform>();
                titleRect.sizeDelta = new Vector2(900, 50);
                titleRect.anchoredPosition = new Vector2(0, 320);

                var instructionObject = new GameObject("Instructions");
                instructionObject.transform.SetParent(_window.transform, false);
                var instructionText = instructionObject.AddComponent<TextMeshProUGUI>();
                instructionText.font = _uiFont;
                instructionText.text = "Press F9 to close. Use number keys to toggle options, arrow keys for sliders.";
                instructionText.fontSize = 16;
                instructionText.alignment = TextAlignmentOptions.Center;
                instructionText.color = new Color(0.8f, 0.8f, 0.8f);
                var instructionRect = instructionObject.GetComponent<RectTransform>();
                instructionRect.sizeDelta = new Vector2(900, 30);
                instructionRect.anchoredPosition = new Vector2(0, 280);

                var pageIndicatorObj = new GameObject("PageIndicator");
                pageIndicatorObj.transform.SetParent(_window.transform, false);
                _pageIndicator = pageIndicatorObj.AddComponent<TextMeshProUGUI>();
                _pageIndicator.font = _uiFont;
                _pageIndicator.fontSize = 18;
                _pageIndicator.alignment = TextAlignmentOptions.Center;
                _pageIndicator.color = Color.yellow;
                var pageRect = pageIndicatorObj.GetComponent<RectTransform>();
                pageRect.sizeDelta = new Vector2(900, 30);
                pageRect.anchoredPosition = new Vector2(0, -320);

                _contentPanel = new GameObject("ContentPanel");
                _contentPanel.transform.SetParent(_window.transform, false);
                _contentPanel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.5f);
                var contentRect = _contentPanel.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 0);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.offsetMin = new Vector2(25, 60);
                contentRect.offsetMax = new Vector2(-25, -100);

                CreateConfigPages();

                Plugin.Logger.LogInfo("Runtime config window created successfully.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in CreateConfigWindow: {ex}");
            }
        }
        
        private static void CreateConfigPages()
        {
            var page1 = CreatePage("Page1");
            CreateSectionHeader("General Settings", page1);
            var generalGrid = CreateGridContainer(page1);
            CreateToggle("Pause On Focus Loss", "PauseOnFocusLoss", Plugin.EnablePauseOnFocusLoss, generalGrid);
            CreateSectionHeader("Quality of Life", page1);
            var qolGrid = CreateGridContainer(page1);
            CreateToggle("Enable BGM Info Display", "BgmInfo", Plugin.EnableBgmInfo, qolGrid);
            CreateToggle("Show BGM Once Per Session", "ShowOncePerSession", Plugin.ShowOncePerSession, qolGrid);
            CreateToggle("Enable Movement Speed Multiplier", "MovementMultiplier", Plugin.EnableMovementMultiplier, qolGrid);
            CreateSlider("Movement Speed", "MovementSpeed", Plugin.MovementSpeedMultiplier, 0.5f, 3.0f, qolGrid);
            CreateSectionHeader("Difficulty - General", page1);
            var diffGeneralGrid = CreateGridContainer(page1);
            CreateToggle("Remove Full Heal on Level Up", "NoHealOnLevelUp", Plugin.EnableNoHealOnLevelUp, diffGeneralGrid);
            
            var page2 = CreatePage("Page2");
            CreateSectionHeader("Difficulty - Formation Bonuses", page2);
            var formationGrid = CreateGridContainer(page2);
            CreateToggle("Reset Every Battle", "FormationBonusReset", Plugin.EnableFormationBonusReset, formationGrid);
            CreateToggle("Halve Bonus Effects", "FormationBonusHalved", Plugin.EnableFormationBonusHalved, formationGrid);
            CreateToggle("Harder to Acquire", "FormationBonusHarder", Plugin.EnableFormationBonusHarder, formationGrid);
            CreateToggle("Disable Completely", "FormationBonusDisable", Plugin.EnableFormationBonusDisable, formationGrid);
            CreateSlider("Point Gain", "FormationBonusPoint", Plugin.FormationBonusPointMultiplier, 0.1f, 1.0f, formationGrid);
            CreateSectionHeader("Difficulty - Chain Battles", page2);
            var chainGrid = CreateGridContainer(page2);
            CreateToggle("Reduce Chain Bonuses", "ChainBattleNerf", Plugin.EnableChainBattleNerf, chainGrid);
            CreateToggle("Disable Chain Bonuses", "ChainBattleDisable", Plugin.EnableChainBattleDisable, chainGrid);
            CreateSlider("Chain Bonus", "ChainBattleBonus", Plugin.ChainBattleBonusMultiplier, 0.0f, 1.0f, chainGrid);

            var page3 = CreatePage("Page3");
            CreateSectionHeader("Difficulty - Mission Rewards", page3);
            var missionGrid = CreateGridContainer(page3);
            CreateToggle("Reduce Mission Rewards", "MissionRewardNerf", Plugin.EnableMissionRewardNerf, missionGrid);
            CreateToggle("Nerf ALL Missions", "NerfAllMissions", Plugin.NerfAllMissionRewards, missionGrid);
            CreateSlider("Reward Multiplier", "MissionReward", Plugin.MissionRewardMultiplier, 0.1f, 1.0f, missionGrid);
            CreateSectionHeader("Debug", page3);
            var debugGrid = CreateGridContainer(page3);
            CreateToggle("Enable Debug Logging", "DebugMode", Plugin.EnableDebugMode, debugGrid);

            ShowPage(0);
        }
        
        private static GameObject CreateGridContainer(GameObject parent)
        {
            var gridObj = new GameObject("GridContainer");
            gridObj.transform.SetParent(parent.transform, false);
            var gridLayout = gridObj.AddComponent<GridLayoutGroup>();
            var padding = new RectOffset();
            padding.left = 10; padding.right = 10; padding.top = 5; padding.bottom = 15;
            gridLayout.padding = padding;
            gridLayout.cellSize = new Vector2(380, 32); // Made cells shorter
            gridLayout.spacing = new Vector2(15, 5); // Reduced vertical spacing
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            var fitter = gridObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return gridObj;
        }
        
        private static GameObject CreatePage(string name)
        {
            var pageObj = new GameObject(name);
            pageObj.transform.SetParent(_contentPanel.transform, false);
            var pageRect = pageObj.AddComponent<RectTransform>();
            pageRect.anchorMin = Vector2.zero;
            pageRect.anchorMax = Vector2.one;
            pageRect.sizeDelta = Vector2.zero;
            
            var vLayout = pageObj.AddComponent<VerticalLayoutGroup>();
            var padding = new RectOffset(); 
            padding.left = 15; padding.right = 15; padding.top = 15; padding.bottom = 15;
            vLayout.padding = padding;
            vLayout.spacing = 2; 
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = false;
            vLayout.childControlHeight = false;

            _pageContents.Add(pageObj);
            pageObj.SetActive(false);
            return pageObj;
        }

        private static void CreateSectionHeader(string text, GameObject parent)
        {
            try
            {
                var headerObj = new GameObject($"Header_{text}");
                headerObj.transform.SetParent(parent.transform, false);
                headerObj.AddComponent<LayoutElement>().minHeight = 30;
                var headerText = headerObj.AddComponent<TextMeshProUGUI>();
                headerText.font = _uiFont;
                headerText.text = $"━━━ {text} ━━━";
                headerText.fontSize = 20;
                headerText.alignment = TextAlignmentOptions.Center;
                headerText.color = Color.white;
                headerText.fontStyle = FontStyles.Bold;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in CreateSectionHeader for {text}: {ex}");
            }
        }

        private static void CreateToggle(string label, string key, ConfigEntry<bool> config, GameObject parent)
        {
            try
            {
                var toggleObj = new GameObject($"Toggle_{key}");
                toggleObj.transform.SetParent(parent.transform, false);
                var hLayout = toggleObj.AddComponent<HorizontalLayoutGroup>();
                hLayout.childAlignment = TextAnchor.MiddleLeft;
                hLayout.spacing = 10;
                
                var toggle = toggleObj.AddComponent<Toggle>();

                var bgObj = new GameObject("Background");
                bgObj.transform.SetParent(toggleObj.transform, false);
                bgObj.AddComponent<LayoutElement>().preferredWidth = 24;
                var bgImage = bgObj.AddComponent<Image>();
                bgImage.color = new Color(0.3f, 0.3f, 0.3f);
                var bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.sizeDelta = new Vector2(24, 24);

                var checkObj = new GameObject("Checkmark");
                checkObj.transform.SetParent(bgObj.transform, false);
                var checkImage = checkObj.AddComponent<Image>();
                checkImage.color = Color.green;
                var checkRect = checkObj.GetComponent<RectTransform>();
                checkRect.sizeDelta = new Vector2(16, 16);
                checkRect.anchoredPosition = Vector2.zero;

                toggle.targetGraphic = bgImage;
                toggle.graphic = checkImage;
                toggle.isOn = config.Value;
                toggle.interactable = false;

                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(toggleObj.transform, false);
                var labelLayout = labelObj.AddComponent<LayoutElement>();
                labelLayout.flexibleWidth = 1;
                var labelText = labelObj.AddComponent<TextMeshProUGUI>();
                labelText.font = _uiFont;
                labelText.text = $"{GetKeyHint(key)} {label}";
                labelText.fontSize = 16;
                labelText.color = Color.white;
                labelText.alignment = TextAlignmentOptions.Left;

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

        // Final reworked slider - now places label and slider side-by-side
        private static void CreateSlider(string label, string key, ConfigEntry<float> config, float min, float max, GameObject parent)
        {
            try
            {
                var sliderCell = new GameObject($"Slider_{key}");
                sliderCell.transform.SetParent(parent.transform, false);
                var hLayout = sliderCell.AddComponent<HorizontalLayoutGroup>();
                hLayout.childAlignment = TextAnchor.MiddleLeft;
                hLayout.spacing = 8;
                
                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(sliderCell.transform, false);
                labelObj.AddComponent<LayoutElement>().minWidth = 160;
                var labelText = labelObj.AddComponent<TextMeshProUGUI>();
                labelText.font = _uiFont;
                labelText.text = $"{GetSliderLabel(key)}: {config.Value:F2}x";
                labelText.fontSize = 16;
                labelText.color = Color.white;
                labelText.alignment = TextAlignmentOptions.Left;

                var sliderObj = new GameObject("Slider");
                sliderObj.transform.SetParent(sliderCell.transform, false);
                sliderObj.AddComponent<LayoutElement>().flexibleWidth = 1;
                
                var slider = sliderObj.AddComponent<Slider>();

                var bgObj = new GameObject("Background");
                bgObj.transform.SetParent(sliderObj.transform, false);
                bgObj.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 1f);
                var bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0, 0.5f);
                bgRect.anchorMax = new Vector2(1, 0.5f);
                bgRect.pivot = new Vector2(0.5f, 0.5f);
                bgRect.sizeDelta = new Vector2(0, 4);

                var fillAreaObj = new GameObject("Fill Area");
                fillAreaObj.transform.SetParent(sliderObj.transform, false);
                var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = new Vector2(0, 0.5f);
                fillAreaRect.anchorMax = new Vector2(1, 0.5f);
                fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
                fillAreaRect.sizeDelta = new Vector2(0, 4);

                var fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(fillAreaObj.transform, false);
                fillObj.AddComponent<Image>().color = new Color(0.2f, 0.7f, 0.2f);
                var fillRect = fillObj.GetComponent<RectTransform>();
                fillRect.anchorMin = new Vector2(0, 0);
                fillRect.anchorMax = new Vector2(0, 1);
                fillRect.pivot = new Vector2(0, 0.5f);
                fillRect.sizeDelta = Vector2.zero;
                slider.fillRect = fillRect;

                var handleAreaObj = new GameObject("Handle Slide Area");
                handleAreaObj.transform.SetParent(sliderObj.transform, false);
                var handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
                handleAreaRect.anchorMin = Vector2.zero;
                handleAreaRect.anchorMax = Vector2.one;
                handleAreaRect.offsetMin = new Vector2(5, 0);
                handleAreaRect.offsetMax = new Vector2(-5, 0);

                var handleObj = new GameObject("Handle");
                handleObj.transform.SetParent(handleAreaObj.transform, false);
                handleObj.AddComponent<Image>().color = Color.white;
                var handleRect = handleObj.GetComponent<RectTransform>();
                handleRect.sizeDelta = new Vector2(10, 18);
                slider.handleRect = handleRect;
                
                slider.targetGraphic = handleObj.GetComponent<Image>();
                slider.minValue = min;
                slider.maxValue = max;
                slider.value = config.Value;
                slider.interactable = false;

                _sliders[key] = sliderCell;
                _sliderLabels[key] = labelText;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in CreateSlider for {key}: {ex}");
            }
        }
        
        private static void UpdateUIValues()
        {
            if (_sliders.ContainsKey("MovementSpeed")) _sliders["MovementSpeed"].SetActive(Plugin.EnableMovementMultiplier.Value);
            if (_sliders.ContainsKey("FormationBonusPoint")) _sliders["FormationBonusPoint"].SetActive(Plugin.EnableFormationBonusHarder.Value);
            if (_sliders.ContainsKey("ChainBattleBonus")) _sliders["ChainBattleBonus"].SetActive(Plugin.EnableChainBattleNerf.Value);
            if (_sliders.ContainsKey("MissionReward")) _sliders["MissionReward"].SetActive(Plugin.EnableMissionRewardNerf.Value);
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