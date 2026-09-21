using System.Collections.Generic;
using UnityEngine;

namespace GunsGunsGuns.Core
{
    internal sealed class WeaponProfile
    {
        public string Name      = "Weapon";
        public Color  IconColor = Color.white;

        /// <summary>Prefix that ties this weapon to its Config fields ("AK" → Config.AK_FireRateRPM).
        /// Leave it null and the weapon simply has no live tunables. See WeaponTuning.</summary>
        public string Key       = null;

        // ── Firing ────────────────────────────────────────────────────────────
        public float FireRateRPM   = 600f;
        public int   Pellets       = 1;     // >1 makes it a shotgun
        public float SpreadDegrees = 0f;    // cone half-angle per pellet

        // ── Round ─────────────────────────────────────────────────────────────
        // Flight, penetration and wounding are FruitLib's (FruitBallistics), driven by the
        // game's own wound model. What a weapon decides is what it fires: a real bullet's
        // mass, calibre, speed and drag. Power in tissue follows from its energy, so there is
        // no separate damage number to balance.
        public float BulletMassGrams = 7.9f;
        public float CaliberMm       = 7.62f;
        public float MuzzleVelocity  = 715f;
        public float DragCoefficient = 0.29f;
        public float GravityScale    = 1f;
        public float RoundLifetime   = 4f;

        // ── Impact ────────────────────────────────────────────────────────────
        public float ImpactImpulse = 65f;    // N·s on a struck limb at full power
        public float WorldImpulse  = 10f;    // N·s on anything else

        // ── Wound (voxel steps, ~23 mm each) ──────────────────────────────────
        /// <summary>The wound track's "neck": tissue crossed before the round yaws and the
        /// temporary cavity opens.</summary>
        public int   CleanEntryDepth      = 2;
        public float SpreadChance         = 0.5f;
        public int   CavitationPeakRadius = 4;
        public float CavitationDamage     = -350f;
        public float TearMinRadius        = 1.5f;
        public float TearMaxRadius        = 3f;
        public float ExitTearDamage       = -350f;
        public int   MaxDepth             = 80;
        /// <summary>1 = the game's bone, which stops a 7.62 in a thigh. 0.1 lets real rounds
        /// through bone the way they go through it in life.</summary>
        public float HardTissueScale      = 0.1f;
        public float PenetrationDeflect   = 1f;

        // ── Ricochet ──────────────────────────────────────────────────────────
        public float RicochetAngle      = 70f;
        public int   MaxBounces         = 1;
        public float RicochetEnergyLoss = 0.5f;
        public float RicochetScatter    = 3f;

        /// <summary>The FruitLib spec this weapon fires, kept in step with the fields above by
        /// <see cref="SyncSpec"/>. Registered as "GunsGunsGuns." + Key.</summary>
        public FruitLib.ProjectileSpec Spec { get; private set; }

        public string SpecId => "GunsGunsGuns." + (Key ?? Name);

        /// <summary>Copies the tunables into the spec and (re)registers it. Mutating the same
        /// object keeps any round already in flight consistent with what it was fired as.</summary>
        public void SyncSpec(bool externalForces)
        {
            Spec ??= new FruitLib.ProjectileSpec { Id = SpecId };
            var s = Spec;
            s.MassGrams          = BulletMassGrams;
            s.CaliberMm          = CaliberMm;
            s.MuzzleVelocity     = MuzzleVelocity;
            s.DragCoefficient    = DragCoefficient;
            s.GravityScale       = GravityScale;
            s.Lifetime           = RoundLifetime;
            s.ExternalForces     = externalForces;
            s.WorldImpulse       = WorldImpulse;
            s.PenetrationDeflect = PenetrationDeflect;
            s.RicochetAngle      = RicochetAngle;
            s.MaxBounces         = MaxBounces;
            s.RicochetEnergyLoss = RicochetEnergyLoss;
            s.RicochetScatter    = RicochetScatter;

            var w = s.Wound;
            w.CleanEntryDepth      = CleanEntryDepth;
            w.SpreadChance         = SpreadChance;
            w.CavitationPeakRadius = CavitationPeakRadius;
            w.CavitationDamage     = CavitationDamage;
            w.TearMinRadius        = TearMinRadius;
            w.TearMaxRadius        = TearMaxRadius;
            w.ExitTearDamage       = ExitTearDamage;
            w.MaxDepth             = MaxDepth;
            w.HardTissueScale      = HardTissueScale;
            w.ImpactImpulse        = ImpactImpulse;

            FruitLib.FruitBallistics.Register(s);
        }

