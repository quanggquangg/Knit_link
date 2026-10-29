// GameController – Yarn bag (revive "shelf full"), decided 20/10:
//   - a purple chip at the right end of the SHELF row, hidden until the first revive (its space is always reserved)
//   - inside: one ball per COLOR with a small count; the chip grows with the number of colors, anchored right
//   - "balls fly into the bag" animation: a big bag pops up in the middle of the board, the 4 oldest shelf balls
//     lift off one by one, arc into its mouth and drop inside (bag back -> ball -> bag front), the bag squashes on
//     each catch, then it shrinks and flies to the chip. With the brown sack (yarn-bag/, 29/10) the balls stay
//     visible in its mouth until it flies off, like the revive popup. Timings in BagT (seconds).
// (GameController is split into GameController.*.cs files by feature; all parts share the same fields.)
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        static class BagT { public const float Pop = .32f, First = .38f, Step = .23f, Lift = .13f, Fly = .43f, Drop = .17f, Hold = .28f, ToChip = .46f; }
        const float ChipH = 88, ChipIcon = 66, ChipBall = 61, ChipGap = 33;

        void BuildBagChip()
        {
            var chip = UIKit.Img("YarnBag", shelfHead, UIKit.Rounded, UIKit.Hex("#8a5aa8")); bagRT = chip.rectTransform; Pill(chip, ChipH);
            bagRT.anchorMin = bagRT.anchorMax = new Vector2(1, .5f); bagRT.pivot = new Vector2(1, .5f); bagRT.sizeDelta = new Vector2(200, ChipH); bagRT.anchoredPosition = Vector2.zero;
            var icon = UIKit.Img("Icon", bagRT, UIKit.BagChipIcon, Color.white); var irt = icon.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0, .5f); irt.sizeDelta = new Vector2(ChipIcon, ChipIcon); irt.anchoredPosition = new Vector2(12 + ChipIcon / 2, 0);
            bagGroupsRT = UIKit.Rect("Groups", bagRT); bagGroupsRT.anchorMin = new Vector2(0, 0); bagGroupsRT.anchorMax = new Vector2(1, 1); bagGroupsRT.offsetMin = new Vector2(12 + ChipIcon + 22, 0); bagGroupsRT.offsetMax = Vector2.zero;
            bagRT.gameObject.AddComponent<CanvasGroup>();
            bagRT.gameObject.SetActive(false);
        }

        /// <summary>Draw the chip: one ball per color + count. bagHold >= 0 draws only the first bagHold balls (others still flying).</summary>
        void RenderBag()
        {
            bool show = S.bagCap > 0; bagRT.gameObject.SetActive(show); if (!show) return;
            var list = bagHold >= 0 ? S.bag.Take(bagHold).ToList() : S.bag;
            var groups = new List<(string c, int n)>();
            foreach (var c in list) { int i = groups.FindIndex(g => g.c == c); if (i >= 0) groups[i] = (c, groups[i].n + 1); else groups.Add((c, 1)); }
            foreach (Transform t in bagGroupsRT) Destroy(t.gameObject);
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                if (SK && SkinBagGroup(g.c, g.n, i)) continue;   // new art: color dot + white number (GameController.Skin.cs)
                var ball = UIKit.YarnImg("Group_" + g.c, bagGroupsRT, g.c); ball.preserveAspect = true; var rt = ball.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, .5f); rt.sizeDelta = new Vector2(ChipBall, ChipBall); rt.anchoredPosition = new Vector2(ChipBall / 2 + i * (ChipBall + ChipGap), 0);
                var badge = UIKit.Img("N", rt, UIKit.Rounded, UIKit.Hex("#6c4aa3")); Pill(badge, 42); var brt = badge.rectTransform;
                brt.anchorMin = brt.anchorMax = new Vector2(1, 0); brt.sizeDelta = new Vector2(42, 42); brt.anchoredPosition = new Vector2(6, 2);
                var inner = UIKit.Img("Bg", brt, UIKit.Rounded, UIKit.Hex("#fffaf0")); UIKit.Stretch(inner.rectTransform, 4); Pill(inner, 34);
                var t = UIKit.Txt("T", brt, g.n.ToString(), 28, UIKit.Ink); UIKit.Stretch(t.rectTransform); if (SK) UIKit.Lift(t, 2);
            }
            // the chip grows with the number of colors (the only element allowed to flex), anchored right, max ~610
            float w = SK ? SkinBagChipWidth(groups.Count) : 12 + ChipIcon + 22 + groups.Count * (ChipBall + ChipGap) + 24;
            bagRT.sizeDelta = new Vector2(Mathf.Min(610, w), SK ? SkinLay.BagChipH : ChipH);
        }
        /// <summary>Where a ball of color c sits in the chip (or the chip's bag icon).</summary>
        Vector3 BagTarget(string c)
        {
            var g = bagGroupsRT.Find("Group_" + c);
            return g != null ? g.position : bagRT.GetChild(0).position;
        }

        /// <summary>The "balls fly into the bag" sequence. items: color, start position, the shelf slot it leaves.</summary>
        IEnumerator BagCollect(List<(string c, Vector3 from, Image slot)> items, bool firstTime)
        {
            int my = token; float k = canvas.scaleFactor;
            var stage = UIKit.Rect("BagStage", fx); UIKit.Stretch(stage);
            var dim = UIKit.Img("Dim", stage, null, new Color(.055f, .078f, .17f, 0), true); UIKit.Stretch(dim.rectTransform);
            const float BW = 343;
            var bag = UIKit.Rect("Bag", stage); bag.sizeDelta = new Vector2(BW, BW);
            bag.position = boardPanel.position;   // middle of the board, well below the shelf
            var back = UIKit.Img("Back", bag, UIKit.BagBack, Color.white); UIKit.Stretch(back.rectTransform);
            var balls = UIKit.Rect("Balls", bag); UIKit.Stretch(balls);
            var front = UIKit.Img("Front", bag, UIKit.BagFront, Color.white); UIKit.Stretch(front.rectTransform);
            if (firstTime) bagRT.GetComponent<CanvasGroup>().alpha = 0;
            var mouth = new Vector2(0, BW * (.5f - .33f));   // bag-local position of the mouth centre
            // artist's bag: open bag behind the balls, its front over them (stays open when full) – GameController.SkinPopup.cs
            if (SK && SkinBagParts(bag, back, front, BW, out var m)) mouth = m;
            float t0 = Time.unscaledTime;
            // 1) pop + dim
            Sfx.Play(Sfx.BagOpen);
            for (float t = 0; t < BagT.Pop; t += Time.unscaledDeltaTime)
            {
                if (my != token) break; float p = t / BagT.Pop;
                bag.localScale = Vector3.one * KF.Pop(p); bag.localEulerAngles = new Vector3(0, 0, KF.At(p, 0, 12, .65f, -4, 1, 0));
                dim.color = new Color(.055f, .078f, .17f, .45f * Mathf.Min(1, p * 1.6f)); yield return null;
            }
            bag.localScale = Vector3.one; bag.localEulerAngles = Vector3.zero; dim.color = new Color(.055f, .078f, .17f, .45f);
            // 2) balls, one every Step seconds
            var runs = new List<Coroutine>();
            for (int i = 0; i < items.Count; i++) runs.Add(StartCoroutine(BagBall(items[i], i, bag, balls, front, mouth, my)));
            foreach (var r in runs) yield return r;
            // the bag keeps its open mouth (bag1 + front) when full: switching to bag2 looked like the top of the bag was cut off (user 25/10)
            if (my == token) yield return new WaitForSecondsRealtime(BagT.Hold);
            // 3) shrink and fly to the chip
            if (my == token)
            {
                bagRT.gameObject.SetActive(true); Canvas.ForceUpdateCanvases();
                Vector3 from = bag.position, to = bagRT.GetChild(0).position; float endScale = ChipIcon / BW * 1.15f;
                for (float t = 0; t < BagT.ToChip; t += Time.unscaledDeltaTime)
                {
                    if (my != token) break; float p = t / BagT.ToChip, e = p < .5f ? 2 * p * p : 1 - Mathf.Pow(-2 * p + 2, 2) / 2;
                    var mid = (from + to) / 2 + Vector3.up * 110 * k;
                    bag.position = Vector3.Lerp(Vector3.Lerp(from, mid, e), Vector3.Lerp(mid, to, e), e); bag.localScale = Vector3.one * Mathf.Lerp(1, endScale, e);
                    dim.color = new Color(.055f, .078f, .17f, .45f * (1 - p)); yield return null;
                }
            }
            Destroy(stage.gameObject);
            bagRT.GetComponent<CanvasGroup>().alpha = 1;
            if (my != token) yield break;
            bagHold = -1; RenderBag(); StartCoroutine(Bump(bagRT, 1.16f)); Sfx.Play(Sfx.Boost);
            KLog.Info("Revive", $"Yarn bag animation done in {Time.unscaledTime - t0:0.00}s");
        }
        IEnumerator BagBall((string c, Vector3 from, Image slot) it, int i, RectTransform bag, RectTransform balls, Image front, Vector2 mouth, int my)
        {
            yield return new WaitForSecondsRealtime(BagT.First + i * BagT.Step);
            if (my != token) yield break;
            float k = canvas.scaleFactor, sz = Lay.SlotSize;
            if (it.slot != null) SetSlot(slots.IndexOf(it.slot), null);
            Sfx.Play(Sfx.BagLift);
            var im = UIKit.YarnImg("Ball", balls, it.c); im.preserveAspect = true; var rt = im.rectTransform; rt.sizeDelta = new Vector2(sz, sz);
            rt.SetParent(bag.parent, true); rt.SetAsLastSibling();   // above the bag while flying
            rt.position = it.from;
            Vector3 start = it.from, lift = start + Vector3.up * 33 * k;
            for (float t = 0; t < BagT.Lift; t += Time.unscaledDeltaTime) { if (my != token) yield break; float p = KF.EaseOut(t / BagT.Lift); rt.position = Vector3.Lerp(start, lift, p); rt.localScale = Vector3.one * Mathf.Lerp(1, 1.18f, p); yield return null; }
            Vector3 mouthW = bag.TransformPoint(mouth) + Vector3.up * 8 * k, mid = (lift + mouthW) / 2 + Vector3.up * 190 * k;
            for (float t = 0; t < BagT.Fly; t += Time.unscaledDeltaTime)
            {
                if (my != token) yield break; float p = t / BagT.Fly, e = p < .5f ? 2 * p * p : 1 - Mathf.Pow(-2 * p + 2, 2) / 2;
                rt.position = Vector3.Lerp(Vector3.Lerp(lift, mid, e), Vector3.Lerp(mid, mouthW, e), e);
                rt.localScale = Vector3.one * Mathf.Lerp(1.18f, .8f, e); rt.localEulerAngles = new Vector3(0, 0, -360 * e); yield return null;
            }
            // now behind the bag front
            rt.SetParent(balls, true); Vector3 a = rt.position;
            bool sack = SK && Skin.BagSprites(out _, out _, out bool isSack) && isSack;
            if (sack)
            {   // new sack art (29/10): the ball settles in the mouth and STAYS there (like the revive popup, RB.Pile) until the bag flies off
                if (i < 2) rt.SetAsFirstSibling();   // the first two sit behind the last two
                float pile = bag.rect.width * RB.PileOfBag; Vector3 b = bag.TransformPoint(mouth + RB.Pile[i % RB.Pile.Length] * pile);
                for (float t = 0; t < BagT.Drop; t += Time.unscaledDeltaTime) { if (my != token) yield break; float p = KF.EaseOut(t / BagT.Drop); rt.position = Vector3.Lerp(a, b, p); rt.localScale = Vector3.one * Mathf.Lerp(.8f, pile / sz, p); rt.localEulerAngles = Vector3.zero; yield return null; }
                rt.position = b; rt.localScale = Vector3.one * (pile / sz);
            }
            else
            {   // old bag: it drops inside and disappears
                Vector3 b = bag.TransformPoint(mouth + new Vector2(0, -70));
                for (float t = 0; t < BagT.Drop; t += Time.unscaledDeltaTime) { if (my != token) yield break; float p = t / BagT.Drop; rt.position = Vector3.Lerp(a, b, p * p); rt.localScale = Vector3.one * Mathf.Lerp(.8f, .62f, p); yield return null; }
                Destroy(im.gameObject);
            }
            StartCoroutine(Squash(bag)); Sfx.Play(Sfx.Clink);
            // soft glow, not a solid white oval (a solid one looked like a hole in the bag's mouth – fixed 25/10)
            var puff = UIKit.Img("Puff", bag, UIKit.SoftGlow, new Color(1, 1, 1, sack ? .4f : .9f));   // lighter on the sack: the balls in its mouth stay readable puff.rectTransform.sizeDelta = new Vector2(220, 96); puff.rectTransform.anchoredPosition = mouth;
            StartCoroutine(PuffFade(puff));
        }
        IEnumerator Squash(RectTransform rt)
        {
            for (float t = 0; t < .24f; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break; float p = t / .24f;
                rt.localScale = new Vector3(KF.At(p, 0, 1, .35f, 1.12f, .7f, .96f, 1, 1), KF.At(p, 0, 1, .35f, .88f, .7f, 1.05f, 1, 1), 1); yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }
        IEnumerator PuffFade(Image im)
        {
            for (float t = 0; t < .38f; t += Time.unscaledDeltaTime) { if (im == null) yield break; float p = t / .38f; im.rectTransform.localScale = Vector3.one * Mathf.Lerp(.4f, 1.6f, p); im.color = new Color(1, 1, 1, .9f * (1 - p)); yield return null; }
            if (im != null) Destroy(im.gameObject);
        }
    }
}
