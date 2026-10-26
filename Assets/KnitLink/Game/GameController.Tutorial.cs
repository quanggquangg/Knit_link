// GameController – Tutorials: level 1 hand, tips, booster unlock + first free use, new-mechanic popups, coach overlay, hint bubbles.
// (GameController is split into GameController.*.cs files by feature; all parts share the same fields.)
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        // ---------- tutorials ----------
        IEnumerator RunIntro(int my)
        {
            yield return null; if (my != token || L == null) yield break;
            // popups and hands wait for the level start animation (GameController.Intro.cs)
            if (introUntil > Time.unscaledTime) yield return new WaitForSecondsRealtime(introUntil - Time.unscaledTime);
            if (my != token) yield break;
            // hard / super hard levels: warning ribbon first (GameController.HardBanner.cs), tutorials after it
            yield return HardBanner(my);
            if (my != token) yield break;
            // a booster unlocks at its level NUMBER (GameConfig), whatever level file sits there
            foreach (var k in BKeys)
                if (L.num == Unlock[k] && !Claimed(k)) { yield return new WaitForSecondsRealtime(.35f); if (my == token) BoosterIntro(k); yield break; }
            if (string.IsNullOrEmpty(L.intro)) yield break;
            KLog.Info("Tutorial", $"Intro {L.intro} {L.introKey} on level {L.num}");
            switch (L.intro)
            {
                case "howto":
                    if (!Seen("howto")) { MarkSeen("howto"); yield return TutHowTo(my); }
                    break;
                case "tip":
                    if (Seen("tip." + L.introKey)) break;
                    MarkSeen("tip." + L.introKey);
                    if (L.introKey == "shelf") tipShelfPending = true;
                    else if (L.introKey == "gaps") { yield return new WaitForSecondsRealtime(.6f); if (my == token) GapTip(); }
                    break;
                case "booster":
                    if (!Claimed(L.introKey)) { yield return new WaitForSecondsRealtime(.35f); if (my == token) BoosterIntro(L.introKey); }
                    break;
                case "mech":
                case "roll":
                    string key = L.intro + "." + L.introKey;
                    if (Seen(key)) break; MarkSeen(key);
                    yield return new WaitForSecondsRealtime(.3f); if (my == token) MechIntro(L.intro, L.introKey);
                    break;
            }
        }

        // Level 1: two forced matches with the hand, then one pair that has no path
        IEnumerator TutHowTo(int my)
        {
            yield return new WaitForSecondsRealtime(.5f);
            string[] texts = { "Tap two yarn balls of the same color.", "The yarn line can bend up to 2 times. Match this pair!" };
            for (int i = 0; i < 2; i++)
            {
                if (my != token) yield break;
                var pr = PickPair(i == 1); if (pr == null) break;
                bool done = false; StartCoach(new Coach { trays = new List<int> { pr.Value.a, pr.Value.b }, text = texts[i], done = () => done = true });
                while (!done) { if (my != token) yield break; yield return null; }
                while (pending > 0) { if (my != token) yield break; yield return null; }
            }
            var np = PickNoPath();
            if (np != null && my == token)
            {
                bool done = false; StartCoach(new Coach { trays = new List<int> { np.Value.a, np.Value.b }, nopath = true, text = "Other yarn blocks every line between these two. Tap them and see.", done = () => done = true });
                while (!done) { if (my != token) yield break; yield return null; }
                yield return new WaitForSecondsRealtime(1.1f);
            }
            if (my == token) HintBubble(boardPanel, "Great! Fill the basket to knit the picture.", 2.6f);
        }
        (int a, int b)? PickPair(bool bends)
        {
            var w = S.WantSet(); var pr = S.Pairs(); if (pr.Count == 0) return null;
            var good = pr.Where(m => w.Contains(S.Top(m.a).c)).ToList(); if (good.Count > 0) pr = good;
            int Bends((int a, int b) m) { var p = S.FindPath(m.a, m.b); return p == null ? 9 : p.Count - 2; }
            return bends ? pr.OrderByDescending(Bends).First() : pr.OrderBy(Bends).First();
        }
        (int a, int b)? PickNoPath()
        {
            var by = new Dictionary<string, List<int>>();
            for (int t = 0; t < S.stacks.Length; t++) { var b = S.Top(t); if (b == null) continue; if (!by.TryGetValue(b.c, out var l)) by[b.c] = l = new List<int>(); l.Add(t); }
            foreach (var l in by.Values) for (int i = 0; i < l.Count; i++) for (int j = i + 1; j < l.Count; j++) if (!S.Connected(l[i], l[j])) return (l[i], l[j]);
            return null;
        }

        // booster unlock: popup + Claim (+2), then a forced and free first use
        // the "New booster" popup is in GameController.Popups.cs (BoosterIntro)
        void ClaimBooster(string k)
        {
            PlayerPrefs.SetInt("kl.claimed." + k, 1); SetInv(k, Inv(k) + Cfg.boostersOnUnlock); CloseModal(); RenderBoosters(); Sfx.Play(Sfx.Unlock);
            StartCoroutine(Bump((RectTransform)bButtons[k].transform, 1.16f));
            StartCoroutine(CoachBoosterWhenReady(k, token));
        }
        /// <summary>Short line after the free first use: what the booster just did (user 25/10: clearer than "used for free"; no count –
        /// during this first use the button shows "Free" instead of the number, see FreeBadge).</summary>
        string BoosterDoneText(string k)
        {
            switch (k)
            {
                case "undo": return "Your last move is back!";
                case "hint": return "Link the glowing pair!";
                default:     return "2 more shelf slots for this picture!";
            }
        }
        IEnumerator CoachBoosterWhenReady(string k, int my)
        {
            yield return new WaitForSecondsRealtime(.4f);
            if (k == "undo" && history.Count == 0)
            {   // Undo needs a move to take back: let the player match a pair first
                HintBubble(boardPanel, "Match a pair first, then try Undo.", 3f);
                while (history.Count == 0 || pending > 0 || autoRun) { if (my != token || S == null || S.Won || S.over) yield break; yield return null; }
                yield return new WaitForSecondsRealtime(.2f);
            }
            if (my != token || S == null || S.Won || S.over || modalRT.gameObject.activeSelf) yield break;
            {
                StartCoach(new Coach { booster = k, text = $"Tap {BName[k]} to use it. This first one is free!", done = () => StartCoroutine(Delay(.3f, () => HintBubble((RectTransform)bButtons[k].transform, BoosterDoneText(k), 3f))) });
            }
        }

        // new mechanic popups (animated) are in GameController.ItemIntro.cs (MechIntro)
        void GapTip()
        {
            var g = S.geo; int best = -1, bestN = -1;
            for (int y = 0; y < g.H; y++) for (int x = 0; x < g.W; x++)
                {
                    if (g.at[y * g.W + x] >= 0) continue; int n = 0;
                    if (x > 0 && g.at[y * g.W + x - 1] >= 0) n++; if (x < g.W - 1 && g.at[y * g.W + x + 1] >= 0) n++;
                    if (y > 0 && g.at[(y - 1) * g.W + x] >= 0) n++; if (y < g.H - 1 && g.at[(y + 1) * g.W + x] >= 0) n++;
                    if (n > bestN) { bestN = n; best = y * g.W + x; }
                }
            if (best < 0) return;
            var spot = UIKit.Rect("GapMark", board); spot.anchorMin = spot.anchorMax = new Vector2(0, 1); spot.sizeDelta = new Vector2(cs, cs * CELL_H);
            spot.anchoredPosition = GridPos(best % g.W, best / g.W);
            StartCoroutine(PulseRing(spot, 4f)); HintBubble(spot, "Yarn lines can pass through gaps in the board and around its edge.", 4.2f);
            StartCoroutine(Delay(4.3f, () => { if (spot != null) Destroy(spot.gameObject); }));
        }

        // ---------- coach overlay ----------
        void BuildCoach()
        {
            coachRT = UIKit.Rect("Coach", screenRT); UIKit.Stretch(coachRT);
            var cv = coachRT.gameObject.AddComponent<Canvas>(); cv.overrideSorting = true; cv.sortingOrder = 50; coachRT.gameObject.AddComponent<GraphicRaycaster>();
            var dim = UIKit.Img("Dim", coachRT, null, new Color(.035f, .05f, .13f, .78f), true); UIKit.Stretch(dim.rectTransform);
            var hb = UIKit.Rect("Top", coachRT); UIKit.Stretch(hb);
            var hc = hb.gameObject.AddComponent<Canvas>(); hc.overrideSorting = true; hc.sortingOrder = 70;
            var bub = UIKit.Img("Bubble", hb, UIKit.Rounded, UIKit.Hex("#fffaf0")); bubbleRT = bub.rectTransform;
            bubbleRT.anchorMin = bubbleRT.anchorMax = new Vector2(.5f, .5f); bubbleRT.sizeDelta = new Vector2(940, 150);
            var outl = bub.gameObject.AddComponent<Outline>(); outl.effectColor = UIKit.Honey; outl.effectDistance = new Vector2(4, -4);
            bubbleText = UIKit.Txt("Text", bubbleRT, "", 38, UIKit.Ink); UIKit.Stretch(bubbleText.rectTransform, 20); SkinBubble(bub, bubbleText);
            var hand = UIKit.Img("Hand", hb, UIKit.Hand, Color.white); handRT = hand.rectTransform;
            handRT.sizeDelta = new Vector2(170, 170); handRT.pivot = new Vector2(.44f, .93f);   // pivot = fingertip
            SkinHand(hand);
            coachRT.gameObject.SetActive(false);
        }
        void StartCoach(Coach c)
        {
            EndCoach(); coach = c; foreach (Transform t in fx) if (t.name == "HintBubble") Destroy(t.gameObject); coachRT.gameObject.SetActive(true); coachRT.SetAsLastSibling(); modalRT.SetAsLastSibling(); RenderBoosters();   // the coached booster shows "Free"
            var targets = c.booster != null ? new List<GameObject> { bButtons[c.booster].gameObject } : c.trays.Select(t => cells[t].rt.gameObject).ToList();
            foreach (var go in targets)
            {   // lift the targets above the dim layer; they stay clickable
                var cvs = go.AddComponent<Canvas>(); cvs.overrideSorting = true; cvs.sortingOrder = 60; go.AddComponent<GraphicRaycaster>(); lifted.Add(go);
            }
            bubbleText.text = c.text; SkinBubbleFit(bubbleRT, bubbleText); CoachHand();
            // bubble above the targets when they sit low on the screen, below them otherwise
            float midY = targets.Average(go => go.transform.position.y), rootMid = root.position.y;
            float tY = targets.Max(go => go.transform.position.y), bY = targets.Min(go => go.transform.position.y);
            bubbleRT.position = new Vector3(root.position.x, midY < rootMid ? tY + 200 * canvas.scaleFactor : bY - 220 * canvas.scaleFactor, 0);
        }
        void CoachHand()
        {
            if (coach == null) return;
            RectTransform target = coach.booster != null ? (RectTransform)bButtons[coach.booster].transform
                : cells[sel >= 0 && coach.trays.Contains(sel) ? coach.trays.First(t => t != sel) : coach.trays[0]].rt;
            handTarget = target.position + new Vector3(0, -10, 0) * canvas.scaleFactor;
        }
        void ResolveCoach() { var c = coach; EndCoach(); c?.done?.Invoke(); }
        void EndCoach()
        {
            coach = null;
            foreach (var go in lifted) if (go != null) { DestroyImmediate(go.GetComponent<GraphicRaycaster>()); DestroyImmediate(go.GetComponent<Canvas>()); }
            lifted.Clear();
            if (coachRT != null) coachRT.gameObject.SetActive(false);
            if (S != null && bBadges.Count > 0) RenderBoosters();   // "Free" goes, the normal count comes back
        }

        // soft hint: a short bubble near the target, nothing is blocked
        void HintBubble(RectTransform target, string text, float secs)
        {
            if (target == null) return;
            var bub = UIKit.Img("HintBubble", fx, UIKit.Rounded, UIKit.Hex("#fffaf0")); var rt = bub.rectTransform;
            rt.sizeDelta = new Vector2(940, 140);
            var outl = bub.gameObject.AddComponent<Outline>(); outl.effectColor = UIKit.Honey; outl.effectDistance = new Vector2(4, -4);
            var t = UIKit.Txt("Text", rt, text, 34, UIKit.Ink); UIKit.Stretch(t.rectTransform, 18); SkinBubble(bub, t); SkinBubbleFit(rt, t);
            float y = target.position.y; bool low = y < root.position.y;
            rt.position = new Vector3(root.position.x, y + (low ? 190 : -200) * canvas.scaleFactor, 0);
            StartCoroutine(FadeAway(rt, secs));
        }
        IEnumerator FadeAway(RectTransform rt, float secs)
        {
            var cg = rt.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false;
            for (float t = 0; t < .2f; t += Time.unscaledDeltaTime) { if (rt == null) yield break; cg.alpha = t / .2f; yield return null; }
            yield return new WaitForSecondsRealtime(secs);
            for (float t = 0; t < .3f; t += Time.unscaledDeltaTime) { if (rt == null) yield break; cg.alpha = 1 - t / .3f; yield return null; }
            if (rt != null) Destroy(rt.gameObject);
        }
        IEnumerator PulseRing(RectTransform target, float secs)
        {
            if (target == null) yield break;
            // new art: a strong orange ring + soft orange glow, readable on the cream board (the pale yellow one was hard to see – user 25/10)
            Color ringCol = SK ? UIKit.Hex("#ff7a2e") : new Color(1f, .89f, .6f);
            Image glow = null;
            if (SK) { glow = UIKit.Img("PulseGlow", target, UIKit.SoftGlow, new Color(1f, .55f, .2f, .5f)); glow.transform.SetAsFirstSibling(); glow.raycastTarget = false; }   // behind the ball, not over it
            var im = UIKit.Img("Pulse", fx, UIKit.Ring, ringCol); var rt = im.rectTransform;
            float size = Mathf.Max(target.rect.width, target.rect.height) * 1.15f; rt.sizeDelta = new Vector2(size, size);
            if (glow != null) glow.rectTransform.sizeDelta = new Vector2(size * 1.5f, size * 1.5f);
            for (float t = 0; t < secs; t += Time.unscaledDeltaTime)
            {
                if (rt == null || target == null) break;
                rt.position = target.position; float p = Mathf.Repeat(t / .9f, 1f), beat = Mathf.Sin(p * Mathf.PI);
                rt.localScale = Vector3.one * (1 + .12f * Mathf.Sin(p * Mathf.PI * 2)); im.color = new Color(ringCol.r, ringCol.g, ringCol.b, .65f + .35f * beat);
                if (glow != null) glow.color = new Color(1f, .55f, .2f, .35f + .35f * beat);
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
            if (glow != null) Destroy(glow.gameObject);
        }
    }
}
