using System.Collections.Generic;
using UnityEngine;

namespace GunsGunsGuns.Core
{
    internal sealed class WeaponProfile
    {
        public string Name      = "Weapon";
        public Color  IconColor = Color.white;

        // ── Firing ────────────────────────────────────────────────────────────
        public float FireRateRPM   = 600f;
        public int   Pellets       = 1;     // >1 makes it a shotgun
        public float SpreadDegrees = 0f;    // cone half-angle per pellet

        // ── Ballistics ────────────────────────────────────────────────────────
        public float MuzzleVelocity = 220f;
        public float RoundGravity   = -9.81f;
        public float RoundLifetime  = 4f;

        // ── Impact ────────────────────────────────────────────────────────────
        public float ImpactImpulse = 140f;
        public float ImpactTorque  = 5f;
        public float WorldImpulse  = 40f;

        // ── Penetration ───────────────────────────────────────────────────────
        public int   MaxPenetrations     = 3;
        public float PenetrationLoss     = 0.35f;
        public float MaxPenetrationDepth = 0.8f;
        public float PenetrationDeflect  = 3f;

        // ── Ricochet ──────────────────────────────────────────────────────────
        public float RicochetAngle      = 65f;
        public int   MaxBounces         = 3;
        public float RicochetEnergyLoss = 0.35f;
        public float RicochetScatter    = 4f;

        // ── Wound ─────────────────────────────────────────────────────────────
        public float DepthScale  = 1f;
        public float ConeRadius0 = 1f;
        public float ConeRadius1 = 3f;

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
                Name = "AK-47", IconColor = new Color(0.85f, 0.55f, 0.15f),
                FireRateRPM = 600f, Pellets = 1, SpreadDegrees = 0.2f,
                MuzzleVelocity = 220f,
                ImpactImpulse = 80f, ImpactTorque = 5f, WorldImpulse = 40f,
                MaxPenetrations = 3, PenetrationLoss = 0.35f, MaxPenetrationDepth = 0.8f,
                DepthScale = 1f, ConeRadius0 = 1f, ConeRadius1 = 3f,
                ShakeAmount = 0.05f, ShotPitch = 0.65f, TracerSize = 0.04f,
                RecoilKick = 0.01f, RecoilRise = 1f, RecoilRock = 0.5f,
                RecoilStiffness = 90f, RecoilDamping = 11f,
                ViewKick = 5.0f, ViewYaw = 0.9f, ViewStiffness = 70f, ViewDamping = 11f,
                BodyMesh = "AKBody_mesh", MagMesh = "AKMag_mesh", BoltMesh = "AKBolt_mesh",
                ModelScale = 0.09f,
                ModelPos   = new Vector3(-0.013f, 0.09f, -0.067f),
                ModelRot   = new Vector3(180f, 90f, 90.5f),
                MagOffset  = new Vector3( 0.083344f, -0.62418f, 0f),
                BoltOffset = new Vector3(-0.11936f,  -1.2398f,  0f),
                BoltTravel = new Vector3(0f, -0.55f, 0f),
                MuzzleOffset = new Vector3( 0f, 4.0411f, 0f),
                AdsOffset    = new Vector3(-0.1160f, 0.1000f, -0.0100f),
                EjectOffset  = new Vector3(-0.1389f, -0.4396f, -0.7507f),
                ShellLength = 0.039f, ShellRadius = 0.0057f,
            },

