using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using Il2CppInterop.Runtime.Injection;

namespace SO2R_Warp_Drive_Mods
{
    public static class RuntimeConfigManager
    {
        private static FileSystemWatcher _configWatcher;
        private static bool _showMenu = false;
        private static float _lastReloadTime = 0f;
        private const float RELOAD_COOLDOWN = 1f;
        private static RuntimeConfigGUI _guiRenderer;
        
        public static void Initialize()
        {
            try
            {
                // Register our GUI component with Il2Cpp
                ClassInjector.RegisterTypeInIl2Cpp<RuntimeConfigGUI>();
                
                // Set up file watcher for config changes
                var configFile = Path.Combine(Paths.ConfigPath, "com.zorkats.so2r_qol.cfg");
                var configDir = Path.GetDirectoryName(configFile);
                
                _configWatcher = new FileSystemWatcher(configDir);
                _configWatcher.Filter = Path.GetFileName(configFile);
                _configWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                _configWatcher.Changed += OnConfigFileChanged;
                _configWatcher.EnableRaisingEvents = true;
                
                // Create GUI renderer
                var guiObject = new GameObject("SO2R_ConfigGUI");
                _guiRenderer = guiObject.AddComponent<RuntimeConfigGUI>();
                UnityEngine.Object.DontDestroyOnLoad(guiObject);
                
                Plugin.Logger.LogInfo("Runtime configuration manager initialized - watching for config changes");
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
                    ToggleMenu();
                }
            }
            catch (Exception ex)
            {
                // Silently ignore input errors
            }
        }
        
        public static void ToggleMenu()
        {
            _showMenu = !_showMenu;
            if (_guiRenderer != null)
            {
                _guiRenderer.SetMenuVisible(_showMenu);
            }
            
            // Pause/unpause game when menu is shown/hidden
            if (_showMenu)
            {
                Time.timeScale = 0f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Time.timeScale = 1f;
                // Don't hide cursor automatically - let the game handle it
            }
        }
        
        public static void Cleanup()
        {
            _configWatcher?.Dispose();
            if (_guiRenderer != null && _guiRenderer.gameObject != null)
            {
                UnityEngine.Object.Destroy(_guiRenderer.gameObject);
            }
        }
        
        // GUI Component - inherits from MonoBehaviour for IL2CPP
        public class RuntimeConfigGUI : MonoBehaviour
        {
            private bool _showMenu = false;
            private Vector2 _scrollPosition;
            private GUIStyle _windowStyle;
            private GUIStyle _labelStyle;
            private GUIStyle _toggleStyle;
            private GUIStyle _sliderStyle;
            private bool _stylesInitialized = false;
            
            public RuntimeConfigGUI()
            {
                // Constructor for IL2CPP registration
            }
            public void SetMenuVisible(bool visible)
            {
                _showMenu = visible;
            }
            
            void OnGUI()
            {
                try
                {
                    if (!_showMenu) return;
                    
                    // Initialize styles
                    if (!_stylesInitialized)
                    {
                        InitializeStyles();
                        _stylesInitialized = true;
                    }
                    
                    // Draw background overlay
                    GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", GUI.skin.box);
                    
                    // Calculate window size
                    float windowWidth = Math.Min(800, Screen.width - 100);
                    float windowHeight = Math.Min(600, Screen.height - 100);
                    float windowX = (Screen.width - windowWidth) / 2;
                    float windowY = (Screen.height - windowHeight) / 2;
                    
                    // Draw main window
                    GUI.Window(12345, new Rect(windowX, windowY, windowWidth, windowHeight), 
                        DrawSettingsWindow, "SO2R QoL Patches - Runtime Configuration", _windowStyle);
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error in RuntimeConfigGUI OnGUI: {ex}");
                    _showMenu = false;
                }
            }
            
            void InitializeStyles()
            {
                _windowStyle = new GUIStyle(GUI.skin.window);
                _windowStyle.fontSize = 16;
                _windowStyle.fontStyle = FontStyle.Bold;
                
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.fontSize = 14;
                _labelStyle.wordWrap = true;
                
                _toggleStyle = new GUIStyle(GUI.skin.toggle);
                _toggleStyle.fontSize = 14;
                
                _sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            }
            
