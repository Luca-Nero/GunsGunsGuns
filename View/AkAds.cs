using FruitLib;
using GunsGunsGuns.Core;
using MelonLoader;
using UnityEngine;

namespace GunsGunsGuns.View
{
    // ── Aiming down sights ───────────────────────────────────────────────────────
    internal static class AkAds
    {
        private static float _blend;     // 0 = hip, 1 = aimed
        private static float _restFov;
        private static bool  _toggled;
        public static float Blend => _blend * _blend * (3f - 2f * _blend);

        public static bool Aiming => _blend > 0.5f;

        public static float RestFov => _restFov;

        public static void Reset()
        {
            _blend   = 0f;
            _toggled = false;
            _restFov = 0f;
        }

        public static void Tick(bool weaponActive, Camera cam)
        {
            var p = Profiles.Current;

            bool want = weaponActive && Config.AllowAds && !FruitMenu.BlocksGameplayInput;
            if (want)
            {
                if (Config.AdsToggle)
                {
                    if (Input.GetMouseButtonDown(1)) _toggled = !_toggled;
                    want = _toggled;
                }
                else want = Input.GetMouseButton(1);
            }
            else _toggled = false;

            _blend = Mathf.MoveTowards(_blend, want ? 1f : 0f,
                                       Time.deltaTime / Mathf.Max(0.01f, p.AdsTime));

            if (cam == null) return;

            if (_blend <= 0f) { _restFov = cam.fieldOfView; return; }
            if (_restFov <= 0f) _restFov = cam.fieldOfView;

            cam.fieldOfView = Mathf.Lerp(_restFov, _restFov * p.AdsZoom, Blend);
            HandleTuning(p);
        }

        public static Vector3 PoseFor(WeaponProfile p)
        {
            Vector3 muzzle = Quaternion.Euler(p.ModelRot) * (p.MuzzleOffset * p.ModelScale) + p.ModelPos;
            return new Vector3(p.ModelPos.x - muzzle.x,
                               p.ModelPos.y - muzzle.y,
                               p.ModelPos.z) + p.AdsOffset;
        }

        // ── Sight-in rig ──────────────────────────────────────────────────────────

        private static readonly float[] Steps = { 0.05f, 0.01f, 0.005f, 0.001f };
        private static int  _precision = 1;
        private static bool _tuneScope;

        private static void HandleTuning(WeaponProfile p)
        {
            if (!Config.AdsTuning || _blend < 0.9f) return;
            if (FruitMenu.BlocksGameplayInput) return;

            if (Input.GetKeyDown(KeyCode.KeypadPeriod)) { Dump(p); return; }
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _tuneScope = !_tuneScope && p.ScopeRadius > 0f;
                MelonLogger.Msg($"[AK] tuning: {(_tuneScope ? "scope lens" : "sight picture")}");
                return;
            }

            if (Input.GetKeyDown(KeyCode.KeypadMultiply)) _precision = Mathf.Min(_precision + 1, Steps.Length - 1);
            if (Input.GetKeyDown(KeyCode.KeypadDivide))   _precision = Mathf.Max(_precision - 1, 0);

            float s = Steps[_precision];
            Vector3 move = Vector3.zero;
            float scale = 1f, fov = 0f;

            if (Input.GetKeyDown(KeyCode.Keypad4)) move.x -= s;
            if (Input.GetKeyDown(KeyCode.Keypad6)) move.x += s;
            if (Input.GetKeyDown(KeyCode.Keypad9)) move.y += s;
            if (Input.GetKeyDown(KeyCode.Keypad3)) move.y -= s;
            if (Input.GetKeyDown(KeyCode.Keypad8)) move.z += s;
            if (Input.GetKeyDown(KeyCode.Keypad2)) move.z -= s;

            if (Input.GetKeyDown(KeyCode.KeypadPlus))  scale = 0.95f;   // tighter / smaller
            if (Input.GetKeyDown(KeyCode.KeypadMinus)) scale = 1f / 0.95f;

            if (Input.GetKeyDown(KeyCode.Keypad7)) fov = -1f;
            if (Input.GetKeyDown(KeyCode.Keypad1)) fov = +1f;

            if (move == Vector3.zero && scale == 1f && fov == 0f) return;

            if (_tuneScope)
            {

                p.ScopeLens  += Quaternion.Inverse(Quaternion.Euler(p.ModelRot)) * move
                              / Mathf.Max(0.0001f, p.ModelScale);
                p.ScopeRadius = Mathf.Max(0.001f, p.ScopeRadius * (1f / scale));
                p.ScopeFov    = Mathf.Clamp(p.ScopeFov + fov, 1f, 90f);
                AkScope.Refresh(p);

                MelonLogger.Msg($"[AK] {p.Name} scope  lens=({p.ScopeLens.x:F4}, {p.ScopeLens.y:F4}, {p.ScopeLens.z:F4})  " +
                                $"radius={p.ScopeRadius:F4}  fov={p.ScopeFov:F1}  [step {s:F3}]");
                return;
            }

            p.AdsOffset += move;
            p.AdsZoom    = Mathf.Clamp(p.AdsZoom * scale, 0.05f, 1f);

            MelonLogger.Msg($"[AK] {p.Name} sight  offset=({p.AdsOffset.x:F4}, {p.AdsOffset.y:F4}, {p.AdsOffset.z:F4})  " +
                            $"zoom={p.AdsZoom:F3}  [step {s:F3}]");
        }

        private static void Dump(WeaponProfile p)
        {
            MelonLogger.Msg($"[AK] {p.Name} sight block");
            MelonLogger.Msg($"    AdsZoom   = {p.AdsZoom:F3}f,");
            MelonLogger.Msg($"    AdsOffset = new Vector3({p.AdsOffset.x:F4}f, {p.AdsOffset.y:F4}f, {p.AdsOffset.z:F4}f),");
            if (p.ScopeRadius > 0f)
            {
                MelonLogger.Msg($"    ScopeLens   = new Vector3({p.ScopeLens.x:F4}f, {p.ScopeLens.y:F4}f, {p.ScopeLens.z:F4}f),");
                MelonLogger.Msg($"    ScopeRadius = {p.ScopeRadius:F4}f,");
                MelonLogger.Msg($"    ScopeFov    = {p.ScopeFov:F1}f,");
            }
        }
    }
}
