using GunsGunsGuns.Core;
using Il2CppPlayer.Appearances.God.Toolbar;
using MelonLoader;
using Unity.Cinemachine;
using UnityEngine;

namespace GunsGunsGuns.View
{
    // ── Sway and breath ──────────────────────────────────────────────────────────
    internal static class AkSway
    {
        private static GASelectedItemPivotRotationSway[] _sways;
        private static float[] _rot0, _pos0;
        private static float   _nextLookup;
        private static bool    _reported;

        private static CinemachineBasicMultiChannelPerlin[] _noise;
        private static float[] _amp0;

        private static float _breath;
        private static bool  _spent;     // ran dry; no more until it refills
        private static float _hold;      // eased 0..1 version of Holding

        public static bool  Holding { get; private set; }
        public static float BreathLeft => Config.BreathDuration <= 0f ? 1f
                                        : Mathf.Clamp01(_breath / Config.BreathDuration);

        public static void Reset()
        {
            _sways = null; _rot0 = null; _pos0 = null;
            _noise = null; _amp0 = null;
            _reported = false;
            _breath = Config.BreathDuration;
            _spent = false;
            _hold = 0f;
            Holding = false;
        }

        public static void Tick(bool aiming)
        {
            TickBreath(aiming);

            var p = Profiles.Current;
            _hold = Mathf.MoveTowards(_hold, Holding ? 1f : 0f,
                                      Time.deltaTime / Mathf.Max(0.02f, Config.BreathFade));

            float steady = Mathf.Lerp(1f, p.AdsSway, AkAds.Blend);
            steady = Mathf.Lerp(steady, Config.BreathSway, _hold * AkAds.Blend);
            var noise = FindNoise();
            if (noise != null)
                for (int i = 0; i < noise.Length; i++)
                    if (noise[i] != null) noise[i].AmplitudeGain = _amp0[i] * steady;

            var sways = Find();
            if (sways == null) return;

            for (int i = 0; i < sways.Length; i++)
            {
                if (sways[i] == null) continue;
                sways[i].rotationSwayMultiplier = _rot0[i] * steady;
                sways[i].positionSwayMultiplier = _pos0[i] * steady;
            }
        }

        private static CinemachineBasicMultiChannelPerlin[] FindNoise()
        {
            if (_noise != null) return _noise;
            if (Time.time < _nextNoiseLookup) return null;
            _nextNoiseLookup = Time.time + 2f;

            var found = UnityEngine.Object.FindObjectsOfType<CinemachineBasicMultiChannelPerlin>(true);
            if (found == null || found.Length == 0) return null;

            _noise = new CinemachineBasicMultiChannelPerlin[found.Length];
            _amp0  = new float[found.Length];
            for (int i = 0; i < found.Length; i++)
            {
                _noise[i] = found[i];
                _amp0[i]  = found[i].AmplitudeGain;
            }

            MelonLogger.Msg($"[AK] camera noise: {_noise.Length} source(s), first amplitude={_amp0[0]:F3}");
            return _noise;
        }

        private static float _nextNoiseLookup;

        private static void TickBreath(bool aiming)
        {
            float dur = Mathf.Max(0.1f, Config.BreathDuration);

            bool want = aiming
                     && Config.AllowBreath
                     && !_spent
                     && _breath > 0f
                     && !FruitLib.FruitMenu.BlocksGameplayInput
                     && Input.GetKey(Config.HoldBreathKey);

            Holding = want;

            if (want)
            {
                _breath -= Time.deltaTime;
                if (_breath <= 0f) { _breath = 0f; _spent = true; Holding = false; }
                return;
            }

            if (_breath < dur)
            {
                _breath = Mathf.Min(dur, _breath + Time.deltaTime * (dur / Mathf.Max(0.1f, Config.BreathRecovery)));
                if (_spent && _breath >= dur * 0.35f) _spent = false;
            }
        }

        private static GASelectedItemPivotRotationSway[] Find()
        {
            if (_sways != null) return _sways;
            if (Time.time < _nextLookup) return null;
            _nextLookup = Time.time + 2f;

            var found = UnityEngine.Object.FindObjectsOfType<GASelectedItemPivotRotationSway>(true);
            if (found == null || found.Length == 0) return null;

            _sways = new GASelectedItemPivotRotationSway[found.Length];
            _rot0  = new float[found.Length];
            _pos0  = new float[found.Length];

            for (int i = 0; i < found.Length; i++)
            {
                _sways[i] = found[i];
                _rot0[i]  = found[i].rotationSwayMultiplier;
                _pos0[i]  = found[i].positionSwayMultiplier;
            }

            if (!_reported)
            {
                _reported = true;
                var c = UnityEngine.Object.FindObjectsOfType<SelectedItemPivotSwayController>(true);
                MelonLogger.Msg($"[AK] sway: {_sways.Length} rotation component(s), " +
                                $"{(c != null ? c.Length : 0)} controller(s), " +
                                $"first rot={_rot0[0]:F3} pos={_pos0[0]:F3}");
            }

            return _sways;
        }
    }
}
