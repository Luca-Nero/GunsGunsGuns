using FruitLib;
using GunsGunsGuns.View;
using Il2CppPlayer.Appearances.God.Toolbar;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;

namespace GunsGunsGuns.Core
{
    // ── The held weapon model ────────────────────────────────────────────────────
    internal static class AkModel
    {
        public static FruitMeshLibrary Meshes;

        private static GameObject _root;
        private static Transform  _body, _mag, _bolt;
        private static Material   _mat;

        private static bool          _equipped;   // the slot is selected
        private static WeaponProfile _built;      // profile the current rig was built from

        private static bool  _spawnPending;
        private static float _spawnDeadline;
        private const  float SpawnWindow = 3f;

        public static bool IsSpawned => _root != null;

        // ── Spawn / despawn ───────────────────────────────────────────────────────
        public static void RequestSpawn()
        {
            _equipped      = true;
            _spawnPending  = true;
            _spawnDeadline = Time.time + SpawnWindow;
        }

        public static void Despawn()
        {
            _equipped     = false;
            _spawnPending = false;
            _built        = null;
            Teardown();
        }

        public static void OnSceneReload()
        {
            _equipped = false; _spawnPending = false; _built = null;
            _root = null; _body = _mag = _bolt = null;
            _muzzle = _port = null;
            _mat  = null;
            AkViewmodel.Reset();
        }

        public static void Tick()
        {
            if (_spawnPending)
            {
                if (IsSpawned) _spawnPending = false;
                else if (Time.time > _spawnDeadline)
                {
                    _spawnPending = false;
                    MelonLogger.Warning($"[AK] gave up waiting for SelectedItemPivot after {SpawnWindow}s.");
                }
                else if (FindSelectedItemPivot() != null)
                {
                    _spawnPending = false;
                    Spawn();
                }
            }
            // Weapon switched while it was in hand — swap the rig for the new gun's parts.
            else if (_equipped && !ReferenceEquals(_built, Profiles.Current))
            {
                Teardown();
                Spawn();
            }

            TickBolt();
            TickRecoil();

            // Unconditional: the aim blend moves the gun on frames where nothing else does.
            ApplyRootTransform();
            AkViewmodel.Sync();
        }

        private static void SetLayerTree(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerTree(t.GetChild(i), layer);
        }

        private static void Teardown()
        {
            ResetRecoil();
            _muzzle = _port = null;
            AkScope.Detach();
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _body = _mag = _bolt = null;
        }

        private static void Spawn()
        {
            if (IsSpawned || !Config.ShowModel) return;

            var pivot = FindSelectedItemPivot();
            if (pivot == null) return;   // leave _built alone so the next tick retries

            var p = Profiles.Current;
            _built = p;                  // claim it either way, so a modelless weapon doesn't retry forever

            if (string.IsNullOrEmpty(p.BodyMesh)) return;
            if (Meshes == null || Meshes.Count == 0) { MelonLogger.Warning("[AK] no meshes loaded."); return; }

            AkViewmodel.Ensure(Camera.main);

            _root = new GameObject("GGG_Weapon");
            _root.transform.SetParent(pivot, false);
            ApplyRootTransform();

            _body = AddPart("Body", p.BodyMesh, Vector3.zero);
            _mag  = AddPart("Mag",  p.MagMesh,  p.MagOffset);
            _bolt = AddPart("Bolt", p.BoltMesh, p.BoltOffset);

            if (_bolt != null) _boltHome = _bolt.localPosition;
            Advance(StageIdle);
            ResetRecoil();

            _muzzle = AddPort("Muzzle", p.MuzzleOffset);
            _port   = AddPort("EjectPort", p.EjectOffset);
            AkScope.Attach(_root.transform, p);

            // Layer last, so it catches every part and port in one pass.
            if (AkViewmodel.Layer >= 0) SetLayerTree(_root.transform, AkViewmodel.Layer);

            MelonLogger.Msg($"[AK] {p.Name} model built under '{pivot.name}' " +
                            $"(body:{_body != null} mag:{_mag != null} bolt:{_bolt != null})");
        }

        private static Transform AddPart(string label, string meshName, Vector3 localOffset)
        {
            if (string.IsNullOrEmpty(meshName)) return null;

            var mesh = Meshes.GetMesh(meshName);
            if (mesh == null) { MelonLogger.Warning($"[AK] mesh '{meshName}' missing."); return null; }

            var go = new GameObject(label);
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = localOffset;
            go.transform.localRotation = Quaternion.identity;
            go.layer = _root.layer;

            go.AddComponent<MeshFilter>().mesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material          = GetMaterial();
            mr.shadowCastingMode = ShadowCastingMode.Off;

            return go.transform;
        }

        // ── Ports ─────────────────────────────────────────────────────────────────
        private static Transform _muzzle, _port;

        public static Transform Muzzle    => _muzzle;
        public static Transform EjectPort => _port;

        private static Transform AddPort(string name, Vector3 meshOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = meshOffset;
            go.transform.localRotation = Quaternion.Inverse(Quaternion.Euler(Profiles.Current.ModelRot));
            return go.transform;
        }

        private static Material GetMaterial()
        {
            if (_mat != null) return _mat;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            _mat = new Material(shader) { hideFlags = HideFlags.DontUnloadUnusedAsset };

            var c = new Color(Config.ModelColorR, Config.ModelColorG, Config.ModelColorB, 1f);
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
            _mat.color = c;
            return _mat;
        }