        // ── Model ─────────────────────────────────────────────────────────────
        public string  BodyMesh   = null;
        public string  MagMesh    = null;
        public string  BoltMesh   = null;

        public float   ModelScale = 0.09f;
        public Vector3 ModelPos   = new Vector3(-0.013f, 0.085f, -0.067f);
        public Vector3 ModelRot   = new Vector3(180f, 90.5f, 90f);
        public Vector3 MagOffset  = Vector3.zero;
        public Vector3 BoltOffset = Vector3.zero;
        public Vector3 BoltTravel    = new Vector3(0f, -0.55f, 0f);
        public float   BoltBackTime  = 0.03f;  
        public float   BoltDwell     = 0f;    
        public float   BoltCycleTime = 0.06f;  
        public float   CycleDelay    = 0f;

        // ── Ports  ─────────────────────── ────────────────────────────────────
        public Vector3 MuzzleOffset = Vector3.zero;
        public Vector3 EjectOffset  = Vector3.zero;

        // ── Shells ───────────────────────────────────────────────────────────
        public float ShellLength = 0.039f;   // 7.62x39
        public float ShellRadius = 0.0057f;
        public float ShellForce  = 2.2f;     // sideways toss, m/s
        public float ShellSpin   = 14f;      // rad/s

        // ── Sights ─────────────────────────────────────────────
        public float AdsZoom   = 0.65f;
        public float AdsTime   = 0.18f;   // seconds between hip and aimed
        public float AdsSens   = 1f;
        public float AdsSway   = 0.45f;   // pivot sway multiplier while aimed
        public float AdsSpread = 0.30f;   // spread multiplier while aimed
        public float AdsShake  = 0.60f;   // camera shake multiplier while aimed
        public Vector3 ScopeLens   = Vector3.zero;
        public float   ScopeRadius = 0f;
        public float   ScopeFov    = 7f;
        public Vector3 AdsOffset = new Vector3(-0.10f, 0.08f, -0.03f);

        // ── Recoil (the model's own kick; camera shake is ShakeAmount) ───────
        public float RecoilKick      = 0.010f;
        public float RecoilRise      = 1.2f;
        public float RecoilRock      = 0.7f;
        public float ViewKick      = 0.9f;    // degrees the view rises per shot
        public float ViewYaw       = 0.30f;   // degrees of random sideways pull
        public float ViewStiffness = 90f;
        public float ViewDamping   = 12f;

        public float RecoilStiffness = 140f;
        public float RecoilDamping   = 13f;    // under critical (2*sqrt(k)), so it overshoots
        public Vector3 RecoilPivot = Vector3.zero;

        // ── Feel ─────────────────────────────────────────────────────────────
        public float ShakeAmount = 0.18f;
        public float ShotPitch   = 0.65f;
        public float TracerSize  = 0.04f;
    }

