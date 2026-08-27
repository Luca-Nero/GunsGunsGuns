using System.Collections.Generic;
using FruitLib;
using GunsGunsGuns.Core;
using Il2CppInfrastructure.Project.Installers.AssetsHandlers.SFX;
using UnityEngine;
using UnityEngine.Rendering;

namespace GunsGunsGuns.Projectiles
{
    // ── Surface impacts: marks and sound ─────────────────────────────────────────
    internal static class AkImpacts
    {
        private sealed class Mark
        {
            public GameObject Obj;
            public Material   Mat;
            public float      Age;
            public float      Life;
            public Color      Base;
        }

        private static readonly List<Mark> _marks = new List<Mark>();
        private static Shader _shader;

        // ── Sound ─────────────────────────────────────────────────────────────────

        public static void PlayHard(Vector3 pos)  => Play(ImpactSFXType.Bullet9MMHardSurface, pos);
        public static void PlayFlesh(Vector3 pos) => Play(ImpactSFXType.Bullet9MMToBodyOrganic, pos);
        public static void PlaySkid(Vector3 pos)  => Play(ImpactSFXType.RbHit, pos);
        private static void Play(ImpactSFXType type, Vector3 pos)
        {
            var clip = FruitSfx.Impact(type);
            if (clip == null) return;

            var src = SourceAt(pos);
            if (src == null) return;

            src.pitch = Mathf.Clamp(Config.ImpactPitch, 0.1f, 3f);
            src.PlayOneShot(clip, Mathf.Clamp01(Config.ImpactVolume));
        }

        private static AudioSource[] _sources;
        private static int _sourceIdx;

        private static AudioSource SourceAt(Vector3 pos)
        {
            if (_sources == null)
            {
                _sources = new AudioSource[16];
                for (int i = 0; i < _sources.Length; i++)
                {
                    var go = new GameObject("GGG_SfxEmitter" + i);
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.HideAndDontSave;

                    var s = go.AddComponent<AudioSource>();
                    s.playOnAwake  = false;
                    s.spatialBlend = 1f;                       // fully positional
                    s.rolloffMode  = AudioRolloffMode.Linear;
                    s.minDistance  = 2f;
                    s.maxDistance  = Mathf.Max(5f, Config.ImpactMaxDistance);
                    _sources[i] = s;
                }
            }

            var src = _sources[_sourceIdx];
            _sourceIdx = (_sourceIdx + 1) % _sources.Length;

            if (src != null) src.transform.position = pos;
            return src;
        }

        // ── Marks ─────────────────────────────────────────────────────────────────

        public static void Spawn(Vector3 point, Vector3 normal, Vector3 travelDir, float incidence, float energy)
        {
            if (!Config.ShowImpactMarks) return;
            float graze = Mathf.Clamp01(incidence / 90f);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());

            go.transform.position = point + normal * 0.01f;

            Vector3 tangent = Vector3.ProjectOnPlane(travelDir, normal);
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.right;
            go.transform.rotation = Quaternion.LookRotation(normal, tangent.normalized);

            float size    = Mathf.Max(0.005f, Config.MarkSize) * Mathf.Lerp(1f, 0.6f, graze);
            float stretch = 1f + graze * Config.MarkSkidStretch;
            go.transform.localScale = new Vector3(size, size * stretch, 1f);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) { UnityEngine.Object.Destroy(go); return; }

            if (_shader == null)
                _shader = Shader.Find("Universal Render Pipeline/Unlit")
                       ?? Shader.Find("Sprites/Default");

            var mat = _shader != null ? new Material(_shader) : mr.material;
            MakeTransparent(mat);
            float alpha = Mathf.Clamp01(Mathf.Lerp(Config.MarkAlpha, Config.MarkAlpha * 0.35f, graze) * energy);
            var baseCol = new Color(0.05f, 0.04f, 0.03f, alpha);

            mr.material          = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows    = false;
            SetColor(mat, baseCol);

            _marks.Add(new Mark { Obj = go, Mat = mat, Age = 0f, Life = Config.MarkLifetime, Base = baseCol });

            while (_marks.Count > Mathf.Max(1, Config.MaxMarks)) Kill(0);
        }

        public static void Tick()
        {
            if (_marks.Count == 0) return;

            float dt = Time.deltaTime;
            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                var m = _marks[i];
                m.Age += dt;

                if (m.Obj == null || m.Age >= m.Life) { Kill(i); continue; }

                float t = m.Age / m.Life;
                if (t > 0.75f)
                {
                    float k = 1f - (t - 0.75f) / 0.25f;
                    SetColor(m.Mat, new Color(m.Base.r, m.Base.g, m.Base.b, m.Base.a * k));
                }
            }
        }

        public static void Clear()
        {
            for (int i = _marks.Count - 1; i >= 0; i--) Kill(i);
        }

        private static void Kill(int index)
        {
            var m = _marks[index];
            if (m.Obj != null) UnityEngine.Object.Destroy(m.Obj);
            _marks.RemoveAt(index);
        }

        private static void SetColor(Material mat, Color c)
        {
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", c);
        }
        private static void MakeTransparent(Material mat)
        {
            if (mat == null) return;
            try
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            catch { }
        }
    }
}
