using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;

namespace GunsGunsGuns.Core
{
    internal static class WeaponTuning
    {
        private struct Binding
        {
            public WeaponProfile Profile;
            public FieldInfo     ProfileField;
            public FieldInfo     ConfigField;
        }

        private static readonly List<Binding> _bindings = new List<Binding>();
        private static bool _bound;

        public static void Bind()
        {
            if (_bound) return;
            _bound = true;

            var configFields = new Dictionary<string, FieldInfo>();
            foreach (var f in typeof(Config).GetFields(BindingFlags.Public | BindingFlags.Static))
                configFields[f.Name] = f;

            var profileFields = typeof(WeaponProfile).GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var p in Profiles.All)
            {
                if (p == null || string.IsNullOrEmpty(p.Key)) continue;

                int n = 0;
                foreach (var pf in profileFields)
                {
                    if (!configFields.TryGetValue(p.Key + "_" + pf.Name, out var cf)) continue;

                    if (cf.FieldType != pf.FieldType)
                    {
                        MelonLogger.Warning(
                            $"[AK] {cf.Name} is {cf.FieldType.Name} but {pf.Name} is {pf.FieldType.Name} — not bound.");
                        continue;
                    }

                    _bindings.Add(new Binding { Profile = p, ProfileField = pf, ConfigField = cf });
                    n++;
                }

                if (n == 0) MelonLogger.Warning($"[AK] {p.Name} has key '{p.Key}' but no Config fields match it.");
                else Dbg.Log($"[AK] {p.Name}: {n} tunables bound.");
            }
        }

        public static void Capture(WeaponProfile only = null)
        {
            foreach (var b in _bindings)
            {
                if (only != null && !ReferenceEquals(b.Profile, only)) continue;
                try { b.ConfigField.SetValue(null, b.ProfileField.GetValue(b.Profile)); }
                catch (Exception e) { MelonLogger.Warning($"[AK] capture {b.ConfigField.Name}: {e.Message}"); }
            }
        }

        public static void Apply()
        {
            foreach (var b in _bindings)
            {
                try { b.ProfileField.SetValue(b.Profile, b.ConfigField.GetValue(null)); }
                catch (Exception e) { MelonLogger.Warning($"[AK] apply {b.ConfigField.Name}: {e.Message}"); }
            }
        }
    }
}
