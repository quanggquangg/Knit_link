// Haptics – short vibrations (27/10). All of them follow the Vibration switch in Settings.
//   Light  : a pair is matched, a button is pressed, a full basket finishes knitting and goes away, the win confetti
//   Medium : errors / blocked moves / losing (Buzz(); before 27/10 it was Handheld.Vibrate, long and strong)
//   Heavy  : not used yet
//   Android: Vibrator with a one-shot effect (amplitude on Android 8+, plain duration on older phones).
//   iOS    : the Taptic Engine (UIImpactFeedbackGenerator) – native code in Plugins/iOS/KnitLinkHaptics.mm.
//   Editor : nothing vibrates; with KLog.Verbose on, each haptic is written to the Console as [Haptic].
// To make a haptic stronger / longer: change its line in Pulse() (ms = length, amp = strength 1..255).
using UnityEngine;
using UnityEngine.UI;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace KnitLink
{
    public static class Haptics
    {
        public enum Kind { Light, Medium, Heavy }
        const float MinGap = .04f;   // two haptics closer than this feel like one buzz: skip the second
        static float last = -1;

        /// <summary>Settings → Vibration (same key as GameController.SettingOn("vibe")).</summary>
        public static bool Enabled => PlayerPrefs.GetInt("kl.set.vibe", 1) == 1;

        public static void Light() => Play(Kind.Light);
        public static void Medium() => Play(Kind.Medium);
        public static void Heavy() => Play(Kind.Heavy);

        public static void Play(Kind k)
        {
            if (!Enabled) return;
            if (Time.unscaledTime - last < MinGap && k == Kind.Light) return;   // errors always come through
            last = Time.unscaledTime;
            KLog.Info("Haptic", k.ToString());
            Pulse(k);
        }

        static void Pulse(Kind k)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            switch (k)
            {
                case Kind.Light: Android(18, 70); break;
                case Kind.Medium: Android(28, 140); break;
                default: Android(45, 255); break;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            KLHapticImpact((int)k);   // 0 light, 1 medium, 2 heavy
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void KLHapticImpact(int style);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator; static int sdk; static bool ready, broken;
        static void Android(long ms, int amp)
        {
            if (broken) return;
            try
            {
                if (!ready)
                {
                    ready = true;
                    using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var act = up.GetStatic<AndroidJavaObject>("currentActivity")) vibrator = act.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using (var ver = new AndroidJavaClass("android.os.Build$VERSION")) sdk = ver.GetStatic<int>("SDK_INT");
                    if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) { broken = true; return; }
                }
                if (sdk >= 26)
                    using (var ve = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var eff = ve.CallStatic<AndroidJavaObject>("createOneShot", ms, amp)) vibrator.Call("vibrate", eff);
                else vibrator.Call("vibrate", ms);
            }
            catch (System.Exception e) { broken = true; KLog.Warn("Haptic", "Android vibration failed, falling back to Handheld.Vibrate: " + e.Message); Handheld.Vibrate(); }
        }
#endif
    }

    public static class HapticButtonExt
    {
        /// <summary>Make a UI button give a light haptic when pressed: AddComponent&lt;Button&gt;().WithHaptic().</summary>
        public static Button WithHaptic(this Button b) { b.onClick.AddListener(Haptics.Light); return b; }
    }
}
