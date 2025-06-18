using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SO2R_Warp_Drive_Mods
{
    public class BgmEntry
    {
        public string title    { get; set; } = "";
        public string composer { get; set; } = "";
        public int    track    { get; set; }
        public string album    { get; set; } = "";
    }

    static class BgmNameLoader
    {
        // id → metadata
        static Dictionary<int,BgmEntry> _entries = new();

        public static void Load()
        {
            var path = Path.Combine(Paths.ConfigPath, "BgmNames.yaml");
            if (!File.Exists(path))
            {
                Plugin.Logger.LogInfo("BgmNames.yaml not found; using defaults.");
                return;
            }

            try
            {
                var yaml = File.ReadAllText(path);
                var deser = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                _entries = deser
                               .Deserialize<Dictionary<int,BgmEntry>>(yaml)
                           ?? new Dictionary<int,BgmEntry>();

                Plugin.Logger.LogInfo($"Loaded {_entries.Count} BGM metadata entries.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error loading BgmNames.yaml: {ex}");
            }
        }

        public static BgmEntry? Get(int id)
            => _entries.TryGetValue(id, out var e) ? e : null;
    }
}