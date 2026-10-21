// Console logging with a [Tag] prefix so you can filter the Console, e.g. type "[Revive]" in its search box.
// Tags in use: Level, Config, Move, Booster, Revive, Win, Tutorial, Ads.
using UnityEngine;

namespace KnitLink
{
    public static class KLog
    {
        /// <summary>Turn off the routine logs (Info) in a release build; warnings and errors always show.</summary>
        public static bool Verbose = true;
        public static void Info(string tag, string msg) { if (Verbose) Debug.Log($"[{tag}] {msg}"); }
        public static void Warn(string tag, string msg) => Debug.LogWarning($"[{tag}] {msg}");
        public static void Error(string tag, string msg) => Debug.LogError($"[{tag}] {msg}");
    }
}
