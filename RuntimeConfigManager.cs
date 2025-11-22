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
        
        public static bool _isVisible { get; private set; } = false;
        private static GameObject _window;
        private static GameObject _contentPanel;
        private static GameObject _persistentCanvas;
        private static float _lastReloadTime = 0f;
        private const float RELOAD_COOLDOWN = 1f;
        private static Dictionary<string, GameObject> _toggles = new Dictionary<string, GameObject>();
        private static Dictionary<string, GameObject> _sliders = new Dictionary<string, GameObject>();
        private static Dictionary<string, TextMeshProUGUI> _sliderLabels = new Dictionary<string, TextMeshProUGUI>();
        private static int _selectedIndex = 0;
        private static List<string> _optionKeys = new List<string>();
        private static float _originalTimeScale = 1f;
        private static bool _pausedByConfig = false;

        // Pagination
        private static int _currentPage = 0;
        private static int _totalPages = 3;
        private static TextMeshProUGUI _pageIndicator;
        private static List<GameObject> _pageContents = new List<GameObject>();
        
        // Font caching
        private static TMP_FontAsset _cachedFont;
        private static readonly string[] _fallbackFontNames = { "LiberationSans SDF", "Arial SDF", "ARIAL SDF" };

        public static void Initialize()
        {
            try
            {
                var configFile = Path.Combine(Paths.ConfigPath, "com.zorkats.so2r_qol.cfg");
                var configDir = Path.GetDirectoryName(configFile);
                

            // Create persistent canvas immediately
                CreatePersistentCanvas();

                Plugin.Logger.LogInfo("Runtime configuration manager initialized");
                Plugin.Logger.LogInfo("Press F9 in-game to open the configuration menu");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Failed to initialize runtime config manager: {ex}");
            }
        }
        
        private static void InitializeUIComponents()
        {
            try
            {
                // Force initialization of all toggles by directly manipulating their state
                foreach (var kvp in _toggles)
                {
                    if (kvp.Value != null && kvp.Value)
                    {
                        var toggle = kvp.Value.GetComponent<Toggle>();
                        if (toggle != null)
                        {
                            // Force Unity to initialize internal state by accessing properties
                            bool originalValue = toggle.isOn;
                            
                            // Access the graphic components to ensure they're initialized
                            if (toggle.graphic != null)
                            {
                                toggle.graphic.enabled = toggle.graphic.enabled;
                            }
                            if (toggle.targetGraphic != null)
                            {
                                toggle.targetGraphic.enabled = toggle.targetGraphic.enabled;
                            }
                            
                            // Ensure the toggle's internal state is properly set
                            toggle.SetIsOnWithoutNotify(originalValue);
                        }
                    }
                }

                // Force initialization of all sliders
                foreach (var kvp in _sliders)
                {
                    if (kvp.Value != null && kvp.Value)
                    {
                        var slider = kvp.Value.GetComponentInChildren<Slider>();
                        if (slider != null)
                        {
                            // Force Unity to initialize internal state
                            float originalValue = slider.value;
                            
                            // Access the graphic components to ensure they're initialized
                            if (slider.fillRect != null)
                            {
                                slider.fillRect.anchorMax = slider.fillRect.anchorMax;
                            }
                            if (slider.handleRect != null)
                            {
                                slider.handleRect.anchoredPosition = slider.handleRect.anchoredPosition;
                            }
                            if (slider.targetGraphic != null)
                            {
                                slider.targetGraphic.enabled = slider.targetGraphic.enabled;
                            }
                            
                            // Ensure the slider's internal state is properly set
                            slider.SetValueWithoutNotify(originalValue);
                        }
                    }
                }

                Plugin.Logger.LogInfo("UI components initialized successfully");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error initializing UI components: {ex}");
            }
        }
        
        private static void CreatePersistentCanvas()
        {
            try
            {
                // Create a persistent canvas that won't be destroyed during scene transitions
                var canvasObj = new GameObject("PersistentConfigCanvas");
                UnityEngine.Object.DontDestroyOnLoad(canvasObj);
                
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // Ensure it's on top
                
                var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                
                canvasObj.AddComponent<GraphicRaycaster>();
                
                _persistentCanvas = canvasObj;
                Plugin.Logger.LogInfo("Persistent canvas created successfully");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Failed to create persistent canvas: {ex}");
            }
        }

        private static TMP_FontAsset GetSafeFont()
        {
            // Return cached font if it's still valid
            if (_cachedFont != null && _cachedFont)
            {
                return _cachedFont;
            }

            try
            {
                // Try to find an existing font from active UI
                var existingText = UnityEngine.Object.FindObjectOfType<TextMeshProUGUI>();
                if (existingText != null && existingText.font != null)
                {
                    _cachedFont = existingText.font;
                    Plugin.Logger.LogInfo($"Found and cached UI Font: {_cachedFont.name}");
                    return _cachedFont;
                }

                // Try to load fallback fonts
                foreach (var fontName in _fallbackFontNames)
                {
                    var font = Resources.Load<TMP_FontAsset>(fontName);
                    if (font != null)
                    {
                        _cachedFont = font;
                        Plugin.Logger.LogInfo($"Loaded fallback font: {fontName}");
                        return _cachedFont;
                    }
                }
                
                Plugin.Logger.LogWarning("Could not find any suitable font, UI may not display correctly");
                return null;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error getting safe font: {ex}");
                return null;
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
                // Ensure persistent canvas exists
                if (_persistentCanvas == null || !_persistentCanvas)
                {
                    CreatePersistentCanvas();
                    if (_persistentCanvas == null) return; // Still failed, abort
                }
                if (Keyboard.current == null) return;

                if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    _isVisible = !_isVisible;
                    Plugin.Logger.LogInfo($"Runtime Config toggled. Visible: {_isVisible}");

                    // If window was destroyed or doesn't exist, recreate it
                    if (_isVisible && (_window == null || !_window))
                    {
                        CreateConfigWindow();
                    }

                    // Only proceed if window creation was successful
                    if (_window != null && _window)
                    {
                        _window.SetActive(_isVisible);

                        if (_isVisible)
                        {
                            UpdateUIValues();
                            ShowPage(_currentPage);
                            // Store original timescale to restore it later
                            if (!_pausedByConfig)
                            {
                                _originalTimeScale = Time.timeScale;
                                _pausedByConfig = true;
                            }
                            Time.timeScale = 0f;
                            Cursor.visible = true;
                            Cursor.lockState = CursorLockMode.None;
                        }
                        else
                        {
                            // Restore original timescale
                            if (_pausedByConfig)
                            {
                                Time.timeScale = _originalTimeScale;
                                _pausedByConfig = false;
                            }
                        }
                    }
                    else
                    {
                        Plugin.Logger.LogWarning("Failed to create config window");
                        _isVisible = false;
                    }
                }
                
                
                if (_isVisible && _window != null && _window)
                {
                    HandleInput();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in RuntimeConfigManager.Update: {ex}");
                // Reset timescale in case of error
                if (_pausedByConfig)
                {
                    Time.timeScale = _originalTimeScale;
                    _pausedByConfig = false;
                }
            }
        }

        private static void HandleInput()
        {
            try
            {
                var kb = Keyboard.current;
                if (kb == null) return;

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
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in HandleInput: {ex}");
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
            try
            {
                foreach (var page in _pageContents)
                {
                    if (page != null && page) page.SetActive(false);
                }

                if (pageIndex >= 0 && pageIndex < _pageContents.Count && 
                    _pageContents[pageIndex] != null && _pageContents[pageIndex])
                {
                    _pageContents[pageIndex].SetActive(true);
                }

                if (_pageIndicator != null && _pageIndicator)
                {
                    _pageIndicator.text = $"Page {_currentPage + 1} / {_totalPages} - Use [Page Up/Down] or [ ] to navigate";
                }

                _selectedIndex = 0;
                _optionKeys.Clear();

                if (pageIndex >= 0 && pageIndex < _pageContents.Count && 
                    _pageContents[pageIndex] != null && _pageContents[pageIndex])
                {
                    foreach (var kvp in _sliders)
                    {
                        if (kvp.Value != null && kvp.Value && kvp.Value.activeInHierarchy && 
                            kvp.Value.transform.IsChildOf(_pageContents[pageIndex].transform))
                        {
                            _optionKeys.Add(kvp.Key);
                        }
                    }
                }
                HighlightSelectedSlider();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in ShowPage: {ex}");
            }
        }

        private static void ToggleOption(string key)
        {
            try
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

                if (_toggles.ContainsKey(key) && _toggles[key] != null && _toggles[key])
                {
                    var toggle = _toggles[key].GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        toggle.isOn = GetToggleValue(key);
                    }
                }
                UpdateUIValues();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in ToggleOption for {key}: {ex}");
            }
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
            try
            {
                for (int i = 0; i < _optionKeys.Count; i++)
                {
                    var key = _optionKeys[i];
                    if (_sliderLabels.ContainsKey(key) && _sliderLabels[key] != null && _sliderLabels[key])
                    {
                        _sliderLabels[key].color = (i == _selectedIndex) ? Color.yellow : Color.white;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in HighlightSelectedSlider: {ex}");
            }
        }

        private static void AdjustSlider(float delta)
        {
            try
            {
                if (_selectedIndex >= 0 && _selectedIndex < _optionKeys.Count)
                {
                    var key = _optionKeys[_selectedIndex];
                    switch (key)
                    {
                        case "MovementSpeed":
                            Plugin.MovementSpeedMultiplier.Value = Mathf.Clamp(Plugin.MovementSpeedMultiplier.Value + delta, 1f, 4.0f);
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
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in AdjustSlider: {ex}");
            }
        }

        private static void UpdateSliderVisual(string key, float value)
        {
            try
            {
                if (_sliders.ContainsKey(key) && _sliders[key] != null && _sliders[key] &&
                    _sliderLabels.ContainsKey(key) && _sliderLabels[key] != null && _sliderLabels[key])
                {
                    var slider = _sliders[key].GetComponentInChildren<Slider>();
                    if (slider != null)
                    {
                        slider.value = value;
                    }
                    _sliderLabels[key].text = $"{GetSliderLabel(key)}: {value:F2}x";
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in UpdateSliderVisual for {key}: {ex}");
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
                // Ensure we have a valid persistent canvas
                if (_persistentCanvas == null || !_persistentCanvas)
                {
                    CreatePersistentCanvas();
                    if (_persistentCanvas == null)
                    {
                        Plugin.Logger.LogError("Cannot create config window: no persistent canvas available");
                        return;
                    }
                }

                // Get a safe font reference
                var currentFont = GetSafeFont();
                if (currentFont == null)
                {
                    Plugin.Logger.LogError("Cannot create config window: no valid font available");
                    return;
                }

                // Clear existing references
                _toggles.Clear();
                _sliders.Clear();
                _sliderLabels.Clear();
                _optionKeys.Clear();
                _pageContents.Clear();
                _selectedIndex = 0;
                _currentPage = 0;

                _window = new GameObject("RuntimeConfigWindow");
                _window.transform.SetParent(_persistentCanvas.transform, false);

                var background = _window.AddComponent<Image>();
                background.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

                var rect = _window.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(900, 700);
                rect.anchoredPosition = Vector2.zero;

                var titleObject = new GameObject("ConfigTitle");
                titleObject.transform.SetParent(_window.transform, false);
                var titleText = titleObject.AddComponent<TextMeshProUGUI>();
                titleText.font = currentFont;
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
                instructionText.font = currentFont;
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
                _pageIndicator.font = currentFont;
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

                CreateConfigPages(currentFont);
                InitializeUIComponents();

                Plugin.Logger.LogInfo("Runtime config window created successfully.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in CreateConfigWindow: {ex}");
            }
        }
        
        private static void CreateConfigPages(TMP_FontAsset font)
        {
            try
            {
                var page1 = CreatePage("Page1");
                CreateSectionHeader("General Settings", page1, font);
                var generalGrid = CreateGridContainer(page1);
                CreateToggle("Pause On Focus Loss", "PauseOnFocusLoss", Plugin.EnablePauseOnFocusLoss, generalGrid, font);
                CreateSectionHeader("Quality of Life", page1, font);
                var qolGrid = CreateGridContainer(page1);
                CreateToggle("Enable BGM Info Display", "BgmInfo", Plugin.EnableBgmInfo, qolGrid, font);
                CreateToggle("Show BGM Once Per Session", "ShowOncePerSession", Plugin.ShowOncePerSession, qolGrid, font);
                CreateToggle("Enable Movement Speed Multiplier", "MovementMultiplier", Plugin.EnableMovementMultiplier, qolGrid, font);
                CreateSlider("Movement Speed", "MovementSpeed", Plugin.MovementSpeedMultiplier, 1f, 4.0f, qolGrid, font);
                CreateSectionHeader("Difficulty - General", page1, font);
                var diffGeneralGrid = CreateGridContainer(page1);
                CreateToggle("Remove Full Heal on Level Up", "NoHealOnLevelUp", Plugin.EnableNoHealOnLevelUp, diffGeneralGrid, font);
                
                var page2 = CreatePage("Page2");
                CreateSectionHeader("Difficulty - Formation Bonuses", page2, font);
                var formationGrid = CreateGridContainer(page2);
                CreateToggle("Reset Every Battle", "FormationBonusReset", Plugin.EnableFormationBonusReset, formationGrid, font);
                CreateToggle("Halve Bonus Effects", "FormationBonusHalved", Plugin.EnableFormationBonusHalved, formationGrid, font);
                CreateToggle("Harder to Acquire", "FormationBonusHarder", Plugin.EnableFormationBonusHarder, formationGrid, font);
                CreateToggle("Disable Completely", "FormationBonusDisable", Plugin.EnableFormationBonusDisable, formationGrid, font);
                CreateSlider("Point Gain", "FormationBonusPoint", Plugin.FormationBonusPointMultiplier, 0.1f, 1.0f, formationGrid, font);
                CreateSectionHeader("Difficulty - Chain Battles", page2, font);
                var chainGrid = CreateGridContainer(page2);
                CreateToggle("Reduce Chain Bonuses", "ChainBattleNerf", Plugin.EnableChainBattleNerf, chainGrid, font);
                CreateToggle("Disable Chain Bonuses", "ChainBattleDisable", Plugin.EnableChainBattleDisable, chainGrid, font);
                CreateSlider("Chain Bonus", "ChainBattleBonus", Plugin.ChainBattleBonusMultiplier, 0.0f, 1.0f, chainGrid, font);

                var page3 = CreatePage("Page3");
                CreateSectionHeader("Difficulty - Mission Rewards", page3, font);
                var missionGrid = CreateGridContainer(page3);
                CreateToggle("Reduce Mission Rewards", "MissionRewardNerf", Plugin.EnableMissionRewardNerf, missionGrid, font);
                CreateToggle("Nerf ALL Missions", "NerfAllMissions", Plugin.NerfAllMissionRewards, missionGrid, font);
                CreateSlider("Reward Multiplier", "MissionReward", Plugin.MissionRewardMultiplier, 0.1f, 1.0f, missionGrid, font);
                CreateSectionHeader("Debug", page3, font);
                var debugGrid = CreateGridContainer(page3);
                CreateToggle("Enable Debug Logging", "DebugMode", Plugin.EnableDebugMode, debugGrid, font);

                ShowPage(0);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in CreateConfigPages: {ex}");
            }
        }
        
        private static GameObject CreateGridContainer(GameObject parent)
        {
            var gridObj = new GameObject("GridContainer");
            gridObj.transform.SetParent(parent.transform, false);
            var gridLayout = gridObj.AddComponent<GridLayoutGroup>();
            var padding = new RectOffset();
            padding.left = 10; padding.right = 10; padding.top = 5; padding.bottom = 15;
            gridLayout.padding = padding;
            gridLayout.cellSize = new Vector2(380, 32);
            gridLayout.spacing = new Vector2(15, 5);
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

        private static void CreateSectionHeader(string text, GameObject parent, TMP_FontAsset font)
        {
            try
            {
                var headerObj = new GameObject($"Header_{text}");
                headerObj.transform.SetParent(parent.transform, false);
                headerObj.AddComponent<LayoutElement>().minHeight = 30;
                var headerText = headerObj.AddComponent<TextMeshProUGUI>();
                headerText.font = font;
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

        private static void CreateToggle(string label, string key, ConfigEntry<bool> config, GameObject parent, TMP_FontAsset font)
        {
            try
            {
                // This is the main container for the entire row (checkbox + label)
                var toggleObj = new GameObject($"Toggle_{key}");
                toggleObj.transform.SetParent(parent.transform, false);
                var hLayout = toggleObj.AddComponent<HorizontalLayoutGroup>();
                hLayout.childAlignment = TextAnchor.MiddleLeft;
                hLayout.spacing = 10;
                hLayout.childForceExpandHeight = false; // Correctly prevents children from stretching vertically

                // --- THE FIX: A fixed-size container for the checkbox graphic ---
                var checkboxContainer = new GameObject("CheckboxContainer");
                checkboxContainer.transform.SetParent(toggleObj.transform, false);
                var containerLayout = checkboxContainer.AddComponent<LayoutElement>();
                // Set a fixed size for the container, which will not be stretched.
                containerLayout.minWidth = 24;
                containerLayout.minHeight = 24;
                // -------------------------------------------------------------
                
                var toggle = toggleObj.AddComponent<Toggle>();

                // The Background is now a child of the container and its size is controlled by the RectTransform
                var bgObj = new GameObject("Background");
                bgObj.transform.SetParent(checkboxContainer.transform, false);
                var bgImage = bgObj.AddComponent<Image>();
                bgImage.color = new Color(0.3f, 0.3f, 0.3f);
                var bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.sizeDelta = new Vector2(24, 24); // Set the size directly
                bgRect.anchoredPosition = Vector2.zero;

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
                
                // The Label is a sibling of the checkbox container and will align next to it.
                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(toggleObj.transform, false);
                var labelLayout = labelObj.AddComponent<LayoutElement>();
                labelLayout.flexibleWidth = 1;
                var labelText = labelObj.AddComponent<TextMeshProUGUI>();
                labelText.font = font;
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
        private static void CreateSlider(string label, string key, ConfigEntry<float> config, float min, float max, GameObject parent, TMP_FontAsset font)
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
                labelText.font = font;
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
            try
            {
                // Restore timescale before cleanup
                if (_pausedByConfig)
                {
                    Time.timeScale = _originalTimeScale;
                    _pausedByConfig = false;
                }
                
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
                _pageIndicator = null;
                _contentPanel = null;
                _isVisible = false;
                _selectedIndex = 0;
                _currentPage = 0;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error during cleanup: {ex}");
            }
        }
    }
}

