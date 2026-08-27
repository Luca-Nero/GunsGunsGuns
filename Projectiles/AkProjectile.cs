using System;
using System.Collections.Generic;
using GunsGunsGuns.Core;
using MelonLoader;
using UnityEngine;

namespace GunsGunsGuns.Projectiles
{
    // ── Travelling rounds ────────────────────────────────────────────────────────
    internal static class AkProjectiles
    {
        private sealed class Round
        {
            public Vector3    Pos;
            public Vector3    Vel;
            public GameObject Visual;
            public float      Age;
            public int        Bounces;
            public int        Penetrations;
            public float      Energy;   // 1 at the muzzle, drops with each ricochet / penetration
            public LineRenderer Trail;  // debug path, null unless DebugDrawPath
            public WeaponProfile Profile;  // the weapon that fired it — rounds keep their own rules
        }

        // ── Debug path drawing ────────────────────────────────────────────────────
        private sealed class Trail { public GameObject Obj; public float DieAt; }
        private static readonly List<Trail> _trails = new List<Trail>();
        private static Shader _lineShader;

        private static LineRenderer MakeTrail(Vector3 start)
        {
            var go = new GameObject("GGG_RoundPath");
            var lr = go.AddComponent<LineRenderer>();

            if (_lineShader == null)
                _lineShader = Shader.Find("Universal Render Pipeline/Unlit")
                           ?? Shader.Find("Sprites/Default");
            if (_lineShader != null) lr.material = new Material(_lineShader);

            lr.useWorldSpace   = true;
            lr.widthMultiplier = Mathf.Max(0.002f, Config.PathWidth);
            lr.numCapVertices  = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;

            var c = new Color(0.2f, 1.6f, 2.2f, 1f);   // >1 so it blooms
            lr.startColor = c;
            lr.endColor   = c;

            lr.positionCount = 1;
            lr.SetPosition(0, start);
            return lr;
        }

        private static void AppendTrail(Round r)
        {
            if (r.Trail == null) return;
            int n = r.Trail.positionCount;
            if (n >= Config.PathMaxPoints) return;
            r.Trail.positionCount = n + 1;
            r.Trail.SetPosition(n, r.Pos);
        }

        private static void ReleaseTrail(Round r)
        {
            if (r.Trail == null) return;
            AppendTrail(r);
            _trails.Add(new Trail { Obj = r.Trail.gameObject, DieAt = Time.time + Config.PathLifetime });
            r.Trail = null;
        }

        private static void TickTrails()
        {
            for (int i = _trails.Count - 1; i >= 0; i--)
            {
                if (Time.time < _trails[i].DieAt) continue;
                if (_trails[i].Obj != null) UnityEngine.Object.Destroy(_trails[i].Obj);
                _trails.RemoveAt(i);
            }
        }
        private static Vector3 FindExit(RaycastHit entry, Vector3 dir, WeaponProfile profile)
        {
            float maxDepth = Mathf.Max(0.05f, profile.MaxPenetrationDepth);
            Vector3 behind = entry.point + dir * maxDepth;

            if (Physics.Raycast(behind, -dir, out RaycastHit back, maxDepth,
                                ~0, QueryTriggerInteraction.Ignore)
                && back.collider == entry.collider)
                return back.point;

            return behind;
        }

        private static readonly List<Round> _rounds = new List<Round>();
        private static Shader _shader;

        public static void Spawn(Vector3 origin, Vector3 dir, WeaponProfile profile)
        {
            var r = new Round
            {
                Pos     = origin,
                Vel     = dir.normalized * Mathf.Max(1f, profile.MuzzleVelocity),
                Age     = 0f,
                Bounces = 0,
                Energy  = 1f,
                Profile = profile,
                Visual  = Config.ShowTracer ? MakeVisual(profile) : null,
                Trail   = Config.DebugDrawPath ? MakeTrail(origin) : null,
            };

            if (r.Visual != null) r.Visual.transform.position = origin;
            _rounds.Add(r);
        }

        public static void Clear()
        {
            foreach (var r in _rounds)
            {
                if (r.Visual != null) UnityEngine.Object.Destroy(r.Visual);
                if (r.Trail  != null) UnityEngine.Object.Destroy(r.Trail.gameObject);
            }
            _rounds.Clear();

            foreach (var t in _trails)
                if (t.Obj != null) UnityEngine.Object.Destroy(t.Obj);
            _trails.Clear();
        }

