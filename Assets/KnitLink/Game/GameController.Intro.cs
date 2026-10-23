// GameController – Level start animation (same as the web demo, ~1.5 s). Timings in IntroT (seconds):
//   1) top bar slides down, "Level N" pops   2) picture + baskets come in   3) shelf, board and boosters rise
//   4) yarn drops onto the board row by row from the top (small bounce); stack numbers / ice pop after each ball lands.
// Board taps and boosters wait until the last ball has landed (introUntil). The coin HUD only animates on the first level shown.
// (GameController is split into GameController.*.cs files by feature; all parts share the same fields.)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        static class IntroT { public const float Title = .08f, Pic = .15f, Shelf = .26f, Board = .30f, Boosters = .38f, Balls = .52f, RowStep = .055f, ColStep = .025f, Drop = .46f; }
        bool firstIntro = true;
        readonly Dictionary<RectTransform, Vector2> introHome = new Dictionary<RectTransform, Vector2>();

        /// <summary>Starts the animation and returns how long it takes.</summary>
        float PlayLevelIntro()
        {
            float last = IntroT.Balls;
            Slide(btnSettings.transform as RectTransform, 0, .32f, new Vector2(0, 66)); Slide(btnRetry.transform as RectTransform, 0, .32f, new Vector2(0, 66));
            if (firstIntro) { Slide(coinHud, 0, .32f, new Vector2(0, 66)); firstIntro = false; }
            PopIn(title.rectTransform, IntroT.Title, .42f);
            PopIn(picFrame, IntroT.Pic, .36f);
            Slide(basketsHead, IntroT.Pic, .3f, new Vector2(0, 30));
            Slide(shelfHead, IntroT.Shelf, .32f, new Vector2(0, -72)); Slide(shelfRow, IntroT.Shelf + .02f, .32f, new Vector2(0, -72));
            for (int i = 0; i < slots.Count; i++) PopIn(slots[i].rectTransform, IntroT.Shelf + .1f + i * .03f, .26f);
            PopIn(boardPanel, IntroT.Board, .36f, .94f);
            for (int i = 0; i < BKeys.Length; i++) Slide((RectTransform)bButtons[BKeys[i]].transform, IntroT.Boosters + i * .07f, .36f, new Vector2(0, -72));
            var g = S.geo;
            for (int t = 0; t < cells.Count; t++)
            {
                var xy = g.xy[t]; float d = IntroT.Balls + xy[1] * IntroT.RowStep + xy[0] * IntroT.ColStep; last = Mathf.Max(last, d);
                var v = cells[t];
                foreach (var im in new[] { v.front, v.knot })
                    if (im != null && im.gameObject.activeSelf) RunPos(im.rectTransform, DropIn(im, d));
                foreach (var im in new[] { v.back, v.ice })
                    if (im != null && im.gameObject.activeSelf) PopIn(im.rectTransform, d + IntroT.Drop * .65f, .26f);
                if (v.shadow != null) Fade(v.shadow, d - .06f, .16f);
            }
            if (beltStrip != null) PopIn(beltStrip, IntroT.Balls - .08f, .3f, .98f);
            if (boxLid != null) PopIn(boxLid, last, .36f);
            float total = last + IntroT.Drop + .06f; introUntil = Time.unscaledTime + total;
            return total;
        }

        // ---- building blocks: each sets its start state at once (so nothing flashes), waits, then animates ----
        void Slide(RectTransform rt, float delay, float dur, Vector2 from)
        {
            if (rt == null) return; var cg = CG(rt);
            // the true home is recorded once: a new level can start while the previous slide is still running
            if (!introHome.TryGetValue(rt, out var home)) introHome[rt] = home = rt.anchoredPosition;
            StartCoroutine(Tw(delay, dur, p => { if (rt == null) return; float e = KF.EaseOut(p); rt.anchoredPosition = home + from * (1 - e); cg.alpha = Mathf.Clamp01(p * 2); },
                () => { if (rt != null) { rt.anchoredPosition = home; cg.alpha = 1; } }));
        }
        void PopIn(RectTransform rt, float delay, float dur, float fromScale = .6f)
        {
            if (rt == null) return; var cg = CG(rt);
            StartCoroutine(Tw(delay, dur, p => { if (rt == null) return; rt.localScale = Vector3.one * Mathf.Lerp(fromScale, 1, p < .6f ? KF.EaseOut(p / .6f) * 1.08f : Mathf.Lerp(1.08f, 1, (p - .6f) / .4f)); cg.alpha = Mathf.Clamp01(p * 2.2f); },
                () => { if (rt != null) { rt.localScale = Vector3.one; cg.alpha = 1; } }));
        }
        static CanvasGroup CG(RectTransform rt) { var cg = rt.GetComponent<CanvasGroup>(); if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>(); return cg; }
        void Fade(Graphic gr, float delay, float dur)
        {
            float a = gr.color.a;
            StartCoroutine(Tw(Mathf.Max(0, delay), dur, p => { if (gr != null) { var c = gr.color; c.a = a * p; gr.color = c; } }, () => { if (gr != null) { var c = gr.color; c.a = a; gr.color = c; } }));
        }
        IEnumerator Tw(float delay, float dur, System.Action<float> f, System.Action end)
        {
            int my = token; f(0);
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime) { if (my != token) break; f(t / dur); yield return null; }
            end();
        }
        /// <summary>A ball (or tangle) drops into its spot from above with a small bounce. Runs through RunPos so it always ends at home.</summary>
        IEnumerator DropIn(Image im, float delay)
        {
            var rt = im.rectTransform; var home = Home(rt); float h = cs * .9f;
            void Set(float y, float a) { rt.anchoredPosition = home + new Vector2(0, y); var c = im.color; c.a = a; im.color = c; }
            Set(h, 0);
            yield return new WaitForSecondsRealtime(delay);
            for (float t = 0; t < IntroT.Drop; t += Time.unscaledDeltaTime)
            {
                float p = t / IntroT.Drop;
                Set(KF.At(p, 0, h, .6f, 0, .8f, cs * .08f, 1, 0), Mathf.Clamp01(p / .6f)); yield return null;
            }
            Set(0, 1); posAnims.Remove(rt);
        }
    }
}
