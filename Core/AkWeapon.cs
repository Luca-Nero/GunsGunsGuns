using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppEffectors;
using Il2CppEffectors.ReceiveMethods.Index;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppLVA.Organs.EffectorsPerception.Collectors;
using Il2CppPlayer.Cam;
using Il2CppInfrastructure.Project.Installers.AssetsHandlers.SFX;
using Il2CppSpawnables.Weapons;
using Il2CppVoxelMeshGeneration;
using MelonLoader;
using UnityEngine;
using GunsGunsGuns.Projectiles;
using GunsGunsGuns.View;

namespace GunsGunsGuns.Core
{
    internal static class AkWeapon
    {
        private static bool  _active;
        private static float _fireTimer;

        public static void OnSelected(int idx)
        {
            _active    = true;
            _fireTimer = 0f;   // first click fires immediately
            AkModel.RequestSpawn();
            AkNativeCrosshair.Suppress(true);
            Dbg.Log($"[AK] equipped (slot {idx})");
        }

        public static void OnDeselected(int idx)
        {
            _active = false;
            AkModel.Despawn();
            AkNativeCrosshair.Suppress(false);
            Dbg.Log($"[AK] holstered (slot {idx})");
        }

        public static void OnSceneReload()
        {
            _active       = false;
            _fireTimer    = 0f;
            _cycleAt      = 0f;
            AkAds.Reset();
            AkViewmodel.Reset();
            _audio        = null;   // destroyed with the old scene if it ever loses its flags
            _shakeService = null;   // scene object, re-find in the new one
        }

        // ── Action cycle ──────────────────────────────────────────────────────────
        private static float         _cycleAt;        // 0 = nothing pending
        private static WeaponProfile _cycleProfile;

        private static void Cycle(WeaponProfile p)
        {
            _cycleAt = 0f;
            AkModel.RackBolt();
            AkShells.Eject(p);
        }

        private static void TickCycle()
        {
            if (_cycleAt <= 0f) return;
            if (Time.time < _cycleAt) return;
            Cycle(_cycleProfile);
        }

        public static void Tick()
        {
            AkAds.Tick(_active, Camera.main);

            TickCycle();

            if (!_active) return;

            if (FruitLib.FruitMenu.BlocksGameplayInput) { _fireTimer = 0f; return; }

            HandleWeaponSwitch();

            if (!Input.GetMouseButton(0)) { _fireTimer = 0f; return; }

            float interval = 60f / Mathf.Clamp(Profiles.Current.FireRateRPM, 10f, 3000f);

            _fireTimer -= Time.deltaTime;
            int guard = 0;
            while (_fireTimer <= 0f && guard++ < 10)
            {
                Shoot();
                _fireTimer += interval;
            }
        }

        // ── Weapon switching ──────────────────────────────────────────────────────
        public static FruitLib.FruitToolbarItem SlotItem;

        private static void HandleWeaponSwitch()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            var p = Profiles.Cycle(scroll > 0f ? 1 : -1);
            if (p == null) return;

            _fireTimer = 0f;   // don't carry the old weapon's cadence across the switch
            SlotItem?.SetDisplay(p.Name, FruitLib.FruitToolbar.MakeSolidIcon(p.IconColor));
            Dbg.Log($"[AK] switched to {p.Name}");
        }

