// Dev tool: add to the Game object in Play mode to let the solver play every level through the real UI
// (taps, animations, baskets, auto-finish) and log the result of each level.
// It also checks that no yarn ball is left off-centre in its spot after the animations settle.
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

        IEnumerator Start()
        {
            yield return null;
            var gc = GetComponent<GameController>();
            var close = typeof(GameController).GetMethod("CloseModal", BindingFlags.NonPublic | BindingFlags.Instance);
            var modalField = typeof(GameController).GetField("modalRT", BindingFlags.NonPublic | BindingFlags.Instance);
            int levels = LevelData.Load().levels.Length;
            var list = new List<int>();
            if (onlyLevels != null && onlyLevels.Length > 0) list.AddRange(onlyLevels); else for (int i = 0; i < levels; i++) list.Add(i);
            foreach (int li in list)
            {
                gc.devSkipTutorials = true; gc.GoLevel(li); yield return null; close.Invoke(gc, null);
                int steps = 0; float t0 = Time.realtimeSinceStartup; string result = "?"; float worst = 0;
                while (Time.realtimeSinceStartup - t0 < 90)
                {
                    var S = gc.State;
                    var modal = (RectTransform)modalField.GetValue(gc);
                    if (S.Won) { result = "won"; break; }
                    if (S.over) { result = "LOST (shelf)"; break; }
                    if (modal.gameObject.activeSelf) { result = "modal open"; break; }
                    if (S.CanAuto) { yield return new WaitForSecondsRealtime(.5f); continue; }
                    var sol = BoardState.Solve(S, 20000, out _);
                    if (sol == null || sol.Count == 0) { result = "no solution"; break; }
                    gc.TapSpot(sol[0].a); gc.TapSpot(sol[0].b); steps++;
                    // let animations settle every few moves and measure ball offsets
                    if (steps % 3 == 0) { yield return new WaitForSecondsRealtime(1.2f); worst = Mathf.Max(worst, Offset(gc)); }
                    else yield return new WaitForSecondsRealtime(moveGap);
                }
                yield return new WaitForSecondsRealtime(4f);
                var m2 = (RectTransform)modalField.GetValue(gc);
                string line = $"L{li + 1} {result} steps={steps} winPanel={m2.gameObject.activeSelf} maxBallOffset={worst:0.0}px";
                report += line + "\n"; Debug.Log("[AutoPlayer] " + line);
            }
            Debug.Log("[AutoPlayer] DONE\n" + report);
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
