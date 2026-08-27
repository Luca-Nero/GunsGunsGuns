using System;
using System.Collections.Generic;
using GunsGunsGuns.Core;
using Il2CppSpawnables.Weapons;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;

namespace GunsGunsGuns.Projectiles
{
    // ── Spent cases ──────────────────────────────────────────────────────────────
    internal static class AkShells
    {
        public static void Eject(WeaponProfile p)
        {
            if (!Config.ShowShells) return;

            var port = AkModel.EjectPort;
            if (port == null) return;

            if (!Config.CustomShells && TryNative(port, p)) return;
            Custom(port, p);
        }

        private static ShellsEjectionController _ctl;
        private static float _nextLookup;
        private static bool  _nativeFailed;

        private static bool TryNative(Transform port, WeaponProfile p)
        {
            var ctl = Controller();
            if (ctl == null) return false;

            var savedPort   = ctl.rca;
            var savedWeapon = ctl.rcf;
            float savedMinF = ctl.m_minForce,  savedMaxF = ctl.m_maxForce;
            float savedMinT = ctl.m_minTorque, savedMaxT = ctl.m_maxTorque;

            try
            {
                ctl.rca = port;
                ctl.rcf = port;
                ctl.m_minForce  = p.ShellForce * 0.7f;
                ctl.m_maxForce  = p.ShellForce * 1.3f;
                ctl.m_minTorque = p.ShellSpin  * 0.6f;
                ctl.m_maxTorque = p.ShellSpin  * 1.4f;

                ctl.gei();
                return true;
            }
            catch (Exception e)
            {
                if (!_nativeFailed)
                {
                    _nativeFailed = true;
                    MelonLogger.Warning($"[AK] native shell ejection failed, using built shells instead: {e.Message}");
                }
                return false;
            }
            finally
            {
                ctl.rca = savedPort;
                ctl.rcf = savedWeapon;
                ctl.m_minForce  = savedMinF;  ctl.m_maxForce  = savedMaxF;
                ctl.m_minTorque = savedMinT;  ctl.m_maxTorque = savedMaxT;
            }
        }

        private static ShellsEjectionController Controller()
        {
            if (_nativeFailed) return null;
            if (_ctl != null) return _ctl;

            if (Time.time < _nextLookup) return null;
            _nextLookup = Time.time + 2f;

            _ctl = UnityEngine.Object.FindObjectOfType<ShellsEjectionController>(true);
            if (_ctl != null && _ctl.rbz == null)
            {
                // Never initialised, so EjectInternal has no velocity source to read.
                _ctl = null;
            }
            return _ctl;
        }

        // ── Ours ──────────────────────────────────────────────────────────────────

        private sealed class Case
        {
            public GameObject Go;
            public float      Dies;
        }

        private static readonly List<Case> _live = new List<Case>();
        private static Mesh     _mesh;
        private static Material _mat;

        private static void Custom(Transform port, WeaponProfile p)
        {
            var go = new GameObject("GGG_Shell");
            go.transform.position = port.position;
            go.transform.rotation = port.rotation;
            go.transform.localScale = new Vector3(p.ShellRadius * 2f, p.ShellRadius * 2f, p.ShellLength);

            go.AddComponent<MeshFilter>().mesh = CaseMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material          = CaseMaterial();
            mr.shadowCastingMode = ShadowCastingMode.Off;

            var col = go.AddComponent<CapsuleCollider>();
            col.direction = 2;      // along Z, the case's long axis
            col.radius    = 0.5f;
            col.height    = 1f;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.005f, p.ShellRadius * p.ShellLength * 900f);
            rb.linearVelocity = port.right   * UnityEngine.Random.Range(p.ShellForce * 0.7f, p.ShellForce * 1.3f)
                              + port.up      * UnityEngine.Random.Range(p.ShellForce * 0.3f, p.ShellForce * 0.7f)
                              + port.forward * UnityEngine.Random.Range(-0.4f, 0.4f);
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * p.ShellSpin;

            _live.Add(new Case { Go = go, Dies = Time.time + Config.ShellLifetime });

            while (_live.Count > Config.MaxShells) Kill(0);
        }

        public static void Tick()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
                if (_live[i].Go == null || Time.time > _live[i].Dies) Kill(i);
        }

        public static void Clear()
        {
            // The scene took the objects with it; just drop the handles.
            _live.Clear();
            _ctl = null;
        }

        private static void Kill(int i)
        {
            if (_live[i].Go != null) UnityEngine.Object.Destroy(_live[i].Go);
            _live.RemoveAt(i);
        }

        private static Material CaseMaterial()
        {
            if (_mat != null) return _mat;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            _mat = new Material(shader) { hideFlags = HideFlags.DontUnloadUnusedAsset };

            var brass = new Color(0.72f, 0.55f, 0.20f);
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", brass);
            if (_mat.HasProperty("_Metallic"))  _mat.SetFloat("_Metallic", 0.9f);
            if (_mat.HasProperty("_Smoothness")) _mat.SetFloat("_Smoothness", 0.7f);
            _mat.color = brass;
            return _mat;
        }
        private static Mesh CaseMesh()
        {
            if (_mesh != null) return _mesh;

            const int sides = 10;
            var verts = new List<Vector3>();
            var tris  = new List<int>();

            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float x = Mathf.Cos(a) * 0.5f, y = Mathf.Sin(a) * 0.5f;
                verts.Add(new Vector3(x, y, -0.5f));                // head
                verts.Add(new Vector3(x * 0.88f, y * 0.88f, 0.5f)); // mouth, slightly tapered
            }

            for (int i = 0; i < sides; i++)
            {
                int a = i * 2, b = a + 1;
                int c = ((i + 1) % sides) * 2, d = c + 1;
                tris.AddRange(new[] { a, b, c, c, b, d });
            }

            int centre = verts.Count;
            verts.Add(new Vector3(0f, 0f, -0.5f));
            for (int i = 0; i < sides; i++)
                tris.AddRange(new[] { centre, ((i + 1) % sides) * 2, i * 2 });

            _mesh = new Mesh { hideFlags = HideFlags.DontUnloadUnusedAsset };
            _mesh.SetVertices(verts.ToArray());
            _mesh.SetTriangles(tris.ToArray(), 0);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            return _mesh;
        }
    }
}