        private static void Shoot()
        {
            var cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>();
            if (cam == null) return;

            var t = cam.transform;
            var p = Profiles.Current;

            PlayShot(p);
            AddRecoil(p);
            AkViewKick.Add(p);
            AkModel.OnShot();

            if (p.CycleDelay > 0f)
            {
                if (_cycleAt > 0f) Cycle(_cycleProfile);
                _cycleAt      = Time.time + p.CycleDelay;
                _cycleProfile = p;
            }
            else Cycle(p);

            Vector3 aim = t.position + t.forward * Config.AimConvergence;
            if (Physics.Raycast(t.position, t.forward, out RaycastHit look, Config.AimConvergence,
                                ~0, QueryTriggerInteraction.Ignore))
                aim = look.point;

            Vector3 origin = AkModel.Muzzle != null ? AkModel.Muzzle.position
                                                    : t.position + t.forward * 0.5f;

            Vector3 toAim = aim - origin;
            Vector3 dir   = toAim.sqrMagnitude > 1e-6f ? toAim.normalized : t.forward;

            float ahead = Vector3.Dot(origin - t.position, dir);
            if (ahead < Config.MuzzleClearance) origin += dir * (Config.MuzzleClearance - ahead);

            float spread = p.SpreadDegrees * Mathf.Lerp(1f, p.AdsSpread, AkAds.Blend);

            for (int i = 0; i < Mathf.Max(1, p.Pellets); i++)
                AkProjectiles.Spawn(origin, Scatter(dir, spread), p);
        }

  
        private static void DrawBreath(float cx, float cy)
        {
            if (!Config.BreathMeter || !Config.AllowBreath) return;
            if (AkAds.Blend < 0.5f) return;

            float left = AkSway.BreathLeft;
            if (left >= 0.999f) return;

            const float w = 90f, h = 3f;
            float x = cx - w * 0.5f, y = cy + 46f;

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

            GUI.color = AkSway.Holding ? new Color(0.55f, 0.85f, 1f, 0.9f)
                                       : new Color(1f, 1f, 1f, 0.55f);
            GUI.DrawTexture(new Rect(x, y, w * left, h), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private static void DrawReticle(float cx, float cy)
        {
            const float gap = 3f, len = 22f, th = 1f;

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.9f);

            GUI.DrawTexture(new Rect(cx - gap - len, cy - th * 0.5f, len, th), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + gap,       cy - th * 0.5f, len, th), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - th * 0.5f, cy - gap - len, th, len), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - th * 0.5f, cy + gap,       th, len), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - th, cy - th, th * 2f, th * 2f), Texture2D.whiteTexture);

            GUI.color = prev;
        }

        private static Vector3 Scatter(Vector3 dir, float degrees)
        {
            if (degrees <= 0f) return dir;

            float tilt = UnityEngine.Random.Range(0f, degrees);
            float spin = UnityEngine.Random.Range(0f, 360f);
            var   perp = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;

            return (Quaternion.AngleAxis(spin, dir) * Quaternion.AngleAxis(tilt, perp) * dir).normalized;
        }

        // ── Recoil ────────────────────────────────────────────────────────────────

        private static PlayerCameraShakeService _shakeService;

        private static void AddRecoil(WeaponProfile p)
        {
            if (p.ShakeAmount <= 0f) return;

            if (_shakeService == null)
                _shakeService = UnityEngine.Object.FindObjectOfType<PlayerCameraShakeService>(true);

            if (_shakeService == null) return;

            try { _shakeService.fug(p.ShakeAmount * Mathf.Lerp(1f, p.AdsShake, AkAds.Blend)); }
            catch (Exception e) { MelonLogger.Warning($"[AK] camera shake failed: {e.Message}"); }
        }

        // ── Sound: the pistol's Shoot9MM, pitched down ────────────────────────────

        private static AudioSource _audio;
        private static AudioClip   _clip;
        private static bool        _warnedNoClip;
        private static float       _nextClipSearch;
        private const  float       ClipSearchInterval = 2f;

        private static void PlayShot(WeaponProfile p)
        {
            var clip = Clip();
            if (clip == null) return;

            if (_audio == null)
            {
                var go = new GameObject("GGG_AkAudio");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;

                _audio              = go.AddComponent<AudioSource>();
                _audio.playOnAwake  = false;
                _audio.spatialBlend = 0f;   // the player's own weapon — 2D
            }

            float jitter = UnityEngine.Random.Range(-Config.PitchVariance, Config.PitchVariance);
            _audio.pitch = Mathf.Clamp(p.ShotPitch * (1f + jitter), 0.1f, 3f);
            _audio.PlayOneShot(clip, Mathf.Clamp01(Config.ShotVolume));
        }

        private static AudioClip Clip()
        {
            if (_clip != null) return _clip;

            if (Time.time < _nextClipSearch) return null;
            _nextClipSearch = Time.time + ClipSearchInterval;

            _clip = FruitLib.FruitSfx.Weapon(WeaponSFXType.Shoot9MM);

            // Fallbacks, in case the service isn't up yet.
            if (_clip == null)
                foreach (var g in Resources.FindObjectsOfTypeAll<Glock17>())
                {
                    if (g == null || g.m_shootSoundClip == null) continue;
                    _clip = g.m_shootSoundClip;
                    break;
                }

            if (_clip == null)
                foreach (var c in Resources.FindObjectsOfTypeAll<AudioClip>())
                {
                    if (c == null || c.name != "Shoot9MM") continue;
                    _clip = c;
                    break;
                }

            if (_clip != null) Dbg.Log("[AK] borrowed Shoot9MM for the fire sound.");
            else if (!_warnedNoClip)
            {
                _warnedNoClip = true;
                MelonLogger.Warning("[AK] AudioClip 'Shoot9MM' not loaded yet — will keep retrying.");
            }

            return _clip;
        }

        // ── Crosshair ─────────────────────────────────────────────────────────────

        public static void DrawCrosshair()
        {
            if (!_active || !Config.ShowCrosshair) return;
            if (FruitLib.FruitMenu.BlocksGameplayInput) return;   // not over the pause screen

            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;

            DrawBreath(cx, cy);   // gates itself on aiming; irons need it as much as glass

            if (AkScope.Active && AkAds.Blend > 0.5f) { DrawReticle(cx, cy); return; }

            float alpha = 0.85f * (1f - AkAds.Blend);
            if (alpha <= 0.01f) return;

            const float gap = 5f, len = 9f, th = 2f;

            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            GUI.DrawTexture(new Rect(cx - gap - len,  cy - th * 0.5f, len, th), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + gap,        cy - th * 0.5f, len, th), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - th * 0.5f,  cy - gap - len, th, len), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - th * 0.5f,  cy + gap,       th, len), Texture2D.whiteTexture);

            GUI.color = prev;
        }
    }

    // ── Wound application ────────────────────────────────────────────────────────

    internal static class AkHitscan
    {
        public static bool IsLimb(GameObject obj) =>
            obj.GetComponentInParent(Il2CppType.Of<LimbEffectorReceiver>()) != null;

        /// <summary>Wound + shove a single limb the round has reached.</summary>
        public static void ApplyToLimb(RaycastHit hit, Vector3 dir, WeaponProfile p, float energy)
        {
            try
            {
                if (hit.collider == null) return;

                float depthScale = p.DepthScale * energy;
                if (depthScale < Config.MinDepth) return;   // round is spent

                var rb = hit.collider.GetComponentInParent<Rigidbody>();
                if (rb == null) return;

                ApplyCone(hit.point, dir, hit.collider.gameObject, p, depthScale, 1f);
                ApplyImpulse(rb, hit.point, dir, p, energy);
            }
            catch (Exception e) { MelonLogger.Warning($"[AK] wound error: {e.Message}"); }
        }

        private static void ApplyImpulse(Rigidbody rb, Vector3 point, Vector3 dir, WeaponProfile p, float energy)
        {
            try
            {
                rb.AddForceAtPosition(dir.normalized * (p.ImpactImpulse * energy),
                                      point, ForceMode.Impulse);

                if (p.ImpactTorque > 0f)
                    rb.AddTorque(UnityEngine.Random.insideUnitSphere * (p.ImpactTorque * energy),
                                 ForceMode.Impulse);
            }
            catch (Exception e) { MelonLogger.Warning($"[AK] impulse failed: {e.Message}"); }
        }

        private static void ApplyCone(Vector3 entryPos, Vector3 dir, GameObject hitObject,
                                      WeaponProfile p, float depthScale, float radiusScale)
        {
            var rb = hitObject.GetComponentInParent<Rigidbody>();
            if (rb == null) return;

            var birComp = rb.GetComponent(Il2CppType.Of<bir>());
            if (birComp == null) return;

            var receiverBiw = birComp.TryCast<biw>();
            if (receiverBiw == null) return;

            var lerComp = hitObject.GetComponentInParent(Il2CppType.Of<LimbEffectorReceiver>());
            if (lerComp == null) return;
            var limb = lerComp.TryCast<LimbEffectorReceiver>();
            if (limb == null) return;

            var voxelMesh = limb.wtb;
            if (voxelMesh == null) return;

            float r0 = p.ConeRadius0 * radiusScale;
            float r1 = p.ConeRadius1 * radiusScale;
            int stride = Mathf.Max(1, Config.ConeStep);

            int n = 0;
            for (int s = 0; s < Config.ConeMaxSteps; s++)
            {
                if (ct.diz(voxelMesh, entryPos, dir, s * stride) == null) break;
                n++;
            }
            if (n == 0) return;

            int totalVoxels = 0;
            var sets = new List<Il2CppStructArray<Vector3Int>>();

            for (int s = 0; s < n; s++)
            {
                var chunk = ct.diz(voxelMesh, entryPos, dir, s * stride);
                if (chunk == null) break;

                float t      = n > 1 ? s / (float)(n - 1) : 0f;
                int   radius = Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(r0, r1, t)));

                var voxels = new dh(chunk.pla, radius).dki();
                if (voxels == null || voxels.Length == 0) continue;

                sets.Add(voxels);
                totalVoxels += voxels.Length;
            }

            if (totalVoxels == 0) return;

            float signal = -10000f * depthScale;
            var builder = new bjd(totalVoxels, false);
            foreach (var voxels in sets)
                foreach (var v in voxels)
                    builder.jbq(new IndexEffectorSignal(fp.ecz(v), signal, InfluenceProcessType.Sum));

            receiverBiw.cyn(new bjb<bit>(builder));
            builder.Dispose();
        }
    }
}
