using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GunsGunsGuns.View
{
    // ── Viewmodel camera ─────────────────────────────────────────────────────────

    internal static class AkViewmodel
    {
        private const float ViewNear = 0.01f;
        private const float ViewFar  = 30f;
        private const float FallbackNear = 0.03f;

        private static Camera _cam, _baseCam;
        private static int    _layer = -1;
        private static bool   _attempted;

        public static int Layer => _layer;

        public static void Reset()
        {
            _cam = null; _baseCam = null;
            _layer = -1;
            _attempted = false;
        }

        public static void Ensure(Camera baseCam)
        {
            if (baseCam == null) return;
            if (_cam != null && _baseCam == baseCam) return;
            if (_attempted && _baseCam == baseCam) return;

            _attempted = true;
            _baseCam   = baseCam;

            try
            {
                int layer = FreeLayer();
                if (layer < 0) { Fallback(baseCam, "no free layer"); return; }

                var baseData = baseCam.GetComponent<UniversalAdditionalCameraData>();
                if (baseData == null) { Fallback(baseCam, "world camera has no URP data"); return; }

                var go = new GameObject("GGG_ViewmodelCam");
                go.transform.SetParent(baseCam.transform, false);

                var cam = go.AddComponent<Camera>();
                cam.clearFlags    = CameraClearFlags.Depth;
                cam.cullingMask   = 1 << layer;
                cam.nearClipPlane = ViewNear;
                cam.farClipPlane  = ViewFar;
                cam.fieldOfView   = baseCam.fieldOfView;

                var data = go.AddComponent<UniversalAdditionalCameraData>();
                data.renderType = CameraRenderType.Overlay;

                baseCam.cullingMask &= ~(1 << layer);
                baseData.cameraStack.Add(cam);

                _cam   = cam;
                _layer = layer;
                MelonLogger.Msg($"[AK] viewmodel camera on layer {layer} ('{LayerMask.LayerToName(layer)}').");
            }
            catch (Exception e)
            {
                Fallback(baseCam, e.Message);
            }
        }

        public static void Sync()
        {
            if (_cam == null || _baseCam == null) return;
            _cam.fieldOfView = _baseCam.fieldOfView;
        }

        private static void Fallback(Camera baseCam, string why)
        {
            _layer = -1;
            if (baseCam.nearClipPlane > FallbackNear) baseCam.nearClipPlane = FallbackNear;

            MelonLogger.Warning($"[AK] no viewmodel camera ({why}); pulled the world near plane to {FallbackNear} instead.");
        }

        private static int FreeLayer()
        {
            for (int i = 8; i < 32; i++)
                if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) return i;
            return -1;
        }
    }
}