        public static void Tick()
        {
            TickTrails();

            if (_rounds.Count == 0) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            for (int i = _rounds.Count - 1; i >= 0; i--)
            {
                var r = _rounds[i];
                r.Age += dt;

                if (r.Age > r.Profile.RoundLifetime) { Kill(r, i); continue; }

                r.Vel += Vector3.up * (r.Profile.RoundGravity * dt);
                if (Config.ExternalForces && FruitLib.FruitForces.Any)
                    r.Vel += FruitLib.FruitForces.SampleAt(r.Pos) * (dt * Config.ExternalForceScale);
                Vector3 next = r.Pos + r.Vel * dt;
                Vector3 seg  = next - r.Pos;
                float   len  = seg.magnitude;

                if (len <= 0.0001f) { r.Pos = next; continue; }

                if (Physics.Raycast(r.Pos, seg / len, out RaycastHit hit, len,
                                    ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!Impact(r, hit)) { Kill(r, i); continue; }
                    // Ricocheted — Impact() has already repositioned and redirected it.
                }
                else
                {
                    r.Pos = next;
                }

                if (r.Visual != null) r.Visual.transform.position = r.Pos;
                AppendTrail(r);
            }
        }

        /// <summary>Returns true if the round survives (ricocheted), false if it should die.</summary>
        private static bool Impact(Round r, RaycastHit hit)
        {
            Vector3 dir = r.Vel.normalized;

            if (AkHitscan.IsLimb(hit.collider.gameObject))
            {
                AkHitscan.ApplyToLimb(hit, dir, r.Profile, r.Energy);
                AkImpacts.PlayFlesh(hit.point);
                r.Penetrations++;
                r.Energy *= Mathf.Clamp01(1f - r.Profile.PenetrationLoss);

                if (r.Penetrations > r.Profile.MaxPenetrations || r.Energy < Config.MinDepth)
                    return false;

                Vector3 exit = FindExit(hit, dir, r.Profile);
                Vector3 outDir = r.Profile.PenetrationDeflect > 0f
                    ? (Quaternion.Euler(
                           UnityEngine.Random.Range(-r.Profile.PenetrationDeflect, r.Profile.PenetrationDeflect),
                           UnityEngine.Random.Range(-r.Profile.PenetrationDeflect, r.Profile.PenetrationDeflect),
                           0f) * dir).normalized
                    : dir;

                r.Pos = exit + outDir * 0.02f;
                r.Vel = outDir * (r.Vel.magnitude * Mathf.Clamp01(1f - r.Profile.PenetrationLoss));
                return true;
            }
            var rb = hit.collider.attachedRigidbody;
            if (rb != null)
            {
                try
                {
                    rb.AddForceAtPosition(dir * (r.Profile.WorldImpulse * r.Energy),
                                          hit.point, ForceMode.Impulse);
                }
                catch { }
            }
            float incidence = Vector3.Angle(-dir, hit.normal);
            bool  grazing   = incidence >= r.Profile.RicochetAngle;

            AkImpacts.Spawn(hit.point, hit.normal, dir, incidence, r.Energy);
            if (grazing) AkImpacts.PlaySkid(hit.point);
            else         AkImpacts.PlayHard(hit.point);

            if (grazing && r.Bounces < r.Profile.MaxBounces)
            {
                r.Bounces++;
                r.Energy *= Mathf.Clamp01(1f - r.Profile.RicochetEnergyLoss);

                Vector3 reflected = Vector3.Reflect(dir, hit.normal);

                if (r.Profile.RicochetScatter > 0f)
                    reflected = Quaternion.Euler(
                        UnityEngine.Random.Range(-r.Profile.RicochetScatter, r.Profile.RicochetScatter),
                        UnityEngine.Random.Range(-r.Profile.RicochetScatter, r.Profile.RicochetScatter),
                        0f) * reflected;

                r.Vel = reflected * (r.Vel.magnitude * Mathf.Clamp01(1f - r.Profile.RicochetEnergyLoss));
                r.Pos = hit.point + hit.normal * 0.02f;   // lift off the surface, don't re-hit it
                return true;
            }

            return false;
        }

        private static void Kill(Round r, int index)
        {
            ReleaseTrail(r);
            if (r.Visual != null) UnityEngine.Object.Destroy(r.Visual);
            _rounds.RemoveAt(index);
        }

        private static GameObject MakeVisual(WeaponProfile profile)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());   // the raycast is the round
            go.transform.localScale = Vector3.one * Mathf.Max(0.005f, profile.TracerSize);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (_shader == null)
                    _shader = Shader.Find("Universal Render Pipeline/Unlit")
                           ?? Shader.Find("Universal Render Pipeline/Lit")
                           ?? Shader.Find("Sprites/Default");

                if (_shader != null) mr.material = new Material(_shader);

                var mat = mr.material;
                if (mat != null)
                {
                    // Above 1 so it blooms — HDR is what sells a tracer.
                    var c = new Color(2.4f, 1.5f, 0.5f, 1f);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    if (mat.HasProperty("_Color"))     mat.SetColor("_Color", c);
                }

                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows    = false;
            }

            return go;
        }
    }
}