            // Shotgun
            new WeaponProfile
            {
                Name = "RM870", IconColor = new Color(0.75f, 0.2f, 0.2f),
                FireRateRPM = 30f, Pellets = 12, SpreadDegrees = 4.5f,
                MuzzleVelocity = 300f, RoundLifetime = 2.5f,
                ImpactImpulse = 90f, ImpactTorque = 6f, WorldImpulse = 25f,
                MaxPenetrations = 1, PenetrationLoss = 0.75f, MaxPenetrationDepth = 0.3f,
                RicochetAngle = 72f, MaxBounces = 1, RicochetEnergyLoss = 0.6f,
                DepthScale = 0.55f, ConeRadius0 = 1f, ConeRadius1 = 2f,
                ShakeAmount = 0.55f, ShotPitch = 0.45f, TracerSize = 0.03f,
                BodyMesh = "RM870Body_mesh", BoltMesh = "RM870Bolt_and_ForeEnd_mesh",
                ModelScale = 0.13f,
                ModelPos   = new Vector3(0.067f, -0.005f, 0.283f),
                ModelRot   = new Vector3(0f, -90f, 0f),
                BoltOffset = new Vector3(-2.21498f, 0.432121f, 0f),
                BoltTravel = new Vector3(-1.0f, 0f, 0f),
                BoltBackTime = 0.12f, BoltDwell = 0.2f, BoltCycleTime = 0.25f,
                CycleDelay = 0.25f,
                RecoilKick = 0.022f, RecoilRise = 3f, RecoilRock = 1.6f,
                RecoilStiffness = 90f, RecoilDamping = 11f,
                ViewKick = 3.2f, ViewYaw = 0.9f, ViewStiffness = 70f, ViewDamping = 11f,
                MuzzleOffset = new Vector3( 4.0269f,  0.7308f,  0f),
                EjectOffset  = new Vector3(-1.3462f, -0.1577f, -0.0462f),
                ShellLength = 0.070f, ShellRadius = 0.0102f,
                ShellForce = 1.8f, ShellSpin = 10f,
                AdsZoom = 0.60f, AdsTime = 0.14f,
                AdsOffset = new Vector3(-0.1160f, 0.1410f, -0.2580f),
            },

            // Anti-materiel rifle
            new WeaponProfile
            {
                Name = "AS50", IconColor = new Color(0.35f, 0.75f, 0.95f),
                FireRateRPM = 35f, Pellets = 1, SpreadDegrees = 0f,
                MuzzleVelocity = 800f, RoundGravity = -6f, RoundLifetime = 12f,
                ImpactImpulse = 600f, ImpactTorque = 14f, WorldImpulse = 100f,
                MaxPenetrations = 12, PenetrationLoss = 0.12f, MaxPenetrationDepth = 2.5f,
                PenetrationDeflect = 1f,
                RicochetAngle = 78f, MaxBounces = 2, RicochetEnergyLoss = 0.25f,
                DepthScale = 2.4f, ConeRadius0 = 2f, ConeRadius1 = 6f,
                ShakeAmount = 0.9f, ShotPitch = 0.32f, TracerSize = 0.07f,
                BodyMesh = "AS50Body_mesh", MagMesh = "AS50Mag_mesh", BoltMesh = "AS50Bolt_mesh",
                ModelScale = 0.0974f,
                ModelPos   = new Vector3(0.067f, -0.075f, 0.183f),
                ModelRot   = new Vector3(180f, 90.5f, 90f),
                MagOffset  = new Vector3(-1.0072f, -1.6918f,  0f),
                BoltOffset = new Vector3(-1.314f,  -3.9869f,  0f),
                BoltTravel = new Vector3(0f, -0.85f, 0f),
                BoltBackTime = 0.05f, BoltCycleTime = 0.12f,
                RecoilKick = 0.030f, RecoilRise = 3.5f, RecoilRock = 1.4f,
                RecoilStiffness = 70f, RecoilDamping = 10f,
                ViewKick = 4.5f, ViewYaw = 0.7f, ViewStiffness = 55f, ViewDamping = 10f,
                MuzzleOffset = new Vector3(-1.1133f,  8.0276f, -0.0837f),
                EjectOffset  = new Vector3(-1.3000f, -2.0609f, -0.4928f),
                ShellLength = 0.099f, ShellRadius = 0.0102f,
                ShellForce = 2.6f, ShellSpin = 16f,
                ScopeLens   = new Vector3(-2.1058f, -4.1300f, 0f),
                ScopeRadius = 0.2006f,
                ScopeFov    = 8f,
                AdsZoom = 0.85f, AdsTime = 0.30f,
                AdsOffset = new Vector3(-0.0980f, 0.0510f, -0.0520f),
                AdsSway = 0.22f, AdsSpread = 0f, AdsShake = 0.85f,
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