            void DrawSettingsWindow(int windowID)
            {
                GUILayout.BeginVertical();
                
                // Header
                GUILayout.Label("Press F9 to toggle this menu. Changes are applied immediately!", _labelStyle);
                GUILayout.Space(10);
                
                // Scrollable content
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                
                // General Settings
                DrawSectionHeader("General Settings");
                Plugin.EnablePauseOnFocusLoss.Value = GUILayout.Toggle(Plugin.EnablePauseOnFocusLoss.Value, 
                    "Pause On Focus Loss", _toggleStyle);
                GUILayout.Space(5);
                
                // Quality of Life
                DrawSectionHeader("Quality of Life");
                Plugin.EnableBgmInfo.Value = GUILayout.Toggle(Plugin.EnableBgmInfo.Value, 
                    "Enable BGM Info Display", _toggleStyle);
                Plugin.ShowOncePerSession.Value = GUILayout.Toggle(Plugin.ShowOncePerSession.Value, 
                    "Show BGM Once Per Session", _toggleStyle);
                GUILayout.Space(5);
                
                Plugin.EnableMovementMultiplier.Value = GUILayout.Toggle(Plugin.EnableMovementMultiplier.Value, 
                    "Enable Movement Speed Multiplier", _toggleStyle);
                if (Plugin.EnableMovementMultiplier.Value)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Movement Speed: {Plugin.MovementSpeedMultiplier.Value:F2}x", _labelStyle, GUILayout.Width(150));
                    Plugin.MovementSpeedMultiplier.Value = GUILayout.HorizontalSlider(
                        Plugin.MovementSpeedMultiplier.Value, 0.5f, 3.0f, _sliderStyle, GUI.skin.horizontalSliderThumb);
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(10);
                
                // Difficulty - General
                DrawSectionHeader("Difficulty - General");
                Plugin.EnableNoHealOnLevelUp.Value = GUILayout.Toggle(Plugin.EnableNoHealOnLevelUp.Value, 
                    "Remove Full Heal on Level Up", _toggleStyle);
                GUILayout.Space(5);
                
                Plugin.EnableAggroRangeMultiplier.Value = GUILayout.Toggle(Plugin.EnableAggroRangeMultiplier.Value, 
                    "Enable Aggro Range Multiplier (EXPERIMENTAL)", _toggleStyle);
                if (Plugin.EnableAggroRangeMultiplier.Value)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Aggro Range: {Plugin.AggroRangeMultiplier.Value:F2}x", _labelStyle, GUILayout.Width(150));
                    Plugin.AggroRangeMultiplier.Value = GUILayout.HorizontalSlider(
                        Plugin.AggroRangeMultiplier.Value, 0.1f, 2.0f, _sliderStyle, GUI.skin.horizontalSliderThumb);
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(10);
                
                // Formation Bonuses
                DrawSectionHeader("Difficulty - Formation Bonuses");
                Plugin.EnableFormationBonusReset.Value = GUILayout.Toggle(Plugin.EnableFormationBonusReset.Value, 
                    "Reset Every Battle", _toggleStyle);
                Plugin.EnableFormationBonusHalved.Value = GUILayout.Toggle(Plugin.EnableFormationBonusHalved.Value, 
                    "Halve Bonus Effects", _toggleStyle);
                Plugin.EnableFormationBonusHarder.Value = GUILayout.Toggle(Plugin.EnableFormationBonusHarder.Value, 
                    "Harder to Acquire", _toggleStyle);
                if (Plugin.EnableFormationBonusHarder.Value)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Point Gain: {Plugin.FormationBonusPointMultiplier.Value:F2}x", _labelStyle, GUILayout.Width(150));
                    Plugin.FormationBonusPointMultiplier.Value = GUILayout.HorizontalSlider(
                        Plugin.FormationBonusPointMultiplier.Value, 0.1f, 1.0f, _sliderStyle, GUI.skin.horizontalSliderThumb);
                    GUILayout.EndHorizontal();
                }
                Plugin.EnableFormationBonusDisable.Value = GUILayout.Toggle(Plugin.EnableFormationBonusDisable.Value, 
                    "Disable Completely", _toggleStyle);
                GUILayout.Space(10);
                
