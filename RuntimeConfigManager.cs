using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Game;

namespace SO2R_Warp_Drive_Mods
{
    public static class RuntimeConfigManager
    {
        public static bool _isVisible { get; private set; } = false;
        private static GameObject _window;
        private static GameObject _contentPanel;
        private static Canvas _parentCanvas;

        // UI State
        private static Dictionary<string, GameObject> _toggles = new Dictionary<string, GameObject>();
        private static Dictionary<string, GameObject> _sliders = new Dictionary<string, GameObject>();
        private static Dictionary<string, TextMeshProUGUI> _sliderLabels = new Dictionary<string, TextMeshProUGUI>();

        // Navigation
        private static int _selectedIndex = 0;
        private static List<string> _optionKeys = new List<string>();
        private static int _currentPage = 0;
        private static int _totalPages = 3;
        private static List<GameObject> _pageContents = new List<GameObject>();
        private static TextMeshProUGUI _pageIndicator;

        // Font
        private static TMP_FontAsset _cachedFont;

        public static void Initialize()
        {
            Plugin.Logger.LogInfo("RuntimeConfigManager initialized (Lazy Load).");
        }

        public static void RefreshResources()
        {
            _cachedFont = null;
            _parentCanvas = null;

            // Try to find a valid UI Canvas
            var captionCtrl = UnityEngine.Object.FindObjectOfType<UICaptionController>();
            if (captionCtrl != null)
            {
                _parentCanvas = captionCtrl.GetComponentInParent<Canvas>();

                var txt = captionCtrl.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null) _cachedFont = txt.font;
            }
        }

        private static TMP_FontAsset GetSafeFont()
        {
            if (_cachedFont != null) return _cachedFont;

            var allTxt = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            if (allTxt != null && allTxt.Length > 0)
            {
                foreach(var t in allTxt)
                {
                    if(t.font != null)
                    {
                        _cachedFont = t.font;
                        return _cachedFont;
                    }
                }
            }
            return null;
        }

        public static void Update()
        {
            // Safety check: If InputSystem isn't ready, don't do anything
            if (Keyboard.current == null) return;

            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                ToggleWindow();
            }

