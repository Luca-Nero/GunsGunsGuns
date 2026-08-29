using FruitLib;
using GunsGunsGuns.Core;
using GunsGunsGuns.Projectiles;
using GunsGunsGuns.View;
using Il2CppPlayer.Appearances.God.Toolbar;
using Il2CppSpawnables.Weapons;
using MelonLoader;
using Singularity;
using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

[assembly: MelonInfo(typeof(Core), "GunsGunsGuns", GunsGunsGuns.Core.Core.Version, "Luca_Nero")]
[assembly: MelonGame()]

namespace GunsGunsGuns.Core
{

    // ── Mod entry point ───────────────────────────────────────────────────────────
    public class Core : MelonMod
    {
        // ── Versioning & Dependences ─────────────────────────────────────────────────────────────
        public const string Version = "2.1.1";
        private const int LibMajor = 2, LibMinor = 1, LibPatch = 0;
        private bool _active;

        public override void OnInitializeMelon()
        {
            _active = FruitGate.Check("GunsGunsGuns", LibMajor, LibMinor, LibPatch);
            if (!_active) return;

            Init();
        }
        public override void OnLateInitializeMelon()
        {
            if (_active) return;
            try { Unregister(FruitGate.FailureReason, silent: true); } catch { }
        }
        private void Init() 
        {
            WeaponTuning.Bind();
            WeaponTuning.Capture();

            FruitMenu.Register("GunsGunsGuns", ConfigLoader.IniPath, typeof(Config));

            ConfigLoader.Load();
            WeaponTuning.Apply();

            FruitMenu.OnConfigChanged += WeaponTuning.Apply;

            AkModel.Meshes = new FruitMeshLibrary(System.Reflection.Assembly.GetExecutingAssembly());

            var start = Profiles.Current;
            var item = new FruitToolbarItem
            {
                Id = "GunsGunsGuns:Weapons",
                Name = start.Name,
                Icon = FruitToolbar.MakeSolidIcon(start.IconColor),
                OnSelected = AkWeapon.OnSelected,
                OnDeselected = AkWeapon.OnDeselected,
            };

            FruitToolbar.Register(item);
            AkWeapon.SlotItem = item;

            FruitUpdateCheck.Register("GunsGunsGuns", Version, "Luca-Nero", "GunsGunsGuns");

            LoggerInstance.Msg("GunsGunsGuns v2.1.1 loaded.");
        }


        public override void OnUpdate()
        {
            AkWeapon.Tick();
            AkModel.Tick();
            AkProjectiles.Tick();   
            AkImpacts.Tick();
            AkShells.Tick();
            AkViewKick.Tick();  
            AkScope.Tick();
            AkSway.Tick(AkAds.Blend > 0.01f);
            AkNativeCrosshair.Tick();
        }

