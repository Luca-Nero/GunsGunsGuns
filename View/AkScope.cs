using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using GunsGunsGuns.Core;

namespace GunsGunsGuns.View
{
    // ── Picture-in-picture scope ─────────────────────────────────────────────────
    internal static class AkScope
    {
        private static Camera        _cam;
        private static RenderTexture _rt;
        private static Material      _mat;
        private static Mesh          _disc;
        private static Transform     _lens;
        private static bool          _failed;

        public static bool Active => _lens != null && _cam != null;

        public static void Reset()
        {
            _cam = null; _lens = null;
            _failed = false;
        }

        public static void Detach()
        {
            if (_cam != null) _cam.transform.SetParent(null, false);
            _lens = null;
        }

        public static void Refresh(WeaponProfile p)
        {
            if (_lens == null) return;
            _lens.localPosition = p.ScopeLens;
            _lens.localScale    = Vector3.one * Mathf.Max(0.001f, p.ScopeRadius);
        }

        public static void Attach(Transform root, WeaponProfile p)
        {
            _lens = null;
            if (!Config.ShowScope || p.ScopeRadius <= 0f || _failed) return;

            try
            {
                var go = new GameObject("ScopeLens");
                go.transform.SetParent(root, false);
                go.transform.localPosition = p.ScopeLens;
                // Face the eye: cancel the model's pose, then turn to look back down -Z.
                go.transform.localRotation = Quaternion.Inverse(Quaternion.Euler(p.ModelRot));
                go.transform.localScale    = Vector3.one * p.ScopeRadius;

                go.AddComponent<MeshFilter>().mesh = Disc();
                var mr = go.AddComponent<MeshRenderer>();
                mr.material          = LensMaterial();
                mr.shadowCastingMode  = ShadowCastingMode.Off;
                mr.receiveShadows     = false;

                _lens = go.transform;
                EnsureCamera();
                if (_cam != null)
                {
                    _cam.transform.SetParent(_lens, false);
                    _cam.transform.localPosition = Vector3.zero;
                    _cam.transform.localRotation = Quaternion.identity;
                }
            }
            catch (Exception e)
            {
                _failed = true;
                MelonLogger.Warning($"[AK] scope unavailable: {e.Message}");
            }
        }

        private static float _nextIdleFrame;

        public static void Tick()
        {
            if (_lens == null || _cam == null) return;
            if (!Config.ShowScope) { if (_cam.enabled) _cam.enabled = false; return; }

            var p = Profiles.Current;
            _cam.fieldOfView = Mathf.Max(1f, p.ScopeFov);
            if (AkAds.Blend > 0.05f) { _cam.enabled = true; return; }

            float period = 1f / Mathf.Clamp(Config.ScopeIdleHz, 1f, 60f);
            bool due = Time.unscaledTime >= _nextIdleFrame;

            _cam.enabled = due;
            if (due) _nextIdleFrame = Time.unscaledTime + period;
        }

        private static void EnsureCamera()
        {
            if (_cam != null) return;

            var baseCam = Camera.main;
            if (baseCam == null) return;

            int size = Mathf.Clamp(Config.ScopeResolution, 128, 2048);
            if (_rt == null || _rt.width != size)
            {
                _rt = new RenderTexture(size, size, 24) { hideFlags = HideFlags.DontUnloadUnusedAsset };
                _rt.Create();
                if (_mat != null) SetLensTexture(_mat, _rt);
            }

            var go = new GameObject("GGG_ScopeCam");
            _cam = go.AddComponent<Camera>();
            _cam.targetTexture = _rt;
            _cam.clearFlags    = CameraClearFlags.Skybox;
            _cam.nearClipPlane = 0.05f;
            _cam.farClipPlane  = baseCam.farClipPlane;

            _cam.cullingMask = AkViewmodel.Layer >= 0 ? ~(1 << AkViewmodel.Layer) : ~0;
            _cam.enabled = false;

            MelonLogger.Msg($"[AK] scope camera ready at {size}x{size}.");
        }

        private static Material LensMaterial()
        {
            if (_mat != null) return _mat;

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Texture")
                      ?? Shader.Find("Universal Render Pipeline/Lit");

            _mat = new Material(shader) { hideFlags = HideFlags.DontUnloadUnusedAsset };
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", Color.white);
            // Two-sided, so a disc wound the wrong way still shows rather than vanishing.
            if (_mat.HasProperty("_Cull")) _mat.SetFloat("_Cull", 0f);
            if (_rt != null) SetLensTexture(_mat, _rt);
            return _mat;
        }

        private static void SetLensTexture(Material m, Texture tex)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            m.mainTexture = tex;
        }

        private static Mesh Disc()
        {
            if (_disc != null) return _disc;

            const int sides = 48;
            var verts = new List<Vector3> { Vector3.zero };
            var uvs   = new List<Vector2> { new Vector2(0.5f, 0.5f) };
            var tris  = new List<int>();

            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float x = Mathf.Cos(a), y = Mathf.Sin(a);
                verts.Add(new Vector3(x, y, 0f));
                uvs.Add(new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f));
            }

            for (int i = 0; i < sides; i++)
            {
                int a = i + 1, b = (i + 1) % sides + 1;
                tris.Add(0); tris.Add(b); tris.Add(a);   // wound to face -Z
            }

            _disc = new Mesh { hideFlags = HideFlags.DontUnloadUnusedAsset };
            _disc.SetVertices(verts.ToArray());
            _disc.SetUVs(0, uvs.ToArray());
            _disc.SetTriangles(tris.ToArray(), 0);
            _disc.RecalculateNormals();
            _disc.RecalculateBounds();
            return _disc;
        }
    }
}