    internal static class Profiles
    {
        public static readonly List<WeaponProfile> All = new List<WeaponProfile>
        {
            // Assault rifle
            new WeaponProfile
            {
                Name = "AK-47", Key = "AK", IconColor = new Color(0.85f, 0.55f, 0.15f),
                // ──── Firing ────────────────────────────────────────────────────────
                FireRateRPM = 600f, Pellets = 1, SpreadDegrees = 0.2f,
                // ──── Round: 7.62x39 M43 ball, 7.9 g at 715 m/s (~15100 power) ────────
                BulletMassGrams = 7.9f, CaliberMm = 7.62f, MuzzleVelocity = 715f, DragCoefficient = 0.29f,
                RoundLifetime = 2.5f, ImpactImpulse = 30f, WorldImpulse = 10f,
                // M43 is famously stable: it travels ~26 cm of tissue before it yaws, so the
                // cavity opens late and a torso shot is often a clean through-and-through.
                CleanEntryDepth = 11, SpreadChance = 0.5f, CavitationPeakRadius = 4, CavitationDamage = -350f,
                TearMinRadius = 1.5f, TearMaxRadius = 3f, ExitTearDamage = -350f, MaxDepth = 80,
                HardTissueScale = 0.1f, PenetrationDeflect = 1f,
                RicochetAngle = 65f, MaxBounces = 1, RicochetEnergyLoss = 0.6f,
                // ──── View and recoil ───────────────────────────────────────────────
                ShakeAmount = 0.05f, ShotPitch = 0.65f, TracerSize = 0.04f,
                RecoilKick = 0.01f, RecoilRise = 0.5f, RecoilRock = 0.5f,
                RecoilStiffness = 90f, RecoilDamping = 11f,
                ViewKick = 3.0f, ViewYaw = 0.9f, 
                ViewStiffness = 70f, ViewDamping = 11f,
                AdsZoom = 0.65f, AdsTime = 0.18f,
                // ──── Model ─────────────────────────────────────────────────────────────
                ShellForce = 5f, ShellSpin = 1f,
                ShellLength = 0.039f, ShellRadius = 0.0057f,
                ModelPos   = new Vector3(-0.013f, 0.09f, -0.067f),
                ModelRot   = new Vector3(180f, 90f, 90.5f),
                MagOffset  = new Vector3( 0.083344f, -0.62418f, 0f),
                BoltOffset = new Vector3(-0.11936f,  -1.2398f,  0f),
                BoltTravel = new Vector3(0f, -0.55f, 0f),
                MuzzleOffset = new Vector3( 0f, 4.0411f, 0f),
                AdsOffset    = new Vector3(-0.1160f, 0.1000f, -0.0100f),
                EjectOffset  = new Vector3(-0.1389f, -0.4396f, -0.7507f),
                BodyMesh = "AKBody_mesh", MagMesh = "AKMag_mesh", BoltMesh = "AKBolt_mesh",
                ModelScale = 0.09f,
            },

            // Shotgun
            new WeaponProfile
            {
                Name = "RM870", Key = "RM870", IconColor = new Color(0.75f, 0.2f, 0.2f),
                // ──── Firing ────────────────────────────────────────────────────────
                FireRateRPM = 30f, Pellets = 12, SpreadDegrees = 4.5f,
                // ──── Round: 00 buckshot, per pellet 3.5 g, 8.4 mm at 400 m/s (~2100 power) ──
                // (A real 2¾" 00 shell holds 9 pellets; 12 is this weapon's choice.)
                BulletMassGrams = 3.5f, CaliberMm = 8.4f, MuzzleVelocity = 400f, DragCoefficient = 0.47f,
                RoundLifetime = 2.5f, ImpactImpulse = 30f, WorldImpulse = 10f,
                // Round balls do not yaw: no neck, no temporary cavity worth the name - just
                // many short, ragged crush channels.
                CleanEntryDepth = 0, SpreadChance = 0.2f, CavitationPeakRadius = 0, CavitationDamage = 0f,
                TearMinRadius = 0.5f, TearMaxRadius = 1.5f, ExitTearDamage = -250f, MaxDepth = 30,
                // Soft lead balls flatten on bone rather than punch through it, so bone keeps
                // its full native toughness here - unlike the jacketed rifle rounds.
                HardTissueScale = 1f, PenetrationDeflect = 2f,
                RicochetAngle = 72f, MaxBounces = 1, RicochetEnergyLoss = 0.6f,
                // ──── View and recoil ───────────────────────────────────────────────
                ShakeAmount = 0.55f, ShotPitch = 0.45f, TracerSize = 0.03f,
                BoltBackTime = 0.12f, BoltDwell = 0.2f, BoltCycleTime = 0.25f, CycleDelay = 0.25f,
                RecoilKick = 0.022f, RecoilRise = 3f, RecoilRock = 1.6f,
                RecoilStiffness = 90f, RecoilDamping = 11f,
                ViewKick = 3.2f, ViewYaw = 0.9f, ViewStiffness = 70f, ViewDamping = 11f,
                AdsZoom = 0.60f, AdsTime = 0.14f,
                // ──── Model ─────────────────────────────────────────────────────────────
                ShellForce = 1.8f, ShellSpin = 10f,
                ShellLength = 0.070f, ShellRadius = 0.0102f,
                ModelPos   = new Vector3(0.067f, -0.005f, 0.283f),
                ModelRot   = new Vector3(0f, -90f, 0f),
                BoltOffset = new Vector3(-2.21498f, 0.432121f, 0f),
                BoltTravel = new Vector3(-1.0f, 0f, 0f),
                AdsOffset = new Vector3(-0.1160f, 0.1410f, -0.2580f),
                MuzzleOffset = new Vector3( 4.0269f,  0.7308f,  0f),
                EjectOffset  = new Vector3(-1.3462f, -0.1577f, -0.0462f),
                BodyMesh = "RM870Body_mesh", BoltMesh = "RM870Bolt_and_ForeEnd_mesh",
                ModelScale = 0.13f,
            },

            // Anti-materiel rifle
            new WeaponProfile
            {
                Name = "AS50", Key = "AS50", IconColor = new Color(0.35f, 0.75f, 0.95f),
                // ──── Firing ────────────────────────────────────────────────────────
                FireRateRPM = 35f, Pellets = 1, SpreadDegrees = 0f,
                // ──── Round: .50 BMG M33 ball, 42 g at 850 m/s (~114000 power) ─────────
                // The old -6 gravity was standing in for a flat trajectory; real speed and a
                // real drag figure give it one honestly.
                BulletMassGrams = 42f, CaliberMm = 12.7f, MuzzleVelocity = 850f, DragCoefficient = 0.26f,
                RoundLifetime = 12f, ImpactImpulse = 600f, WorldImpulse = 400f,
                CleanEntryDepth = 4, SpreadChance = 0.6f, CavitationPeakRadius = 7, CavitationDamage = -500f,
                TearMinRadius = 2.5f, TearMaxRadius = 5f, ExitTearDamage = -500f, MaxDepth = 200,
                HardTissueScale = 0.1f, PenetrationDeflect = 1f,
                RicochetAngle = 78f, MaxBounces = 2, RicochetEnergyLoss = 0.25f,
                // ──── View and recoil ───────────────────────────────────────────────
                ShakeAmount = 0.9f, ShotPitch = 0.32f, TracerSize = 0.07f,
                BoltBackTime = 0.05f, BoltCycleTime = 0.12f,
                RecoilKick = 0.030f, RecoilRise = 3.5f, RecoilRock = 1.4f,
                RecoilStiffness = 70f, RecoilDamping = 10f,
                ViewKick = 4.5f, ViewYaw = 0.7f, ViewStiffness = 55f, ViewDamping = 10f,  
                ScopeRadius = 0.2006f,
                ScopeFov    = 8f,
                AdsZoom = 0.85f, AdsTime = 0.30f,
                AdsSway = 0.22f, AdsSpread = 0f, AdsShake = 0.85f,
                // ──── Model ─────────────────────────────────────────────────────────────
                ShellLength = 0.099f, ShellRadius = 0.0102f,
                ShellForce = 2.6f, ShellSpin = 16f,
                ModelPos   = new Vector3(0.067f, -0.075f, 0.183f),
                ModelRot   = new Vector3(180f, 90.5f, 90f),
                MagOffset  = new Vector3(-1.0072f, -1.6918f,  0f),
                BoltOffset = new Vector3(-1.314f,  -3.9869f,  0f),
                BoltTravel = new Vector3(0f, -0.85f, 0f),
                MuzzleOffset = new Vector3(-1.1133f,  8.0276f, -0.0837f),
                EjectOffset  = new Vector3(-1.3000f, -2.0609f, -0.4928f),
                ScopeLens   = new Vector3(-2.1058f, -4.1300f, 0f),
                AdsOffset = new Vector3(-0.0980f, 0.0510f, -0.0520f),
                BodyMesh = "AS50Body_mesh", MagMesh = "AS50Mag_mesh", BoltMesh = "AS50Bolt_mesh",
                ModelScale = 0.0974f,
            },
        };

        private static int _index;

        public static WeaponProfile Current => All[Mathf.Clamp(_index, 0, All.Count - 1)];
        public static WeaponProfile Cycle(int direction)
        {
            if (All.Count == 0) return null;
            _index = ((_index + direction) % All.Count + All.Count) % All.Count;
            return Current;
        }
    }
}
