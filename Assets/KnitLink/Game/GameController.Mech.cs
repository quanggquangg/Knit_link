// GameController – Needle box lid, conveyor belt strip and the "no path" feedback (added 23/10, same as the web demo).
//   Rules live in YarnCore (BoxCount, BeltShift, Boxed). This file only draws and animates them.
//   - boxLid: a red lid over the 2x2 box spots with a stitched inner panel and a dark label (ball of the box color + count).
//     The count bumps on every change; at 0 the lid lifts, tilts and fades out. Tapping it: buzz + small shake, no text.
//   - beltStrip: a dark strip behind the belt row with arrows sliding in the belt's direction. After each match the row slides
//     1 spot (AnimateBelt): balls move from their old spot, the one that wrapped fades in from the edge.
//   - No path between 2 same-color balls (decided 21/10): no text – buzz, both balls shake, a red ring blinks twice.
// (GameController is split into GameController.*.cs files by feature; all parts share the same fields.)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        static class MechT { public const float BeltLoop = 1.4f, Wrap = .3f, LidOpen = .62f, Flash = .6f; }
        static readonly Color LidRed = UIKit.Hex("#d9566b"), LidDark = UIKit.Hex("#a5384d"), LidPanel = UIKit.Hex("#e46f84"), LidLabel = UIKit.Hex("#5a2a2f"),
                              BeltColor = UIKit.Hex("#5b6275"), NoPathRed = UIKit.Hex("#e0344f"),
                              IceBlock = UIKit.Hex("#cdeafb");   // opaque frozen-yarn block (23/10)
        RectTransform boxLid, beltStrip; Text boxCountText; int shownBox = -1;

        /// <summary>Called by BuildBoard after the spots exist (sizes depend on cs).</summary>
        void BuildMech()
        {
            if (boxLid != null) Destroy(boxLid.gameObject);
            if (beltStrip != null) Destroy(beltStrip.gameObject);
            boxLid = null; beltStrip = null; shownBox = -1;
            if (S.belt != null) BuildBelt();
            if (S.box != null && S.box.n > 0) BuildBoxLid();
        }
        float StepX => cs * (1 + GAP);
        float StepY => cs * CELL_H + cs * GAP;

        void BuildBelt()
        {
            var strip = UIKit.Img("ConveyorBelt", board, UIKit.Rounded, BeltColor); strip.raycastTarget = false;
            beltStrip = strip.rectTransform; beltStrip.anchorMin = beltStrip.anchorMax = new Vector2(0, 1); beltStrip.pivot = new Vector2(0, 1);
            int row = S.geo.xy[S.belt.ts[0]][1];
            beltStrip.sizeDelta = new Vector2(board.sizeDelta.x + cs * .28f, cs * CELL_H + cs * .14f);
            beltStrip.anchoredPosition = new Vector2(-cs * .14f, -(row * StepY - cs * .07f));
            beltStrip.SetAsFirstSibling();   // behind the spots
            strip.gameObject.AddComponent<RectMask2D>();
            // arrows that slide in the belt direction
            var arrows = UIKit.Rect("Arrows", beltStrip); arrows.anchorMin = new Vector2(0, .5f); arrows.anchorMax = new Vector2(0, .5f); arrows.pivot = new Vector2(0, .5f);
            float gap = cs * .5f; int n = Mathf.CeilToInt(beltStrip.sizeDelta.x / gap) + 2;
            for (int i = 0; i < n; i++)
            {
                var a = UIKit.Img("Arrow", arrows, UIKit.IconArrowDown, new Color(1, 1, 1, .35f)); a.raycastTarget = false;
                a.rectTransform.sizeDelta = new Vector2(cs * .32f, cs * .32f); a.rectTransform.anchoredPosition = new Vector2(i * gap, 0);
                a.rectTransform.localEulerAngles = new Vector3(0, 0, S.belt.dir > 0 ? 90 : -90);   // the arrow sprite points down
            }
            int dir = S.belt.dir;
            if (SkinBelt(strip, arrows, dir)) return;
            LoopAnim.Add(beltStrip.gameObject, MechT.BeltLoop, p => { if (arrows != null) arrows.anchoredPosition = new Vector2(dir * gap * p - gap, 0); });
        }

        void BuildBoxLid()
        {
            int x0 = int.MaxValue, y0 = int.MaxValue;
            foreach (int t in S.box.t) { x0 = Mathf.Min(x0, S.geo.xy[t][0]); y0 = Mathf.Min(y0, S.geo.xy[t][1]); }
            var lid = UIKit.Img("NeedleBox", board, UIKit.Rounded, LidRed); boxLid = lid.rectTransform;
            boxLid.anchorMin = boxLid.anchorMax = new Vector2(0, 1); boxLid.pivot = new Vector2(.5f, .5f);
            var size = new Vector2(2 * cs + cs * GAP + 8, 2 * cs * CELL_H + cs * GAP + 8);
            boxLid.sizeDelta = size; boxLid.anchoredPosition = new Vector2(x0 * StepX - 4 + size.x / 2, -(y0 * StepY - 4 + size.y / 2));
            var shade = UIKit.Img("Shade", boxLid, UIKit.Rounded, LidDark); UIKit.Stretch(shade.rectTransform); shade.rectTransform.offsetMax = new Vector2(0, -size.y * .85f); shade.raycastTarget = false;
            var panel = UIKit.Img("Stitches", boxLid, UIKit.Rounded, LidPanel); UIKit.Stretch(panel.rectTransform, 16); panel.rectTransform.offsetMin = new Vector2(16, 22); panel.raycastTarget = false;
            var label = UIKit.Img("Label", boxLid, UIKit.Rounded, LidLabel); label.raycastTarget = false; Pill(label, cs * .62f);
            label.rectTransform.sizeDelta = new Vector2(cs * 1.3f, cs * .62f);
            var ball = UIKit.YarnImg("Ball", label.transform, S.box.c); ball.preserveAspect = true; ball.raycastTarget = false;
            ball.rectTransform.sizeDelta = new Vector2(cs * .46f, cs * .46f); ball.rectTransform.anchoredPosition = new Vector2(-cs * .3f, 0);
            boxCountText = UIKit.Txt("Count", label.transform, S.box.n.ToString(), Mathf.RoundToInt(cs * .42f), UIKit.Hex("#fffaf0"));
            boxCountText.rectTransform.sizeDelta = new Vector2(cs * .6f, cs * .6f); boxCountText.rectTransform.anchoredPosition = new Vector2(cs * .28f, 0);
            if (SK) SkinBoxLid(lid, shade, panel, label, ball);
            boxLid.gameObject.AddComponent<Button>().onClick.AddListener(BoxNudge);   // the lid takes the taps of the 4 spots under it
            shownBox = S.box.n;
        }

        /// <summary>Called at the end of RenderBoard: count, (re)build after Undo, open animation.</summary>
        void RenderMech()
        {
            if (S == null) return;
            bool closed = S.box != null && S.box.n > 0;
            if (closed)
            {
                if (boxLid == null) BuildBoxLid();
                if (shownBox != S.box.n) { shownBox = S.box.n; boxCountText.text = S.box.n.ToString(); StartCoroutine(Bump(boxCountText.transform.parent as RectTransform, 1.3f)); }
            }
            else if (boxLid != null)
            {
                var lid = boxLid; boxLid = null; shownBox = -1;
                KLog.Info("Box", "Needle box opened"); Sfx.Play(Sfx.Unlock);
                StartCoroutine(LidOpen(lid));
            }
        }
        IEnumerator LidOpen(RectTransform lid)
        {
            var cg = lid.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false; var home = lid.anchoredPosition;
            for (float t = 0; t < MechT.LidOpen; t += Time.unscaledDeltaTime)
            {
                if (lid == null) yield break; float p = t / MechT.LidOpen;
                lid.anchoredPosition = home + new Vector2(0, cs * 1.6f * p * p); lid.localEulerAngles = new Vector3(0, 0, 14 * p); cg.alpha = p < .45f ? 1 : 1 - (p - .45f) / .55f;
                yield return null;
            }
            if (lid != null) Destroy(lid.gameObject);
        }
        void BoxNudge()
        {
            if (S == null || S.over || modalRT.gameObject.activeSelf || Time.unscaledTime < introUntil) return;
            Buzz(); Sfx.Play(Sfx.Err); if (boxLid != null) StartCoroutine(Nudge(boxLid));
        }
        IEnumerator Nudge(RectTransform rt)
        {
            var home = rt.anchoredPosition;
            for (float t = 0; t < .32f; t += Time.unscaledDeltaTime) { if (rt == null) yield break; float p = t / .32f; rt.anchoredPosition = home + new Vector2(Mathf.Sin(p * 28) * 8 * (1 - p), 0); yield return null; }
            if (rt != null) rt.anchoredPosition = home;
        }

        /// <summary>After a match on a belt level: each moved stack slides in from its old spot; the wrapped one fades in from the edge.</summary>
        void AnimateBelt(List<GameEvent> ev)
        {
            foreach (var e in ev)
            {
                if (e.k != EvKind.Belt) continue;
                foreach (var m in e.moves)
                {
                    var v = cells[m.to];
                    var off = m.wrap ? new Vector2(-e.dir * StepX * .7f, 0) : CellPos(m.from) - CellPos(m.to);
                    foreach (var im in new[] { v.front, v.back, v.knot, v.ice })
                        if (im != null && im.gameObject.activeSelf) RunPos(im.rectTransform, BeltPart(im, off, m.wrap));
                }
            }
        }
        IEnumerator BeltPart(Image im, Vector2 off, bool fade)
        {
            var rt = im.rectTransform; var home = Home(rt); rt.anchoredPosition = home + off;
            yield return new WaitForSecondsRealtime(.14f);
            for (float t = 0; t < MechT.Wrap; t += Time.unscaledDeltaTime)
            {
                float p = t / MechT.Wrap, e = 1 - (1 - p) * (1 - p);
                rt.anchoredPosition = home + off * (1 - e);
                if (fade) { var c = im.color; c.a = e; im.color = c; }
                yield return null;
            }
            SnapHome(rt); posAnims.Remove(rt);
        }

        /// <summary>No path between two same-color balls: buzz, shake both, red ring blinks twice (no text).</summary>
        void NoPathFeedback(int a, int b)
        {
            Buzz(); Sfx.Play(Sfx.Err);
            foreach (int t in new[] { a, b }) { Shake(cells[t].front.rectTransform); StartCoroutine(RedFlash(cells[t])); }
        }
        IEnumerator RedFlash(CellView v)
        {
            var ring = Sub(v.rt, UIKit.Ring, NoPathRed, 1.06f, 0, 0); ring.raycastTarget = false;
            var glow = Sub(v.rt, UIKit.Circle, new Color(NoPathRed.r, NoPathRed.g, NoPathRed.b, .22f), .96f, 0, 0); glow.raycastTarget = false;
            for (float t = 0; t < MechT.Flash; t += Time.unscaledDeltaTime)
            {
                float p = t / MechT.Flash, a = KF.At(p, 0, 0, .15f, 1, .35f, .2f, .55f, 1, 1, 0);
                if (ring == null) yield break;
                ring.color = new Color(NoPathRed.r, NoPathRed.g, NoPathRed.b, a); glow.color = new Color(NoPathRed.r, NoPathRed.g, NoPathRed.b, .22f * a);
                yield return null;
            }
            if (ring != null) Destroy(ring.gameObject);
            if (glow != null) Destroy(glow.gameObject);
        }
    }
}
