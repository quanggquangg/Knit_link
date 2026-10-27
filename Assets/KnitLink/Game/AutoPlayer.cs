// Dev tool: add to the Game object in Play mode to let the solver play every level through the real UI
// (taps, animations, baskets, auto-finish) and log the result of each level.
// It also checks that no yarn ball is left off-centre in its spot after the animations settle, counts Console errors /
// exceptions per level, writes the report to Logs/autoplay_report.txt and puts the player's progress back at the end
// (level, coins, boosters, beaten levels) so testing does not change the save (27/10).
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public class AutoPlayer : MonoBehaviour
    {
        public float moveGap = .6f;
        public int[] onlyLevels;            // empty = all levels
        public string report = "";

        int errors; string firstError;
        void OnEnable() => Application.logMessageReceived += OnLog;
        void OnDisable() => Application.logMessageReceived -= OnLog;
        void OnLog(string msg, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            errors++; if (firstError == null) firstError = msg.Split('\n')[0];
        }

        IEnumerator Start()
        {
            yield return null;
            var save = SaveProgress();
            var gc = GetComponent<GameController>();
            var close = typeof(GameController).GetMethod("CloseModal", BindingFlags.NonPublic | BindingFlags.Instance);
            var modalField = typeof(GameController).GetField("modalRT", BindingFlags.NonPublic | BindingFlags.Instance);
            int levels = LevelData.Load().levels.Length;
            var list = new List<int>();
            if (onlyLevels != null && onlyLevels.Length > 0) list.AddRange(onlyLevels); else for (int i = 0; i < levels; i++) list.Add(i);
            foreach (int li in list)
            {
                gc.devSkipTutorials = true; gc.GoLevel(li); yield return null; close.Invoke(gc, null);
                int steps = 0; float t0 = Time.realtimeSinceStartup; string result = "?"; float worst = 0; errors = 0; firstError = null;
                while (Time.realtimeSinceStartup - t0 < 90)
                {
                    var S = gc.State;
                    var modal = (RectTransform)modalField.GetValue(gc);
                    if (S.Won) { result = "won"; break; }
                    if (S.over) { result = "LOST (shelf)"; break; }
                    if (modal.gameObject.activeSelf) { result = "modal open"; break; }
                    if (S.CanAuto) { yield return new WaitForSecondsRealtime(.5f); continue; }
                    // quick search first; big boards (Lv23, Lv26) need the large budget LevelCheck uses (27/10)
                    var sol = BoardState.Solve(S, 20000, out _);
                    if (sol == null || sol.Count == 0) sol = BoardState.Solve(S, 600000, out _);
                    if (sol == null || sol.Count == 0) { result = "no solution"; break; }
                    gc.TapSpot(sol[0].a); gc.TapSpot(sol[0].b); steps++;
                    // let animations settle every few moves and measure ball offsets
                    if (steps % 3 == 0) { yield return new WaitForSecondsRealtime(1.2f); worst = Mathf.Max(worst, Offset(gc)); }
                    else yield return new WaitForSecondsRealtime(moveGap);
                }
                yield return new WaitForSecondsRealtime(4f);
                var m2 = (RectTransform)modalField.GetValue(gc);
                string line = $"L{li + 1} {result} steps={steps} time={Time.realtimeSinceStartup - t0:0}s winPanel={m2.gameObject.activeSelf} maxBallOffset={worst:0.0}px errors={errors}" + (firstError != null ? $" first: {firstError}" : "");
                report += line + "\n"; Debug.Log("[AutoPlayer] " + line);
            }
            Debug.Log("[AutoPlayer] DONE\n" + report);
            try { System.IO.File.WriteAllText("Logs/autoplay_report.txt", report + "DONE\n"); } catch (System.Exception e) { Debug.LogWarning("[AutoPlayer] report not written: " + e.Message); }
            RestoreProgress(save); gc.devSkipTutorials = false;
        }

        // ---------- keep the player's save as it was ----------
        static readonly string[] Keys = { "kl.level", "kl.coins", "kl.inv.undo", "kl.inv.hint", "kl.inv.shelf", "kl.claimed.undo", "kl.claimed.hint", "kl.claimed.shelf" };
        static Dictionary<string, int?> SaveProgress()
        {
            var d = new Dictionary<string, int?>();
            foreach (var k in Keys) d[k] = PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k) : (int?)null;
            for (int n = 1; n <= 200; n++) { string k = "kl.beaten." + n; d[k] = PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k) : (int?)null; }
            return d;
        }
        static void RestoreProgress(Dictionary<string, int?> d)
        {
            foreach (var kv in d) { if (kv.Value.HasValue) PlayerPrefs.SetInt(kv.Key, kv.Value.Value); else PlayerPrefs.DeleteKey(kv.Key); }
            PlayerPrefs.Save(); Debug.Log("[AutoPlayer] progress restored");
        }

        /// <summary>Largest distance (px) between a visible ball and its home spot inside its cell.</summary>
        static float Offset(GameController gc)
        {
            var cellsF = typeof(GameController).GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance);
            var csF = typeof(GameController).GetField("cs", BindingFlags.NonPublic | BindingFlags.Instance);
            float cs = (float)csF.GetValue(gc), worst = 0;
            foreach (var v in (IList)cellsF.GetValue(gc))
            {
                var t = v.GetType();
                var front = (Image)t.GetField("front").GetValue(v); var back = (Image)t.GetField("back").GetValue(v);
                if (front.gameObject.activeSelf) worst = Mathf.Max(worst, front.rectTransform.anchoredPosition.magnitude);
                if (back.gameObject.activeSelf) worst = Mathf.Max(worst, (back.rectTransform.anchoredPosition - new Vector2(cs * .3f, cs * .3f)).magnitude);
            }
            return worst;
        }
    }
}
