using System.Collections.Generic;
using FruitLib;
using GunsGunsGuns.Core;
using UnityEngine;

namespace GunsGunsGuns.Projectiles
{
    // ── Tracers ──────────────────────────────────────────────────────────────────
    //
    // The rounds themselves are FruitLib's (FruitBallistics): it flies them with real drag,
    // walks them through bodies with the game's own wound model, ricochets them. What stays
    // here is how a GunsGunsGuns round looks - the glowing tracer and the debug path - hung
    // off FruitLib's events and drawn only for this mod's specs, so another mod's portal
    // gun does not come out looking like an AK round.
    //
    // Recognised by spec id, not Tag: ProjectileSpawned is raised inside SpawnProjectile,
    // before the caller gets the round back to tag it. It also means a remote player's
    // GunsGunsGuns shot, replayed here as cosmetic, gets a tracer too.
    internal static class AkProjectiles
    {
        private sealed class Look
        {
            public Projectile   Round;
            public GameObject   Visual;
            public LineRenderer Trail;
        }

        private sealed class Fading { public GameObject Obj; public float DieAt; }

        private static readonly Dictionary<int, Look> _looks = new Dictionary<int, Look>();
        private static readonly List<Fading> _fading = new List<Fading>();
        private static Shader _shader, _lineShader;
        private static bool _hooked;

        public static void Init()
        {
            if (_hooked) return;
            _hooked = true;
            FruitBallistics.ProjectileSpawned += OnSpawned;
            FruitBallistics.ProjectileEnded   += OnEnded;
            FruitBallistics.ProjectilesMoved  += Follow;
        }

        internal static WeaponProfile ProfileOf(Projectile r)
        {
            string id = r?.Spec?.Id;
            if (id == null) return null;
            foreach (var p in Profiles.All) if (p != null && p.SpecId == id) return p;
            return null;
        }

        private static void OnSpawned(Projectile r)
        {
            var p = ProfileOf(r);
            if (p == null) return;

            var look = new Look
            {
                Round  = r,
                Visual = Config.ShowTracer ? MakeVisual(p) : null,
                Trail  = Config.DebugDrawPath ? MakeTrail(r.Position) : null,
            };
            if (look.Visual != null) look.Visual.transform.position = r.Position;
            _looks[r.Id] = look;
        }

        private static void OnEnded(Projectile r)
        {
            if (!_looks.TryGetValue(r.Id, out var look)) return;
            _looks.Remove(r.Id);

            if (look.Visual != null) Object.Destroy(look.Visual);
            if (look.Trail != null)
            {
                Append(look.Trail, r.Position);
                _fading.Add(new Fading { Obj = look.Trail.gameObject, DieAt = Time.time + Config.PathLifetime });
            }
        }

        /// <summary>FruitLib's ProjectilesMoved: straight after the rounds moved, so tracers
        /// never trail a frame behind.</summary>
        private static void Follow()
        {
            foreach (var look in _looks.Values)
            {
                var pos = look.Round.Position;
                if (look.Visual != null) look.Visual.transform.position = pos;
                if (look.Trail  != null) Append(look.Trail, pos);
            }
        }

        /// <summary>Retires finished debug paths.</summary>
        public static void Tick()
        {
            for (int i = _fading.Count - 1; i >= 0; i--)
            {
                if (Time.time < _fading[i].DieAt) continue;
                if (_fading[i].Obj != null) Object.Destroy(_fading[i].Obj);
                _fading.RemoveAt(i);
            }
        }

        public static void Clear()
        {
            foreach (var look in _looks.Values)
            {
                if (look.Visual != null) Object.Destroy(look.Visual);
                if (look.Trail  != null) Object.Destroy(look.Trail.gameObject);
            }
            _looks.Clear();

            foreach (var f in _fading) if (f.Obj != null) Object.Destroy(f.Obj);
            _fading.Clear();
        }

        // ── Visuals ──────────────────────────────────────────────────────────────

        private static GameObject MakeVisual(WeaponProfile profile)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());   // FruitLib's raycast is the round
            go.transform.localScale = Vector3.one * Mathf.Max(0.005f, profile.TracerSize);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                _shader ??= Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Sprites/Default");
                if (_shader != null) mr.material = new Material(_shader);

                var mat = mr.material;
                if (mat != null)
                {
                    // Above 1 so it blooms - HDR is what sells a tracer.
                    var c = new Color(2.4f, 1.5f, 0.5f, 1f);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    if (mat.HasProperty("_Color"))     mat.SetColor("_Color", c);
                }

                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows    = false;
            }
            return go;
        }

        private static LineRenderer MakeTrail(Vector3 start)
        {
            var lr = new GameObject("GGG_RoundPath").AddComponent<LineRenderer>();
            _lineShader ??= Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (_lineShader != null) lr.material = new Material(_lineShader);

            lr.useWorldSpace     = true;
            lr.widthMultiplier   = Mathf.Max(0.002f, Config.PathWidth);
            lr.numCapVertices    = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            lr.startColor = lr.endColor = new Color(0.2f, 1.6f, 2.2f, 1f);   // >1 so it blooms
            lr.positionCount = 1;
            lr.SetPosition(0, start);
            return lr;
        }

        private static void Append(LineRenderer lr, Vector3 pos)
        {
            int n = lr.positionCount;
            if (n >= Config.PathMaxPoints) return;
            lr.positionCount = n + 1;
            lr.SetPosition(n, pos);
        }
    }
}
