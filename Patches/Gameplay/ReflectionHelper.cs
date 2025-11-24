using System;
using System.Reflection;
using UnityEngine;
using Game;
using HarmonyLib;
using System.Linq;

namespace SO2R_Warp_Drive_Mods.Patches.Gameplay
{
    public static class ReflectionHelper
    {
        public static object GetObject(object target, params string[] names)
        {
            if (target == null) return null;
            var type = target.GetType();
            foreach (var name in names)
            {
                var p = FindPropertySafe(type, name);
                if (p != null) try { return p.GetValue(target); } catch {}
                var f = FindFieldSafe(type, name);
                if (f != null) try { return f.GetValue(target); } catch {}
            }
            return null;
        }

        // --- STATS ---
        public static int GetHP(object obj) => GetInt(obj, "HitPoint", "currentHp", "hp");
        public static int GetMP(object obj) => GetInt(obj, "MentalPoint", "currentMp", "mp");
        public static int GetLevel(object obj) => GetInt(obj, "Level", "level", "lvl");
        public static long GetExp(object obj) => GetLong(obj, "Experience", "exp", "Exp");

        public static void SetHP(object obj, int val) => SetInt(obj, val, "HitPoint", "currentHp", "hp");
        public static void SetMP(object obj, int val) => SetInt(obj, val, "MentalPoint", "currentMp", "mp");
        public static void SetExp(object obj, long val) => SetLong(obj, val, "Experience", "exp", "Exp");

        // --- PRIMITIVES ---
        public static int GetInt(object obj, params string[] names)
        {
            if (obj == null) return 0;
            var type = obj.GetType();
            foreach (var name in names)
            {
                var p = FindPropertySafe(type, name);
                if (p != null) try { return Convert.ToInt32(p.GetValue(obj)); } catch {}
                var f = FindFieldSafe(type, name);
                if (f != null) try { return Convert.ToInt32(f.GetValue(obj)); } catch {}
            }
            return 0;
        }

        public static void SetInt(object obj, int val, params string[] names)
        {
            if (obj == null) return;
            var type = obj.GetType();
            foreach (var name in names)
            {
                var p = FindPropertySafe(type, name);
                if (p != null) { try { p.SetValue(obj, val); return; } catch {} }
                var f = FindFieldSafe(type, name);
                if (f != null) { try { f.SetValue(obj, val); return; } catch {} }
            }
        }

        public static long GetLong(object obj, params string[] names)
        {
            if (obj == null) return 0;
            var type = obj.GetType();
            foreach (var name in names)
            {
                // Handle int -> long conversion implicitly
                var p = FindPropertySafe(type, name);
                if (p != null) try { return Convert.ToInt64(p.GetValue(obj)); } catch {}
                var f = FindFieldSafe(type, name);
                if (f != null) try { return Convert.ToInt64(f.GetValue(obj)); } catch {}
            }
            return 0;
        }

        public static void SetLong(object obj, long val, params string[] names)
        {
            if (obj == null) return;
            var type = obj.GetType();
            foreach (var name in names)
            {
                var p = FindPropertySafe(type, name);
                if (p != null) {
                    try {
                        if(p.PropertyType == typeof(int)) p.SetValue(obj, (int)val);
                        else p.SetValue(obj, val);
                        return;
                    } catch {}
                }
                var f = FindFieldSafe(type, name);
                if (f != null) {
                    try {
                        if(f.FieldType == typeof(int)) f.SetValue(obj, (int)val);
                        else f.SetValue(obj, val);
                        return;
                    } catch {}
                }
            }
        }

        public static void ApplyStatMultiplier(object enemyParam, float multiplier)
        {
            if (enemyParam == null || multiplier <= 1.0f) return;

            string[] stats = new[] { "HitPointMax", "MentalPointMax", "Power", "Constitution", "Intelligence", "Guts", "Stamina", "HitRate", "EvasionRate" };

            foreach (var stat in stats)
            {
                int val = GetInt(enemyParam, stat);
                if (val > 0)
                {
                    int newVal = (int)(val * multiplier);
                    SetInt(enemyParam, newVal, stat);
                }
            }
            int maxHp = GetInt(enemyParam, "HitPointMax");
            if (maxHp > 0) SetHP(enemyParam, maxHp);
        }

        private static PropertyInfo FindPropertySafe(Type type, string name)
        {
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var p in props) if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return p;
            return null;
        }

        private static FieldInfo FindFieldSafe(Type type, string name)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                if (f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return f;
                if (f.Name.Equals($"<{name}>k__BackingField", StringComparison.OrdinalIgnoreCase)) return f;
                if (f.Name.Equals($"_{name}_k__BackingField", StringComparison.OrdinalIgnoreCase)) return f;
            }
            return null;
        }
    }
}