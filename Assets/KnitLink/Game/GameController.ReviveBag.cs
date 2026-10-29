// GameController – "Out of Slot" revive popup with the artist's yarn-bag art (29/10, mockups yarn-bag/out slot.png + out slot2.png):
//   in the panel's well: a small copy of the shelf (the real shelf balls, empty holes, padlocks on locked slots) and the sack under it.
//   The 4 oldest shelf balls hop one by one into the sack's mouth, the balls left on the shelf slide to the front of the row
//   (like the real shelf), everything rests for a moment, then the balls fade back to their first places and it loops. Every number to tune is in RB (art pixels of the 818 px wide panel).
// (GameController is split into GameController.*.cs files by feature; all parts share the same fields.)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        static class RB
        {
            // positions are from the centre of the well (the BigIcon spot, 390 px under the panel top)
            public const float ShelfW = 660, ShelfY = 128;     // small shelf: width, centre height
            public const float BagW = 235, BagY = -82;         // sack: width, centre height
            public const float Mouth = .24f;                   // sack mouth: this far down from the top of bag1 (the balls sit there)
            public const float Loop = 3.8f;                    // seconds per loop
            public const float First = .45f, Step = .32f, Hop = .5f;   // first ball leaves at First, then one every Step; flight time Hop
            public const float Arc = 120;                      // how high the hop goes above the straight line
            public const float Rest = 3.2f, Fade = .25f;       // the balls in the sack fade out at Rest; shelf balls fade back in over Fade
            public const float BallScale = 1.2f;               // shelf balls × the real shelf's ball size scaled down (29/10 evening: were too small)
            public const float PileScale = .82f;               // ball size in the sack mouth (× shelf ball size)
            public const float PileOfBag = .3f;                // in-game bag (GameController.Bag.cs): ball size in the mouth × sack width
            // after the last ball lands, the balls left on the shelf ROLL to the front (29/10 evening: the straight slide looked stiff):
            // the front one starts first, each next one SlideStagger later; they spin as they roll, go a touch past their slot
            // (Overshoot) and settle back, with a tiny lift on the way and a little squash when they stop
            public const float SlideDelay = .12f, SlideTime = .5f, SlideStagger = .08f, Overshoot = 1.15f, SlideLift = 8, SettleSquash = .08f;
            // where the 4 balls settle in the mouth, × ball size from the mouth centre (2 back, 2 front, like out slot2.png)
            public static readonly Vector2[] Pile = { new Vector2(-.42f, .02f), new Vector2(.40f, .06f), new Vector2(-.05f, .22f), new Vector2(.12f, -.12f) };
        }

        /// <summary>Fills the well of the revive popup. Returns false without the new sack art (the old round bag loop is used then).</summary>
        bool ReviveBagLoop(RectTransform ic)
        {
            var plank = Skin.Get("Ingame/shelf"); if (plank == null || Skin.Tex("yarn-bag/bag1") == null) return false;
            int nSlots = Cfg.shelfSlots + Cfg.extraShelfSlots, take = Mathf.Min(Cfg.reviveBagSize, S.shelf.Count);
            float k = RB.ShelfW / Lay.Width, colW = (Lay.Width - 66) / nSlots, ballSize = SkinLay.SlotBall * Skin.BallBoost * k * RB.BallScale;

            // ---- small shelf: same layout as the real one (GameController.Skin.cs), scaled by k ----
            var shelf = UIKit.Img("MiniShelf", ic, plank, Color.white); var srt = shelf.rectTransform;
            srt.sizeDelta = new Vector2(RB.ShelfW, SkinLay.ShelfH * k); srt.anchoredPosition = new Vector2(0, RB.ShelfY);
            var slotPos = new Vector2[nSlots];
            for (int i = 0; i < nSlots; i++)
            {
                var hp = new Vector2((33 + colW * (i + .5f)) * k - RB.ShelfW / 2, SkinLay.HoleUp * k);
                bool locked = i >= S.cap;
                var hole = UIKit.SkinImg("Hole" + i, srt, locked ? "Ingame/shelf-hole2" : "Ingame/shelf-hole"); var hrt = hole.rectTransform;
                hrt.sizeDelta = locked ? new Vector2(94, 42) * k : new Vector2(SkinLay.HoleW, SkinLay.HoleH) * k; hrt.anchoredPosition = hp;
                slotPos[i] = srt.anchoredPosition + hp + new Vector2(0, SkinLay.SlotBall * SkinLay.SlotBallUp * k);   // ball centre, in well space
            }

            // ---- sack: back, balls, front (the balls drop between the two halves) ----
            var bag = UIKit.Rect("Sack", ic); var back = UIKit.Img("Back", bag, null, Color.white); UIKit.Stretch(back.rectTransform);
            var inBag = UIKit.Rect("Balls", bag); UIKit.Stretch(inBag);
            var front = UIKit.Img("Front", bag, null, Color.white); UIKit.Stretch(front.rectTransform);
            if (!SkinBagParts(bag, back, front, RB.BagW, out var mouth)) { Destroy(bag.gameObject); Destroy(shelf.gameObject); return false; }
            back.preserveAspect = front.preserveAspect = false;
            bag.anchoredPosition = new Vector2(0, RB.BagY);
            Vector2 mouthW = bag.anchoredPosition + mouth;   // mouth in well space

            // ---- shelf balls: the first `take` ones fly, the others stay ----
            var flyers = new List<(RectTransform rt, Image im, Vector2 from, Vector2 to)>();
            var stayers = new List<(RectTransform rt, Image im, Vector2 from, Vector2 to)>();   // shelf space: own slot -> slot (i - take)
            for (int i = 0; i < S.shelf.Count && i < nSlots; i++)
            {
                bool flies = i < take;
                var b = UIKit.YarnImg("Ball" + i, flies ? inBag : (Transform)srt, S.shelf[i]); b.preserveAspect = true; var rt = b.rectTransform;
                rt.sizeDelta = new Vector2(ballSize, ballSize);
                if (!flies) { stayers.Add((rt, b, slotPos[i] - srt.anchoredPosition, slotPos[i - take] - srt.anchoredPosition)); rt.anchoredPosition = stayers[stayers.Count - 1].from; continue; }
                // flying balls live inside the sack (behind its front) and are placed in well space through the sack's centre
                Vector2 to = mouthW + RB.Pile[i % RB.Pile.Length] * ballSize * RB.PileScale;
                flyers.Add((rt, b, slotPos[i] - bag.anchoredPosition, to - bag.anchoredPosition));
            }
            // the two back balls of the pile are drawn first (behind the two front ones)
            foreach (var f in flyers.Where((f, n) => n < 2)) f.rt.SetAsFirstSibling();

            LoopAnim.Add(bag.gameObject, RB.Loop, p =>
            {
                float t = p * RB.Loop, bump = 0;
                for (int n = 0; n < flyers.Count; n++)
                {
                    var f = flyers[n]; float leave = RB.First + n * RB.Step, land = leave + RB.Hop;
                    if (t < leave)
                    {   // on the shelf; fades in again at the start of the loop
                        f.rt.anchoredPosition = f.from; f.rt.localScale = Vector3.one * (t < RB.Fade ? KF.Pop(t / RB.Fade) : 1); f.rt.localEulerAngles = Vector3.zero;
                        Alpha(f.im, 1);
                    }
                    else if (t < land)
                    {   // the hop: up, over and down into the mouth, spinning once
                        float e = (t - leave) / RB.Hop; e = e < .5f ? 2 * e * e : 1 - Mathf.Pow(-2 * e + 2, 2) / 2;
                        Vector2 mid = (f.from + f.to) / 2 + new Vector2(0, RB.Arc);
                        f.rt.anchoredPosition = Vector2.Lerp(Vector2.Lerp(f.from, mid, e), Vector2.Lerp(mid, f.to, e), e);
                        f.rt.localScale = Vector3.one * Mathf.Lerp(1, RB.PileScale, e); f.rt.localEulerAngles = new Vector3(0, 0, -360 * e); Alpha(f.im, 1);
                    }
                    else
                    {   // in the sack's mouth until Rest, then fades out
                        f.rt.anchoredPosition = f.to; f.rt.localScale = Vector3.one * RB.PileScale; f.rt.localEulerAngles = Vector3.zero;
                        Alpha(f.im, t < RB.Rest ? 1 : Mathf.Clamp01(1 - (t - RB.Rest) / RB.Fade));
                        float d = t - land; if (d < .18f) bump = Mathf.Max(bump, Mathf.Sin(d / .18f * Mathf.PI));   // small squash when it lands
                    }
                }
                bag.localScale = new Vector3(1 + .06f * bump, 1 - .07f * bump, 1);
                // the balls left on the shelf slide to the front once the sack has all 4, and go back with the others at the loop end
                float slide = RB.First + (flyers.Count - 1) * RB.Step + RB.Hop + RB.SlideDelay;
                for (int j = 0; j < stayers.Count; j++)
                {
                    var st = stayers[j];
                    float u = flyers.Count == 0 ? 0 : Mathf.Clamp01((t - slide - j * RB.SlideStagger) / RB.SlideTime);
                    float e = BackOut(u, RB.Overshoot);                                         // 0 → a little past 1 → 1
                    var pos = Vector2.LerpUnclamped(st.from, st.to, e) + new Vector2(0, RB.SlideLift * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u * 1.4f)));
                    st.rt.anchoredPosition = pos;
                    // rolling: the turn matches the distance travelled (ball radius = half its size)
                    st.rt.localEulerAngles = new Vector3(0, 0, (st.from.x - pos.x) / (ballSize * .5f) * Mathf.Rad2Deg);
                    float pop = t < RB.Fade ? KF.Pop(t / RB.Fade) : 1, sq = RB.SettleSquash * Mathf.Sin(Mathf.PI * Mathf.Clamp01((u - .55f) / .45f));
                    st.rt.localScale = new Vector3(pop * (1 + sq), pop * (1 - sq), 1);
                    Alpha(st.im, t < RB.Rest ? 1 : Mathf.Clamp01(1 - (t - RB.Rest) / RB.Fade));
                }
            });
            return true;
        }
        /// <summary>Ease out with overshoot: goes past 1 by about (s - 1) × 10 % and settles back to 1 at p = 1.</summary>
        static float BackOut(float p, float s) { float q = p - 1; return 1 + (s + 1) * q * q * q + s * q * q; }
    }
}
