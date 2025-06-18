using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

// Note: I've removed the specific 'using' statements for your patch classes here.
// It's a slightly cleaner practice to keep them out of the main plugin file.

namespace SO2R_Warp_Drive_Mods
{
    [BepInPlugin("com.zorkats.so2r_qol", "SO2R QoL Patches", "1.0.0")]
    public class Plugin : BasePlugin
    {
        // --- Static variables for easy access from patches ---
        internal static ManualLogSource Logger = null!;

        // General Settings
        internal static ConfigEntry<bool> EnablePauseOnFocusLoss = null!;

        // BGM Info Settings
        internal static ConfigEntry<bool> EnableBgmInfo = null!;
        internal static ConfigEntry<bool> ShowOncePerSession = null!;

        // Gameplay Settings
        internal static ConfigEntry<bool> EnableMovementMultiplier = null!;
        internal static ConfigEntry<float> MovementSpeedMultiplier = null!;
        internal static ConfigEntry<bool> EnableAggroRangeMultiplier = null!;
        internal static ConfigEntry<float>? AggroRangeMultiplier;


        // This will be our global flag, accessible from any patch.
        internal static bool IsBattleActive = false;


        public override void Load()
        {
            Logger = Log;

            // --- Configuration Setup ---
            // By using section headers like "1. General", BepInEx will organize the config file automatically.

            Logger.LogInfo("Binding General settings...");
            EnablePauseOnFocusLoss = Config.Bind(
                "1. General",
                "Pause On Focus Loss",
                true,
                "Automatically pauses the game when the window loses focus."
            );

            Logger.LogInfo("Binding BGM Info settings...");
            EnableBgmInfo = Config.Bind(
                "2. BGM Info",
                "Enable",
                true,
                "Shows the current BGM track name and details on screen when a new song starts."
            );

            ShowOncePerSession = Config.Bind(
                "2. BGM Info",
                "Show Once Per Session",
                true,
                "If true, BGM info is shown only the first time a track plays per session. If false, it shows every time."
            );

            Logger.LogInfo("Binding Gameplay settings...");
            EnableMovementMultiplier = Config.Bind(
                "3. Gameplay",
                "Enable Movement Speed Multiplier",
                true,
                "Enables a multiplier for player movement speed on the field.");

            MovementSpeedMultiplier = Config.Bind(
                "3. Gameplay",
                "Movement Speed Multiplier",
                1.75f, // Default to 75% faster
                "The multiplier for player movement speed. 1.0 is normal, 2.0 is double speed.");

            EnableAggroRangeMultiplier = Config.Bind(
                "3. Gameplay", // Add it to your existing Gameplay section
                "Enable Aggro Range Multiplier",
                true,
                "Enables a multiplier for enemy detection range."
            );

            AggroRangeMultiplier = Config.Bind(
                "3. Gameplay",
                "Aggro Range Multiplier",
                3.0f, // Default to half the normal aggro range
                "Multiplier for the enemy detection range. 0.5 is half, 2.0 is double. 0 is effectively invisible."
            );

            // --- Final Initialization ---
            Logger.LogInfo("Loading BGM Name Database...");
            BgmNameLoader.Load();

            Logger.LogInfo("Applying all Harmony patches...");
            new Harmony("com.zorkats.so2r_qol").PatchAll();

            Logger.LogInfo("SO2R QoL - All-in-One loaded successfully.");
        }
    }
}
