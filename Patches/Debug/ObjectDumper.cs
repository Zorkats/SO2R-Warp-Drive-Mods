using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Game;
using System.Linq;

namespace SO2R_Warp_Drive_Mods.Patches.Debug
{
    public static class ObjectDumper
    {
        private static HashSet<object> _visited = new HashSet<object>();

        // --- F11: GLOBAL DUMP ---
        public static void DumpAllGameTypes()
        {
            Plugin.Logger.LogWarning("=== DUMPING ALL GAME TYPES ===");
            try
            {
                // 1. Dump Assembly Types (User Request)
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                          .FirstOrDefault(a => a.GetName().Name.Contains("Assembly-CSharp"));

                if (asm != null)
                {
                    var types = asm.GetTypes()
                        .Where(t => t.Namespace != null && t.Namespace.StartsWith("Game"))
                        .OrderBy(t => t.Name);

                    foreach (var t in types)
                    {
                        Plugin.Logger.LogInfo($"[TYPE] {t.FullName}");
                    }
                }

                // 2. Dump Managers (My Previous Logic)
                if (GameManager.Instance != null)
                    DumpRecursive(GameManager.Instance, "GameManager_Instance", 2);

                if (BattleManager.Instance != null)
                    DumpRecursive(BattleManager.Instance, "BattleManager_Instance", 1);

            }
            catch (Exception ex) { Plugin.Logger.LogError($"DumpTypes Error: {ex}"); }
            Plugin.Logger.LogWarning("=== END TYPE DUMP ===");
        }

        // --- F12: DEEP RECURSIVE DUMP ---
        public static void DumpRecursive(object root, string label, int maxDepth = 2)
        {
            _visited.Clear();
            var sb = new StringBuilder();
            DumpRecursiveInternal(root, label, 0, maxDepth, sb);
            Plugin.Logger.LogInfo(sb.ToString());
        }

        private static void DumpRecursiveInternal(object obj, string label, int currentDepth, int maxDepth, StringBuilder sb)
        {
            string indent = new string('-', currentDepth * 2) + " ";

            if (obj == null)
            {
                sb.AppendLine($"{indent}{label} = NULL");
                return;
            }

            var type = obj.GetType();

            // Primitives / Strings
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            {
                sb.AppendLine($"{indent}{label} ({type.Name}) = {obj}");
                return;
            }

            // Prevent Cycles
            if (_visited.Contains(obj))
            {
                sb.AppendLine($"{indent}{label} = [Cyclic Reference]");
                return;
            }
            _visited.Add(obj);

            sb.AppendLine($"{indent}=== {label} ({type.Name}) ===");

            if (currentDepth >= maxDepth) return;

            // Fields
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    object val = f.GetValue(obj);
                    // Don't recurse into system types to keep log clean
                    if (val != null && (val.GetType().Namespace?.StartsWith("System") ?? false) && !val.GetType().IsPrimitive)
                    {
                        sb.AppendLine($"{indent}  [F] {f.Name} = {val}");
                    }
                    else
                    {
                        DumpRecursiveInternal(val, $"[F] {f.Name}", currentDepth + 1, maxDepth, sb);
                    }
                }
                catch { sb.AppendLine($"{indent}  [F] {f.Name} = <Error>"); }
            }

            // Properties
            foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    if (p.GetIndexParameters().Length == 0)
                    {
                        object val = p.GetValue(obj);
                        sb.AppendLine($"{indent}  [P] {p.Name} ({p.PropertyType.Name}) = {val}");
                    }
                }
                catch {}
            }
        }
    }
}