            if (_isVisible && _window != null)
            {
                HandleInput();
            }
        }

        private static void ToggleWindow()
        {
            _isVisible = !_isVisible;

            if (_isVisible)
            {
                // Try to find canvas if we lost it
                if (_parentCanvas == null) RefreshResources();

                if (_window == null)
                {
                    if (_parentCanvas != null)
                    {
                        CreateConfigWindow(_parentCanvas.transform);
                    }
                    else
                    {
                        Plugin.Logger.LogWarning("Cannot open Config: No UI Canvas found yet.");
                        _isVisible = false;
                        return;
                    }
                }

                if (_window != null)
                {
                    _window.SetActive(true);
                    UpdateUIValues();
                    ShowPage(_currentPage);
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    Time.timeScale = 0f;
                }
            }
            else
            {
                if (_window != null) _window.SetActive(false);
                Time.timeScale = 1f;
            }
        }

        private static void CreateConfigWindow(Transform parent)
        {
            var font = GetSafeFont();
            if (font == null)
            {
                Plugin.Logger.LogError("Cannot create config window: No Font found.");
                return;
            }

            if (_window != null) UnityEngine.Object.Destroy(_window);
            _toggles.Clear(); _sliders.Clear(); _sliderLabels.Clear(); _optionKeys.Clear(); _pageContents.Clear();

            _window = new GameObject("RuntimeConfigWindow");
            _window.transform.SetParent(parent, false);

            var rect = _window.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var bg = _window.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

            var container = new GameObject("Container");
            container.transform.SetParent(_window.transform, false);
            var cRect = container.AddComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(900, 700);
            cRect.anchoredPosition = Vector2.zero;

            CreateText("SO2R QoL Config", 32, new Vector2(0, 320), container.transform, font);
            CreateText("[F9] Close | [1-0, Q-T] Toggle | Arrows: Adjust Sliders | [PgUp/PgDn] Pages", 16, new Vector2(0, 280), container.transform, font);

            var pageObj = CreateText("Page 1/3", 18, new Vector2(0, -320), container.transform, font);
            _pageIndicator = pageObj.GetComponent<TextMeshProUGUI>();
            _pageIndicator.color = Color.yellow;

            _contentPanel = new GameObject("ContentPanel");
            _contentPanel.transform.SetParent(container.transform, false);
            var cpRect = _contentPanel.AddComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0,0); cpRect.anchorMax = new Vector2(1,1);
            cpRect.offsetMin = new Vector2(50, 80); cpRect.offsetMax = new Vector2(-50, -100);

            CreateConfigPages(font);
        }

        private static GameObject CreateText(string content, float size, Vector2 pos, Transform parent, TMP_FontAsset font)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var txt = obj.AddComponent<TextMeshProUGUI>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = size;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            var r = obj.GetComponent<RectTransform>();
            r.sizeDelta = new Vector2(800, 50);
            r.anchoredPosition = pos;
            return obj;
        }

        private static void CreateConfigPages(TMP_FontAsset font)
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
        }

        private static GameObject CreatePage(string name)
        {
            var pageObj = new GameObject(name);
            pageObj.transform.SetParent(_contentPanel.transform, false);
            var pageRect = pageObj.AddComponent<RectTransform>();
            pageRect.anchorMin = Vector2.zero;
            pageRect.anchorMax = Vector2.one;
            pageRect.offsetMin = Vector2.zero;
            pageRect.offsetMax = Vector2.zero;

            var vLayout = pageObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(15, 15, 15, 15);
            vLayout.spacing = 5;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;

            _pageContents.Add(pageObj);
            pageObj.SetActive(false);
            return pageObj;
        }

        private static GameObject CreateGridContainer(GameObject parent)
        {
            var gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(parent.transform, false);
            var gridLayout = gridObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(380, 30);
            gridLayout.spacing = new Vector2(20, 5);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            return gridObj;
        }

        private static void CreateSectionHeader(string text, GameObject parent, TMP_FontAsset font)
        {
            var obj = CreateText($"-- {text} --", 18, Vector2.zero, parent.transform, font);
            var le = obj.AddComponent<LayoutElement>();
            le.minHeight = 40;
        }

        private static void CreateToggle(string label, string key, ConfigEntry<bool> config, GameObject parent, TMP_FontAsset font)
        {
            var root = new GameObject(key);
            root.transform.SetParent(parent.transform, false);
            var h = root.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 10;

            var box = new GameObject("Box");
            box.transform.SetParent(root.transform, false);
            var boxImg = box.AddComponent<Image>();
            boxImg.color = Color.gray;
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.sizeDelta = new Vector2(20, 20);

            var check = new GameObject("Check");
            check.transform.SetParent(box.transform, false);
            var checkImg = check.AddComponent<Image>();
            checkImg.color = Color.green;
            var checkRect = check.GetComponent<RectTransform>();
            checkRect.sizeDelta = new Vector2(14, 14);

            var tog = root.AddComponent<Toggle>();
            tog.targetGraphic = boxImg;
            tog.graphic = checkImg;
            tog.isOn = config.Value;

            var lblObj = new GameObject("Label");
            lblObj.transform.SetParent(root.transform, false);
            var txt = lblObj.AddComponent<TextMeshProUGUI>();
            txt.font = font;
            txt.text = $"{GetKeyHint(key)} {label}";
            txt.fontSize = 14;
            txt.color = Color.white;

            _toggles[key] = root;
        }

        private static void CreateSlider(string label, string key, ConfigEntry<float> config, float min, float max, GameObject parent, TMP_FontAsset font)
        {
             var root = new GameObject(key);
            root.transform.SetParent(parent.transform, false);
            var h = root.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 10;

            var lblObj = new GameObject("Label");
            lblObj.transform.SetParent(root.transform, false);
            var le = lblObj.AddComponent<LayoutElement>();
            le.minWidth = 150;
            var txt = lblObj.AddComponent<TextMeshProUGUI>();
            txt.font = font;
            txt.text = $"{label}: {config.Value:F2}";
            txt.fontSize = 14;
            txt.color = Color.white;

            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(root.transform, false);
            var sle = sliderObj.AddComponent<LayoutElement>();
            sle.flexibleWidth = 1;
            sle.minHeight = 20;

            var sl = sliderObj.AddComponent<Slider>();

            var bg = new GameObject("Bg");
            bg.transform.SetParent(sliderObj.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f,0.2f,0.2f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f); bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var faRect = fillArea.AddComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0, 0.25f); faRect.anchorMax = new Vector2(1, 0.75f);
            faRect.offsetMin = Vector2.zero; faRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fImg = fill.AddComponent<Image>();
            fImg.color = Color.green;
            var fRect = fill.GetComponent<RectTransform>();
            fRect.sizeDelta = Vector2.zero;

            sl.fillRect = fRect;
            sl.minValue = min;
            sl.maxValue = max;
            sl.value = config.Value;

            _sliders[key] = root;
            _sliderLabels[key] = txt;
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

        private static void ShowPage(int index)
        {
            for(int i=0; i<_pageContents.Count; i++)
                _pageContents[i].SetActive(i == index);

            _currentPage = index;
            _pageIndicator.text = $"Page {_currentPage + 1} / {_totalPages}";

            _optionKeys.Clear();
             foreach (var kvp in _sliders)
            {
                if (kvp.Value.activeInHierarchy && kvp.Value.transform.IsChildOf(_pageContents[index].transform))
                {
                    _optionKeys.Add(kvp.Key);
                }
            }
            _selectedIndex = 0;
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

         private static void UpdateUIValues()
        {
            if (_sliders.ContainsKey("MovementSpeed")) _sliders["MovementSpeed"].SetActive(Plugin.EnableMovementMultiplier.Value);
            if (_sliders.ContainsKey("FormationBonusPoint")) _sliders["FormationBonusPoint"].SetActive(Plugin.EnableFormationBonusHarder.Value);
            if (_sliders.ContainsKey("ChainBattleBonus")) _sliders["ChainBattleBonus"].SetActive(Plugin.EnableChainBattleNerf.Value);
            if (_sliders.ContainsKey("MissionReward")) _sliders["MissionReward"].SetActive(Plugin.EnableMissionRewardNerf.Value);
        }

        private static void HandleInput()
        {
            var kb = Keyboard.current;
            if (kb.pageDownKey.wasPressedThisFrame || kb.rightBracketKey.wasPressedThisFrame) { _currentPage = (_currentPage+1)%_totalPages; ShowPage(_currentPage); }
            if (kb.pageUpKey.wasPressedThisFrame || kb.leftBracketKey.wasPressedThisFrame) { _currentPage = (_currentPage-1+_totalPages)%_totalPages; ShowPage(_currentPage); }

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
                if (kb.downArrowKey.wasPressedThisFrame) { _selectedIndex = (_selectedIndex + 1) % _optionKeys.Count; HighlightSelectedSlider(); }
                if (kb.upArrowKey.wasPressedThisFrame) { _selectedIndex = (_selectedIndex - 1 + _optionKeys.Count) % _optionKeys.Count; HighlightSelectedSlider(); }
                if (kb.leftArrowKey.wasPressedThisFrame) AdjustSlider(-0.05f);
                if (kb.rightArrowKey.wasPressedThisFrame) AdjustSlider(0.05f);
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
                var t = _toggles[key].GetComponent<Toggle>();
                t.isOn = !t.isOn;
            }
            UpdateUIValues();
        }

        private static void AdjustSlider(float delta)
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

        private static void UpdateSliderVisual(string key, float val)
        {
             if (_sliders.ContainsKey(key))
            {
                var s = _sliders[key].GetComponentInChildren<Slider>();
                s.value = val;
                _sliderLabels[key].text = $"{key}: {val:F2}";
            }
        }

        public static void Cleanup()
        {
            if (_window != null) UnityEngine.Object.Destroy(_window);
        }
    }
}