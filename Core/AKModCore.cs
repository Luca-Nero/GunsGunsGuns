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

    // ── Configuration ─────────────────────────────────────────────────────────────
    internal static class Config
    {
        // ── Controls ───────────────────────────────────────────────────────────

        [FruitLib.MenuCategory("Controls")] public static KeyCode HoldBreathKey = KeyCode.Mouse4;
        [FruitLib.MenuCategory("Controls")] public static bool AdsToggle = false;  // otherwise hold RMB


        // ── Impacts ───────────────────────────────────────────────────────────
        public static bool  ShowImpactMarks = true;
        public static float ImpactPitch       = 0.65f; // matches the pitched-down fire sound
        public static float ImpactVolume      = 0.15f;
        public static float ImpactMaxDistance = 60f;
        public static float MarkSize        = 0.07f;
        public static float MarkSkidStretch = 4f;    // how far a grazing mark elongates
        public static float MarkAlpha       = 0.85f;
        public static float MarkLifetime    = 25f;
        public static int   MaxMarks        = 96;

        // ── Model ─────────────────────────────────────────────────────────────

        public static bool  ShowModel   = true;
        public static bool  ModelRecoil = true;  // the gun's own kick
        public static bool  ViewRecoil  = true;  // the camera's
        public static float RecoilScale = 1f;
        public static float ModelColorR = 0.30f;
        public static float ModelColorG = 0.28f;
        public static float ModelColorB = 0.26f;

        // ── Scope ─────────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Scope")] public static bool ShowScope       = true;
        [FruitLib.MenuCategory("Scope")] public static int   ScopeResolution = 768;
        [FruitLib.MenuCategory("Scope")] public static float ScopeIdleHz     = 30f;  // refresh with the gun down

        // ── Breath ────────────────────────────────────────────────────────────
        public static bool  AllowBreath    = true;
        [FruitLib.MenuCategory("Breath")] public static float BreathSway     = 0.05f;  // multiplier while held
        [FruitLib.MenuCategory("Breath")] public static float BreathDuration = 5f;
        [FruitLib.MenuCategory("Breath")] public static float BreathRecovery = 4f;
        [FruitLib.MenuCategory("Breath")] public static float BreathFade     = 0.25f; // seconds to settle
        [FruitLib.MenuCategory("Breath")] public static bool  BreathMeter    = true;

        // ── Sights ─────────────────────────────────────────────
        public static bool AllowAds  = true;
        public static bool AdsTuning = false;   // keypad nudging, aimed only
        public static bool AdsSensScaling = false;

        // ── Barrel ────────────────────────────────────────────────────────────
        public static float AimConvergence  = 100f; 
        public static float MuzzleClearance = 0.45f;
        // ── Shells ────────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Shells")] public static bool  ShowShells    = true;
        public static bool  CustomShells  = false;
        [FruitLib.MenuCategory("Shells")] public static float ShellLifetime = 20f;
        [FruitLib.MenuCategory("Shells")] public static int   MaxShells     = 48;

        // ── Wound (shape of the voxel carve; strength/radii are per weapon) ───
        public static float MinDepth      = 0.05f;  // below this the round is spent
        public static int   ConeStep      = 1;
        public static int   ConeMaxSteps  = 64;

        // ── Presentation ──────────────────────────────────────────────────────
        public static float PitchVariance = 0.05f;  // ±5% so repeat shots aren't monotone
        public static float ShotVolume    = 1f;
        [FruitLib.MenuCategory("Crosshair")] public static bool ShowCrosshair = true;
        [FruitLib.MenuCategory("Crosshair")] public static bool HideNativeCrosshair = true;  // drop the game's centre dot while a gun is out
        public static bool  ShowTracer    = true;

        // ── External forces ───────────────────────────────────────────────────
        public static bool  ExternalForces     = true;
        public static float ExternalForceScale = 1f;

        // ── Debug ─────────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Debug")] public static bool  DebugDrawPath = false;
        [FruitLib.MenuCategory("Debug")] public static float PathWidth     = 0.02f;
        [FruitLib.MenuCategory("Debug")] public static float PathLifetime  = 8f;   // how long a path lingers after the round dies
        [FruitLib.MenuCategory("Debug")] public static int   PathMaxPoints = 512;
        [FruitLib.MenuCategory("Debug")] public static bool  DebugLog      = false;
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
                foreach (var line in File.ReadAllLines(IniPath))
                {
                    string t = line.Trim();
                    if (string.IsNullOrEmpty(t) || t.StartsWith("#")) continue;
                    int eq = t.IndexOf('=');
                    if (eq < 0) continue;
                    SetField(t.Substring(0, eq).Trim(), t.Substring(eq + 1).Trim());
                }
                MelonLogger.Msg("GGGConfig.ini loaded.");
            }
            catch (System.Exception e) { MelonLogger.Warning($"Config load failed: {e.Message}"); }
        }

        private static void SetField(string key, string value)
        {
            var f = typeof(Config).GetField(key,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (f == null) return;
            try
            {
                if (f.FieldType == typeof(float)) f.SetValue(null, float.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
                else if (f.FieldType == typeof(int))   f.SetValue(null, int.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
                else if (f.FieldType == typeof(bool))  f.SetValue(null, value.ToLower() == "true");
            }
            catch { }
        }
        private static void Write()
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# GunsGunsGuns v2.0  —  Configuration");

            foreach (var f in typeof(Config).GetFields(
                         System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                object v = f.GetValue(null);
                string s = f.FieldType == typeof(float) ? ((float)v).ToString("0.###", ci)
                         : f.FieldType == typeof(bool)  ? ((bool)v ? "true" : "false")
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

    // ── Mod entry point ───────────────────────────────────────────────────────────
    public class Core : MelonMod
    {
        // ── Versioning & Dependences ─────────────────────────────────────────────────────────────
        public const string Version = "2.0.0";
        private const int LibMajor = 2, LibMinor = 1, LibPatch = 0;
        private bool _active;

        public override void OnInitializeMelon()
        {
            _active = FruitGate.Check("GunsGunsGuns", LibMajor, LibMinor, LibPatch);
            if (!_active) return;

            Init();
        }

        private void Init() 
        {
            ConfigLoader.Load();
            FruitMenu.Register("GunsGunsGuns", ConfigLoader.IniPath, typeof(Config));
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

            LoggerInstance.Msg("GunsGunsGuns v2.0 loaded.");
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

}