        public override void OnGUI() => AkWeapon.DrawCrosshair();

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            AkModel.OnSceneReload();
            AkWeapon.OnSceneReload();
            AkProjectiles.Clear();  
            AkImpacts.Clear();
            AkShells.Clear();
            AkViewKick.Reset();
            AkScope.Reset();
            AkSway.Reset();
            AkNativeCrosshair.Reset();
        }
    }

    internal static class Config
    {
        // ── Player Facing Configuration ────────────────────────────────────────

        // ── Controls ───────────────────────────────────────────────────────────

        [FruitLib.MenuCategory("Controls")] public static KeyCode HoldBreathKey = KeyCode.Mouse4;
        [FruitLib.MenuCategory("Controls")] public static bool AdsToggle = false;  // otherwise hold RMB

        // ── Per-weapon tunables ───────────────────────────────────────────────

        // AK-47
        [FruitLib.MenuCategory("AK-47")] public static float AK_FireRateRPM = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_MuzzleVelocity = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_EntryWound = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_ExitWound = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_RoundGravity = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_RoundLifetime = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_ImpactImpulse = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_WorldImpulse = 0f;
        [FruitLib.MenuCategory("AK-47")] public static int AK_MaxPenetrations = 0;
        [FruitLib.MenuCategory("AK-47")] public static float AK_PenetrationLoss = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_MaxPenetrationDepth = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_PenetrationDeflect = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_RicochetAngle = 0f;
        [FruitLib.MenuCategory("AK-47")] public static int AK_MaxBounces = 0;
        [FruitLib.MenuCategory("AK-47")] public static float AK_RicochetEnergyLoss = 0f;
        [FruitLib.MenuCategory("AK-47")] public static float AK_RicochetScatter = 0f;

        // RM870
        [FruitLib.MenuCategory("RM870")] public static float RM870_FireRateRPM = 0f;
        [FruitLib.MenuCategory("RM870")] public static int RM870_Pellets = 0;
        [FruitLib.MenuCategory("RM870")] public static float RM870_SpreadDegrees = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_MuzzleVelocity = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_EntryWound = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_ExitWound = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_RoundGravity = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_RoundLifetime = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_ImpactImpulse = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_WorldImpulse = 0f;
        [FruitLib.MenuCategory("RM870")] public static int RM870_MaxPenetrations = 0;
        [FruitLib.MenuCategory("RM870")] public static float RM870_PenetrationLoss = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_MaxPenetrationDepth = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_PenetrationDeflect = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_RicochetAngle = 0f;
        [FruitLib.MenuCategory("RM870")] public static int RM870_MaxBounces = 0;
        [FruitLib.MenuCategory("RM870")] public static float RM870_RicochetEnergyLoss = 0f;
        [FruitLib.MenuCategory("RM870")] public static float RM870_RicochetScatter = 0f;

        // AS50
        [FruitLib.MenuCategory("AS50")] public static float AS50_FireRateRPM = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_MuzzleVelocity = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_EntryWound = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_ExitWound = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_RoundGravity = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_RoundLifetime = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_ImpactImpulse = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_WorldImpulse = 0f;
        [FruitLib.MenuCategory("AS50")] public static int AS50_MaxPenetrations = 0;
        [FruitLib.MenuCategory("AS50")] public static float AS50_PenetrationLoss = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_MaxPenetrationDepth = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_PenetrationDeflect = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_RicochetAngle = 0f;
        [FruitLib.MenuCategory("AS50")] public static int AS50_MaxBounces = 0;
        [FruitLib.MenuCategory("AS50")] public static float AS50_RicochetEnergyLoss = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_RicochetScatter = 0f;
        [FruitLib.MenuCategory("AS50")] public static bool ShowScope = true;
        [FruitLib.MenuCategory("AS50")] public static float ScopeIdleHz = 30f;  // refresh with the gun down
        [FruitLib.MenuCategory("AS50")] public static float AS50_AdsSway = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_AdsShake = 0f;
        [FruitLib.MenuCategory("AS50")] public static float AS50_ScopeFov = 0f;

        // ── Debug ─────────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Debug")] public static bool DebugDrawPath = false;
        [FruitLib.MenuCategory("Debug")] public static float PathWidth = 0.02f;
        [FruitLib.MenuCategory("Debug")] public static float PathLifetime = 8f;   // how long a path lingers after the round dies
        [FruitLib.MenuCategory("Debug")] public static int PathMaxPoints = 512;
        [FruitLib.MenuCategory("Debug")] public static bool DebugLog = false;

        // ── Breath ─────────────────────────────────────────────────────────────

        [FruitLib.MenuCategory("Breath")] public static float BreathSway = 0.05f;  // multiplier while held
        [FruitLib.MenuCategory("Breath")] public static float BreathDuration = 5f;
        [FruitLib.MenuCategory("Breath")] public static float BreathRecovery = 4f;
        [FruitLib.MenuCategory("Breath")] public static float BreathFade = 0.25f; // seconds to settle
        [FruitLib.MenuCategory("Breath")] public static bool BreathMeter = true;

        // ── Crosshair ─────────────────────────────────────────────────────────

        [FruitLib.MenuCategory("Crosshair")] public static bool ShowCrosshair = true;
        [FruitLib.MenuCategory("Crosshair")] public static bool HideNativeCrosshair = true;  // drop the game's centre dot while a gun is out

        // ── Sounds ───────────────────────────────────────────────────────────
        public static bool ShowImpactMarks = true;
        public static float ImpactPitch = 0.65f; // matches the pitched-down fire sound
        public static float ImpactVolume = 0.15f;
        public static float ImpactMaxDistance = 60f;
        public static float MarkSize = 0.07f;
        public static float MarkSkidStretch = 4f;    // how far a grazing mark elongates
        public static float MarkAlpha = 0.85f;
        public static float MarkLifetime = 25f;
        public static int MaxMarks = 96;

        // ── Model ─────────────────────────────────────────────────────────────

        public static bool ShowModel = true;
        public static bool ModelRecoil = true;  // the gun's own kick
        public static bool ViewRecoil = true;  // the camera's
        public static float RecoilScale = 1f;
        public static float ModelColorR = 0.30f;
        public static float ModelColorG = 0.28f;
        public static float ModelColorB = 0.26f;

        // ── Shells ─────────────────────────────────────────────────────────────
        public static bool CustomShells = false;

        // ── Breath ────────────────────────────────────────────────────────────
        public static bool AllowBreath = true;


        // ── Sights ─────────────────────────────────────────────
        public static bool AllowAds = true;
        public static bool AdsTuning = false;   // keypad nudging, aimed only
        public static bool AdsSensScaling = false;

        // ── Barrel ────────────────────────────────────────────────────────────
        public static float AimConvergence = 100f;
        public static float MuzzleClearance = 0.45f;


        // ── Wound (shape of the voxel carve; strength/radii are per weapon) ───
        public static float MinDepth = 0.05f;  // below this the round is spent
        public static int ConeStep = 1;
        public static int ConeMaxSteps = 64;

        // ── Presentation ──────────────────────────────────────────────────────
        public static float PitchVariance = 0.05f;  // ±5% so repeat shots aren't monotone
        public static float ShotVolume = 1f;
        public static bool ShowTracer = true;

        // ── External forces ───────────────────────────────────────────────────
        public static bool ExternalForces = true;
        public static float ExternalForceScale = 1f;
        public static int ScopeResolution = 768;


        // ── Shells ────────────────────────────────────────────────────────────
        public static bool ShowShells = true;
        public static float ShellLifetime = 20f;
        public static int MaxShells = 48;
    }

    // ── Config Loader (INI) ───────────────────────────────────────────────────────
    internal static class ConfigLoader
    {
        public static string IniPath => Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "GGGConfig.ini");

        public static void Load()
        {
            try
            {
                if (!File.Exists(IniPath)) { Write(); MelonLogger.Msg("Wrote default GGGConfig.ini"); return; }

                var seen = new HashSet<string>();
                bool stale = false;

                foreach (var line in File.ReadAllLines(IniPath))
                {
                    string t = line.Trim();
                    if (string.IsNullOrEmpty(t) || t.StartsWith("#")) continue;
                    int eq = t.IndexOf('=');
                    if (eq < 0) continue;

                    string key = t.Substring(0, eq).Trim();
                    if (SetField(key, t.Substring(eq + 1).Trim())) seen.Add(key);
                    else stale = true;   // a key we no longer have — the file predates a rename
                }

                // A renamed key, or a weapon gaining tunables, leaves the file short. Write it
                // back so the INI always lists everything that can be edited, defaults included.
                foreach (var f in Fields())
                    if (!seen.Contains(f.Name)) { stale = true; break; }

                if (stale) { Write(); MelonLogger.Msg("GGGConfig.ini refreshed — keys added or dropped."); }
                else MelonLogger.Msg("GGGConfig.ini loaded.");
            }
            catch (System.Exception e) { MelonLogger.Warning($"Config load failed: {e.Message}"); }
        }

        private static System.Reflection.FieldInfo[] Fields() =>
            typeof(Config).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        /// <summary>Returns false only when the key names no field — a bad value still counts as known.</summary>
        private static bool SetField(string key, string value)
        {
            var f = typeof(Config).GetField(key,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (f == null) return false;
            try
            {
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                if (f.FieldType == typeof(float)) f.SetValue(null, float.Parse(value, ci));
                else if (f.FieldType == typeof(int)) f.SetValue(null, int.Parse(value, ci));
                else if (f.FieldType == typeof(bool)) f.SetValue(null, value.ToLower() == "true");
                else if (f.FieldType == typeof(string)) f.SetValue(null, value);
                else if (f.FieldType == typeof(KeyCode)) f.SetValue(null, System.Enum.Parse(typeof(KeyCode), value, true));
            }
            catch { }
            return true;
        }

        private static void Write()
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# GunsGunsGuns v2.1.1  —  Configuration");

            foreach (var f in Fields())
            {
                object v = f.GetValue(null);
                string s = f.FieldType == typeof(float) ? ((float)v).ToString("0.##############", ci)
                         : f.FieldType == typeof(bool) ? ((bool)v ? "true" : "false")
                         : System.Convert.ToString(v, ci);
                sb.AppendLine($"{f.Name} = {s}");
            }

            File.WriteAllText(IniPath, sb.ToString());
        }
    }

    internal static class Dbg
    {
        public static void Log(string msg) { if (Config.DebugLog) MelonLogger.Msg(msg); }
    }

}
