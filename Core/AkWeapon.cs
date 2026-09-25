using System;
using System.Collections.Generic;
using Il2CppPlayer.Cam;
using Il2CppPlayer.Cam.Shake;
using Il2CppInfrastructure.Project.AssetsHandlers.SFX;
using Il2CppSpawnables.Weapons;
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

        /// <summary>The weapon in hand, or null. Each gun is its own inventory item now.</summary>
        private static WeaponProfile _held;

        public static void OnSelected(WeaponProfile p, int slot)
        {
            // Switching straight from one gun to another can deliver the new gun's select
            // before the old one's deselect. Selecting over a held gun is therefore a swap:
            // AkModel sees Current change and rebuilds the rig for the new parts.
            bool swap = _active;
            FruitLib.FruitTrace.Mark($"[GGG] OnSelected {p.Name} slot {slot} (swap={swap})");

            Profiles.Select(p);
            _held      = p;
            _active    = true;
            _fireTimer = 0f;   // first click fires immediately, and no cadence carries over

            if (!swap)
            {
                AkModel.RequestSpawn();
                FruitLib.FruitTrace.Mark("[GGG] OnSelected: spawn requested, suppressing crosshair");
                AkNativeCrosshair.Suppress(true);
                FruitLib.FruitTrace.Mark("[GGG] OnSelected: crosshair suppressed");
            }
            Dbg.Log($"[AK] equipped {p.Name} (slot {slot}){(swap ? " over another gun" : "")}");
        }

        public static void OnDeselected(WeaponProfile p, int slot)
        {
            // The late deselect of the gun just swapped out: the new one is already in hand.
            if (!ReferenceEquals(_held, p)) return;

            _held   = null;
            _active = false;
            AkModel.Despawn();
            FruitLib.FruitTrace.Mark("[GGG] OnDeselected: restoring crosshair");
            AkNativeCrosshair.Suppress(false);
            FruitLib.FruitTrace.Mark("[GGG] OnDeselected: done");
            Dbg.Log($"[AK] holstered {p.Name} (slot {slot})");
        }

        public static void OnSceneReload()
        {
            _active       = false;
            _held         = null;
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

        private static void Shoot()
        {
            var cam = Camera.main != null ? Camera.main : FruitLib.FruitScene.First<Camera>();
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

            // Each pellet is its own command. Scatter is decided here, before the command, so a
            // networked shot reproduces exactly: only the spec id, origin, direction and seed
            // travel. A null return means something (a multiplayer client) took the shot over.
            for (int i = 0; i < Mathf.Max(1, p.Pellets); i++)
            {
                var round = FruitLib.FruitBallistics.SpawnProjectile(p.SpecId, origin, Scatter(dir, spread));
                if (round != null) round.Tag = p;
            }
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
                _shakeService = FruitLib.FruitScene.First<PlayerCameraShakeService>();

            if (_shakeService == null) return;

            try { _shakeService.PlayCameraShake(p.ShakeAmount * Mathf.Lerp(1f, p.AdsShake, AkAds.Blend)); }
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

            // Fallback, in case the service isn't up yet.
            //
            // There used to be a second one above this: borrow m_shootSoundClip off any
            // Viper17 in the scene. The Steam demo build took the clip off the weapon - a
            // Viper17 now only names its sound (ShootSFX, a WeaponSFXType) and leaves the
            // SFX service to resolve it, so there is no clip on it left to borrow.
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
}
