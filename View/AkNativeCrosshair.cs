using System.Collections.Generic;
using GunsGunsGuns.Core;
using Il2CppViews.Toolbar;
using UnityEngine;
using UnityEngine.UI;

namespace GunsGunsGuns.View
{
    /// <summary>
    /// Hides the game's native centre dot while a custom weapon is out, so it doesn't sit
    /// under our own crosshair.
    ///
    /// Hidden via <c>Graphic.enabled</c> — deliberately not <c>SetActive</c> and not a
    /// CanvasGroup. Deactivating a HUD canvas child stops its Update and silently unhooks
    /// whatever it observes, which is what NREs the LVA death cascade. And the CanvasGroup
    /// channel on these elements already belongs to NoHUD; writing alpha from here would put
    /// two mods in a tug-of-war over one float. Graphic.enabled is an independent channel, so
    /// either mod can hide the dot without clobbering the other.
    /// </summary>
    internal static class AkNativeCrosshair
    {
        private static Transform _center;
        private static readonly List<Graphic> _graphics = new List<Graphic>();
        private static readonly List<bool>    _wasEnabled = new List<bool>();

        private static bool _suppressed;
        private static int  _pollCountdown;

        /// <summary>Called on weapon select/deselect.</summary>
        public static void Suppress(bool on)
        {
            if (_suppressed == on) return;
            _suppressed = on;
            if (!on) Restore();
        }

        /// <summary>
        /// Re-asserted every frame: the game owns this object and re-enables it on its own
        /// (it re-activates on pause, respawn and toolbar changes), so a one-shot hide leaks
        /// the dot back in.
        /// </summary>
        public static void Tick()
        {
            if (!_suppressed || !Config.HideNativeCrosshair) { Restore(); return; }
            if (!Resolve()) return;

            for (int i = 0; i < _graphics.Count; i++)
            {
                var g = _graphics[i];
                if (g != null && g.enabled) g.enabled = false;
            }
        }

        public static void Reset()
        {
            // Scene reload drops the toolbar selection (FruitToolbar.ResetForScene), so the
            // weapon is no longer out and the old canvas is gone with the scene.
            _center = null;
            _graphics.Clear();
            _wasEnabled.Clear();
            _suppressed    = false;
            _pollCountdown = 0;
        }

        private static void Restore()
        {
            for (int i = 0; i < _graphics.Count; i++)
            {
                var g = _graphics[i];
                if (g != null) g.enabled = _wasEnabled[i];
            }
        }

        private static bool Resolve()
        {
            if (_center != null) return true;

            if (--_pollCountdown > 0) return false;
            _pollCountdown = 30;

            // ToolbarView is unique to the HUD canvas — find it and walk up.
            var toolbar = Object.FindObjectOfType<ToolbarView>(true);
            var canvas  = toolbar != null ? toolbar.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return false;

            var center = canvas.transform.Find("Center");
            if (center == null)
            {
                Dbg.Log("[AK] native centre dot not found under the HUD canvas.");
                return false;
            }

            _graphics.Clear();
            _wasEnabled.Clear();

            // Center may be the dot itself or a wrapper around it; take whatever draws.
            var found = center.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < found.Length; i++)
            {
                var g = found[i];
                if (g == null) continue;
                _graphics.Add(g);
                _wasEnabled.Add(g.enabled);   // captured before we ever write, so Restore is faithful
            }

            if (_graphics.Count == 0)
            {
                Dbg.Log("[AK] 'Center' has nothing drawable to hide.");
                return false;
            }

            _center = center;
            Dbg.Log($"[AK] native centre dot resolved ({_graphics.Count} graphic(s)).");
            return true;
        }
    }
}
