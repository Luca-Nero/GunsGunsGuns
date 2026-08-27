using GunsGunsGuns.Core;
using Il2CppPlayer.Cam;
using MelonLoader;
using Unity.Cinemachine;
using UnityEngine;

namespace GunsGunsGuns.View
{
    // ── View kick ────────────────────────────────────────────────────────────────
    internal static class AkViewKick
    {
        private static PlayerCameraService _svc;
        private static float _nextLookup;
        private static bool  _warned;

        private static float _pitch, _pitchVel;   // degrees, positive = view rises
        private static float _yaw,   _yawVel;
        private static float _sentPitch, _sentYaw;

        private static float _lastTilt, _lastPan;
        private static bool  _tracking;

        public static void Reset()
        {
            _svc = null;
            _pitch = _pitchVel = _yaw = _yawVel = 0f;
            _sentPitch = _sentYaw = 0f;
            _tracking = false;
        }

        public static void Add(WeaponProfile p)
        {
            if (!Config.ViewRecoil) return;

            float omega = Mathf.Sqrt(Mathf.Max(1f, p.ViewStiffness)) * Config.RecoilScale;

            _pitchVel += p.ViewKick * omega;
            _yawVel   += Random.Range(-p.ViewYaw, p.ViewYaw) * omega;
        }

        public static void Tick()
        {
            var p = Profiles.Current;

            if (_pitch != 0f || _pitchVel != 0f || _yaw != 0f || _yawVel != 0f)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);

                Spring(ref _pitch, ref _pitchVel, p.ViewStiffness, p.ViewDamping, dt);
                Spring(ref _yaw,   ref _yawVel,   p.ViewStiffness, p.ViewDamping, dt);

                if (Mathf.Abs(_pitch) < 0.0005f && Mathf.Abs(_pitchVel) < 0.005f) { _pitch = 0f; _pitchVel = 0f; }
                if (Mathf.Abs(_yaw)   < 0.0005f && Mathf.Abs(_yawVel)   < 0.005f) { _yaw   = 0f; _yawVel   = 0f; }
            }

            Apply(p, SensScale(p));
        }

        // ── Look speed ────────────────────────────────────────────────────────────

        private static float SensScale(WeaponProfile p)
        {
            if (!Config.AdsSensScaling || AkAds.Blend <= 0f) return 1f;

            float rest = AkAds.RestFov;
            if (rest <= 1f) return 1f;

            // Through glass the magnification is the scope's, not the camera's.
            float fov = AkScope.Active && p.ScopeRadius > 0f ? p.ScopeFov : rest * p.AdsZoom;

            float matched = Mathf.Clamp(fov / rest * p.AdsSens, 0.02f, 1f);
            return Mathf.Lerp(1f, matched, AkAds.Blend);
        }

        private static void Apply(WeaponProfile p, float sens)
        {
            float dPitch = _pitch - _sentPitch;
            float dYaw   = _yaw   - _sentYaw;

            bool idle = Mathf.Abs(dPitch) < 1e-5f && Mathf.Abs(dYaw) < 1e-5f;
            if (idle && sens >= 0.999f) { _tracking = false; return; }

            var pt = PanTilt();
            if (pt == null)
            {
                _sentPitch = _pitch; _sentYaw = _yaw;
                _tracking = false;
                return;
            }

            _sentPitch = _pitch;
            _sentYaw   = _yaw;

            var tilt = pt.TiltAxis;
            var pan  = pt.PanAxis;

            float t = tilt.Value, n = pan.Value;

            if (_tracking && sens < 0.999f)
            {
                float dt = t - _lastTilt;
                float dn = n - _lastPan;

                if (Mathf.Abs(dn) > 90f) dn = 0f;

                float take = 1f - sens;
                t -= dt * take;
                n -= dn * take;
            }

            t -= dPitch;   // Cinemachine tilts positive downward
            n += dYaw;

            if (tilt.Range.y > tilt.Range.x) t = Mathf.Clamp(t, tilt.Range.x, tilt.Range.y);

            tilt.Value = t; pt.TiltAxis = tilt;
            pan.Value  = n; pt.PanAxis  = pan;

            _lastTilt = t; _lastPan = n;
            _tracking = true;
        }

        private static void Spring(ref float x, ref float v, float stiffness, float damping, float dt)
        {
            v += (-stiffness * x - damping * v) * dt;
            x += v * dt;
        }
        private static CinemachinePanTilt PanTilt()
        {
            if (_svc == null)
            {
                if (Time.time < _nextLookup) return null;
                _nextLookup = Time.time + 2f;

                _svc = UnityEngine.Object.FindObjectOfType<PlayerCameraService>(true);
                if (_svc == null) return null;
            }

            var pt = _svc.m_panTilt;
            if (pt == null && !_warned)
            {
                _warned = true;
                MelonLogger.Warning("[AK] camera service has no pan/tilt; view recoil is off.");
            }
            return pt;
        }
    }
}
