// Small reusable animation pieces for the UI (all use unscaled time, so they keep running when the game is paused).
//   Breath   – slow pulse on the button we want the player to press (coins if affordable, else the ad; Claim ×3 on win)
//   LoopAnim – runs step(phase 0..1) every frame on a loop; used by the animated popups (item intros, yarn bag icon)
//   KF       – keyframe lookup: KF.At(p, t0,v0, t1,v1, ...) linearly interpolates; holds the first/last value outside
using UnityEngine;

namespace KnitLink
{
    public class Breath : MonoBehaviour
    {
        public float period = 1.6f, amount = .06f;
        void Update()
        {
            float s = Mathf.Sin(Mathf.Repeat(Time.unscaledTime, period) / period * Mathf.PI);
            transform.localScale = Vector3.one * (1 + amount * s * s);
        }
        void OnDisable() { transform.localScale = Vector3.one; }
    }

    public class LoopAnim : MonoBehaviour
    {
        public float period = 3f, delay;
        public System.Action<float> step;
        float t0;
        void OnEnable() { t0 = Time.unscaledTime; }
        void Update()
        {
            if (step == null) return;
            float t = Time.unscaledTime - t0 - delay;
            step(t < 0 ? 0 : Mathf.Repeat(t, period) / period);
        }
        public static LoopAnim Add(GameObject go, float period, System.Action<float> step, float delay = 0)
        {
            var a = go.AddComponent<LoopAnim>(); a.period = period; a.step = step; a.delay = delay; return a;
        }
    }

    public static class KF
    {
        /// <summary>Pairs of (time, value), times ascending in 0..1.</summary>
        public static float At(float p, params float[] tv)
        {
            if (p <= tv[0]) return tv[1];
            for (int i = 2; i < tv.Length; i += 2)
                if (p <= tv[i]) { float a = tv[i - 2], b = tv[i]; return Mathf.Lerp(tv[i - 1], tv[i + 1], b > a ? (p - a) / (b - a) : 1); }
            return tv[tv.Length - 1];
        }
        public static float EaseOut(float p) => 1 - (1 - p) * (1 - p) * (1 - p);
        /// <summary>Overshoot "pop": 0 -> 1.1 -> 1.</summary>
        public static float Pop(float p) => p < .65f ? Mathf.Lerp(0, 1.1f, EaseOut(p / .65f)) : Mathf.Lerp(1.1f, 1, (p - .65f) / .35f);
    }
}