                // Chain Battles
                DrawSectionHeader("Difficulty - Chain Battles");
                Plugin.EnableChainBattleNerf.Value = GUILayout.Toggle(Plugin.EnableChainBattleNerf.Value, 
                    "Reduce Chain Bonuses", _toggleStyle);
                if (Plugin.EnableChainBattleNerf.Value)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Chain Bonus: {Plugin.ChainBattleBonusMultiplier.Value:F2}x", _labelStyle, GUILayout.Width(150));
                    Plugin.ChainBattleBonusMultiplier.Value = GUILayout.HorizontalSlider(
                        Plugin.ChainBattleBonusMultiplier.Value, 0.0f, 1.0f, _sliderStyle, GUI.skin.horizontalSliderThumb);
                    GUILayout.EndHorizontal();
                }
                Plugin.EnableChainBattleDisable.Value = GUILayout.Toggle(Plugin.EnableChainBattleDisable.Value, 
                    "Disable Chain Bonuses Completely", _toggleStyle);
                GUILayout.Space(10);
                
                // Mission Rewards
                DrawSectionHeader("Difficulty - Mission Rewards");
                Plugin.EnableMissionRewardNerf.Value = GUILayout.Toggle(Plugin.EnableMissionRewardNerf.Value, 
                    "Reduce Mission Rewards", _toggleStyle);
                if (Plugin.EnableMissionRewardNerf.Value)
                {
                    Plugin.NerfAllMissionRewards.Value = GUILayout.Toggle(Plugin.NerfAllMissionRewards.Value, 
                        "Nerf ALL Missions (not just high-value)", _toggleStyle);
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Reward Multiplier: {Plugin.MissionRewardMultiplier.Value:F2}x", _labelStyle, GUILayout.Width(150));
                    Plugin.MissionRewardMultiplier.Value = GUILayout.HorizontalSlider(
                        Plugin.MissionRewardMultiplier.Value, 0.1f, 1.0f, _sliderStyle, GUI.skin.horizontalSliderThumb);
                    GUILayout.EndHorizontal();
                    
                    if (!Plugin.NerfAllMissionRewards.Value)
                    {
                        GUILayout.Label("High-Value Thresholds:", _labelStyle);
                        
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Money: ", _labelStyle, GUILayout.Width(60));
                        string moneyText = GUILayout.TextField(Plugin.HighValueMoneyThreshold.Value.ToString(), GUILayout.Width(100));
                        if (int.TryParse(moneyText, out int moneyValue))
                        {
                            Plugin.HighValueMoneyThreshold.Value = moneyValue;
                        }
                        GUILayout.Label(" Fol", _labelStyle);
                        GUILayout.EndHorizontal();
                        
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Items: ", _labelStyle, GUILayout.Width(60));
                        string itemText = GUILayout.TextField(Plugin.HighValueItemThreshold.Value.ToString(), GUILayout.Width(100));
                        if (int.TryParse(itemText, out int itemValue))
                        {
                            Plugin.HighValueItemThreshold.Value = itemValue;
                        }
                        GUILayout.Label(" count", _labelStyle);
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.Space(10);
                
                // Debug
                DrawSectionHeader("Debug");
                Plugin.EnableDebugMode.Value = GUILayout.Toggle(Plugin.EnableDebugMode.Value, 
                    "Enable Debug Logging", _toggleStyle);
                
                GUILayout.EndScrollView();
                
                // Footer
                GUILayout.FlexibleSpace();
                GUILayout.Label("Changes are saved automatically to the config file.", _labelStyle);
                
                if (GUILayout.Button("Close (F9)", GUILayout.Height(30)))
                {
                    RuntimeConfigManager.ToggleMenu();
                }
                
                GUILayout.EndVertical();
            }
            
            void DrawSectionHeader(string title)
            {
                GUILayout.Space(5);
                GUILayout.Label($"━━━ {title} ━━━", _labelStyle);
                GUILayout.Space(5);
            }
        } // End of RuntimeConfigGUI class
    } // End of RuntimeConfigManager class
} // End of namespace