        private static void ApplyRootTransform()
        {
            if (_root == null) return;
            var p = Profiles.Current;
            float aim = AkAds.Blend;

            Vector3 pose = aim > 0f ? Vector3.Lerp(p.ModelPos, AkAds.PoseFor(p), aim) : p.ModelPos;
            Quaternion kick = Quaternion.Euler(_recoilRot);
            Vector3 anchor = p.RecoilPivot;

            _root.transform.localPosition = anchor + kick * (pose - anchor) + _recoilPos;
            _root.transform.localRotation = kick * Quaternion.Euler(p.ModelRot);
            _root.transform.localScale    = Vector3.one * p.ModelScale;
        }

        // ── Recoil ────────────────────────────────────────────────────────────────

        private static Vector3 _recoilPos, _recoilVel;
        private static Vector3 _recoilRot, _recoilAngVel;

        private static void ResetRecoil()
        {
            _recoilPos = _recoilVel = _recoilRot = _recoilAngVel = Vector3.zero;
        }

        private static void AddRecoil(WeaponProfile p)
        {
            if (!Config.ModelRecoil) return;

            float omega = Mathf.Sqrt(Mathf.Max(1f, p.RecoilStiffness));
            float s     = Config.RecoilScale * omega;

            _recoilVel    += new Vector3(0f, 0f, -p.RecoilKick * s);
            _recoilAngVel += new Vector3(-p.RecoilRise * s,
                                         Random.Range(-p.RecoilRock, p.RecoilRock) * s,
                                         Random.Range(-p.RecoilRock, p.RecoilRock) * s);
        }

        private static void TickRecoil()
        {
            if (_root == null) return;
            if (_recoilPos == Vector3.zero && _recoilVel == Vector3.zero &&
                _recoilRot == Vector3.zero && _recoilAngVel == Vector3.zero) return;

            var p = Profiles.Current;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            Spring(ref _recoilPos, ref _recoilVel, p.RecoilStiffness, p.RecoilDamping, dt);
            Spring(ref _recoilRot, ref _recoilAngVel, p.RecoilStiffness, p.RecoilDamping, dt);

            if (_recoilPos.sqrMagnitude < 1e-10f && _recoilVel.sqrMagnitude < 1e-8f)
            { _recoilPos = Vector3.zero; _recoilVel = Vector3.zero; }
            if (_recoilRot.sqrMagnitude < 1e-6f && _recoilAngVel.sqrMagnitude < 1e-4f)
            { _recoilRot = Vector3.zero; _recoilAngVel = Vector3.zero; }

            ApplyRootTransform();
        }

        private static void Spring(ref Vector3 x, ref Vector3 v, float stiffness, float damping, float dt)
        {
            v += (-stiffness * x - damping * v) * dt;
            x += v * dt;
        }

        // ── Bolt cycle ────────────────────────────────────────────────────────────
        private const int StageIdle = 0, StageBack = 1, StageHold = 2, StageHome = 3;
        private const float StrokeEase = 1.6f;

        private static Vector3 _boltHome;
        private static int     _boltStage;
        private static float   _boltT;     

        /// <summary>A round went out: kick the gun. The action cycles separately.</summary>
        public static void OnShot() => AddRecoil(Profiles.Current);

        /// <summary>Run the bolt or pump through its stroke.</summary>
        public static void RackBolt()
        {
            _boltStage = StageBack;
            _boltT     = 0f;
        }

        private static void TickBolt()
        {
            if (_bolt == null || _boltStage == StageIdle) return;

            var p = Profiles.Current;
            _boltT += Time.deltaTime;

            float x;   // 0 = home, 1 = fully back

            switch (_boltStage)
            {
                case StageBack:
                {
                    float t = Mathf.Clamp01(_boltT / Mathf.Max(0.001f, p.BoltBackTime));
                    x = Mathf.Pow(t, StrokeEase);
                    if (t >= 1f) Advance(p.BoltDwell > 0f ? StageHold : StageHome);
                    break;
                }

                case StageHold:
                {
                    x = 1f;
                    if (_boltT >= p.BoltDwell) Advance(StageHome);
                    break;
                }

                default:
                {
                    float t = Mathf.Clamp01(_boltT / Mathf.Max(0.001f, p.BoltCycleTime));
                    x = 1f - Mathf.Pow(t, StrokeEase);
                    if (t >= 1f) { Advance(StageIdle); x = 0f; }
                    break;
                }
            }

            _bolt.localPosition = _boltHome + p.BoltTravel * x;
        }

        private static void Advance(int stage)
        {
            _boltStage = stage;
            _boltT     = 0f;
        }

        private static Transform FindSelectedItemPivot()
        {
            var gaToolbar = UnityEngine.Object.FindObjectOfType<GAToolbarReferences>(true);
            if (gaToolbar == null) return null;

            for (int i = 0; i < gaToolbar.transform.childCount; i++)
            {
                var c = gaToolbar.transform.GetChild(i);
                if (c.name != "Pivots") continue;
                for (int j = 0; j < c.childCount; j++)
                {
                    var p = c.GetChild(j);
                    if (p.name == "SelectedItemPivot") return p;
                }
            }
            return null;
        }
    }
}
