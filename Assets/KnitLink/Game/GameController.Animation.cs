// GameController – Every movement: balls flying to baskets/shelf, knitting into the picture, rolling, tangles/ice breaking, toast, yarn line.
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
        // ---------- animation ----------
        IEnumerator AnimateThen(List<GameEvent> ev, Dictionary<int, Vector3> origins, float start, System.Action then)
        {
            foreach (var e in ev) if (e.k == EvKind.Done && !busyBaskets.Contains(e.basketId)) busyBaskets.Add(e.basketId);
            pending++;
            try { yield return Animate(ev, origins, start); }
            finally { pending--; }
            then?.Invoke();
        }

        IEnumerator Animate(List<GameEvent> ev, Dictionary<int, Vector3> origins, float start)
        {
            int my = token;
            var leaving = ev.Where(e => e.k == EvKind.Leave).SelectMany(e => e.items).ToList();
            // baskets that open and fill within this move need a view right away
            {
                int n = 0;
                foreach (var e in ev)
                {
                    if (e.k == EvKind.Jar || e.k == EvKind.Shelf || e.k == EvKind.Over)
                    {
                        var it = n < leaving.Count ? leaving[n] : default(LeaveItem); n++;
                        if (e.k == EvKind.Jar) EnsureBasket(e.basketId, it.c);
                    }
                    else if (e.k == EvKind.Absorb || e.k == EvKind.Done) EnsureBasket(e.basketId, e.c);
                }
            }
            RenderBaskets(false);   // the full basket stays until its balls land; the next one already moves up
            var flights = new List<Coroutine>(); var doneEv = new List<GameEvent>(); var shelfUsed = new List<int>();
            int li = 0; float delay = start;
            foreach (var e in ev)
            {
                if (e.k == EvKind.Jar || e.k == EvKind.Shelf || e.k == EvKind.Over)
                {
                    if (li >= leaving.Count) continue; var it = leaving[li++];
                    if (!origins.TryGetValue(it.id, out var from)) from = cells[it.t].rt.position;
                    float d = delay; delay += leaving.Count > 4 ? .11f : .07f; var ek = e;
                    flights.Add(StartCoroutine(FlyTo(from, () => ek.k == EvKind.Jar ? BasketPos(ek.basketId) : ek.k == EvKind.Shelf ? SlotPos(ek.idx) : shelfRow.position,
                        it.c, d, .44f, ek.k == EvKind.Jar ? .45f : ShelfLandScale, () =>
                        {
                            if (my != token) return;
                            if (ek.k == EvKind.Jar) LandIn(ek.basketId);
                            else if (ek.k == EvKind.Shelf) { SetSlot(ek.idx, it.c); Sfx.Play(Sfx.Shelf); }   // the slot shows the ball the moment it lands (no empty frame = no flash)
                        })));
                }
                else if (e.k == EvKind.Absorb)
                {
                    int idx = -1; for (int i = 0; i < slots.Count; i++) if (!shelfUsed.Contains(i) && slotCol[i] == e.c) { idx = i; break; }
                    if (idx < 0) continue; shelfUsed.Add(idx); float d = delay + .25f; delay += .06f; var ek = e; int sidx = idx;
                    flights.Add(StartCoroutine(FlyTo(slots[sidx].rectTransform.position, () => BasketPos(ek.basketId), e.c, d, .42f, .45f,
                        () => { if (my == token) LandIn(ek.basketId); }, () => SetSlot(sidx, null))));
                }
                else if (e.k == EvKind.BagAbsorb)
                {   // from the bag chip's group of that color; the bag state is already updated, so redrawing removes it from the group
                    float d = delay + .25f; delay += .06f; var ek = e; var from = BagTarget(e.c);
                    flights.Add(StartCoroutine(FlyTo(from, () => BasketPos(ek.basketId), e.c, d, .42f, .45f,
                        () => { if (my == token) LandIn(ek.basketId); }, () => { if (my == token) RenderBag(); })));
                }
                else if (e.k == EvKind.Done) doneEv.Add(e);
            }
            foreach (var f in flights) yield return f;
            if (my != token) yield break;
            RenderShelf(); RenderBoosters();
            if (doneEv.Count > 0)
            {
                Sfx.Play(Sfx.Pour);
                var knits = doneEv.Select(d => StartCoroutine(Knit(d.basketId, d.li, d.c, my))).ToList();
                RenderBaskets(false);
                foreach (var k in knits) yield return k;
                if (my != token) yield break;
                Sfx.Play(Sfx.Done);
            }
            RenderBaskets(pending <= 1); DrawPicture();   // don't jump counters while another move's balls are still flying
        }
        static bool SameColor(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < .02f;
        Vector3 BasketPos(string id) => basketViews.TryGetValue(id, out var v) && v.rt != null ? v.body.rectTransform.position : basketRow.position;
        Vector3 SlotPos(int i) => slots[Mathf.Clamp(i, 0, slots.Count - 1)].rectTransform.position;

        void LandIn(string id)
        {
            if (!basketViews.TryGetValue(id, out var v)) return;
            Sfx.Play(Sfx.Clink);
            v.shown = Mathf.Min(4, v.shown + 1); UpdateCount(v); StartCoroutine(Bump(BasketBumpTarget(v)));
            if (v.shown >= 1 && v.fill[v.shown - 1] != null) StartCoroutine(Pop(v.fill[v.shown - 1].rectTransform));   // the new yarn drops into the basket
        }

        /// <summary>Flying ball size at the end of a flight to the shelf = the shelf ball size, so nothing jumps when it lands.</summary>
        float ShelfLandScale => SK ? SkinLay.SlotBall / (cs * .84f) : Lay.SlotSize / (cs * .84f);
        IEnumerator FlyTo(Vector3 from, System.Func<Vector3> to, string c, float delay, float dur, float scaleTo, System.Action onLand, System.Action onStart = null)
        {
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            onStart?.Invoke();
            var im = UIKit.YarnImg("Fly", fx, c); im.preserveAspect = true; if (SK) UIKit.DropShadow(im, 16, 12, ShadowNavy); var rt = im.rectTransform;
            float size = cs * .84f; rt.sizeDelta = new Vector2(size, size); rt.position = from;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.Clamp01(t / dur), e = p < .5f ? 2 * p * p : 1 - Mathf.Pow(-2 * p + 2, 2) / 2;
                var target = to(); var mid = (from + target) / 2 + Vector3.up * 60 * canvas.scaleFactor;
                var a = Vector3.Lerp(from, mid, e); var b = Vector3.Lerp(mid, target, e);
                rt.position = Vector3.Lerp(a, b, e); rt.localScale = Vector3.one * Mathf.Lerp(1, scaleTo, e); rt.localEulerAngles = new Vector3(0, 0, -360 * e);   // a whole turn: lands upright like the ball it becomes
                yield return null;
            }
            Destroy(im.gameObject); onLand?.Invoke();
        }

        IEnumerator Knit(string id, int li, string c, int my)
        {
            leavingBaskets.Add(id);
            Image line = null, dot = null;
            try
            {
                basketViews.TryGetValue(id, out var v); var col = UIKit.Colors[c];
                if (v != null)
                {   // the full basket lifts out of the row towards the picture
                    if (v.label != null) v.label.SetActive(false);
                    Vector3 p0 = v.rt.position, p1 = picImg.rectTransform.position + new Vector3(170, -60, 0) * canvas.scaleFactor;
                    for (float t = 0; t < .22f; t += Time.unscaledDeltaTime)
                    {
                        if (my != token) yield break; float e = 1 - Mathf.Pow(1 - t / .22f, 3);
                        v.rt.position = Vector3.Lerp(p0, p1, e); v.rt.localScale = Vector3.one * Mathf.Lerp(v.targetScale, .62f * v.targetScale, e); v.cg.alpha = 1; yield return null;
                    }
                }
                line = UIKit.Img("Strand", fx, null, col.c); var lrt = line.rectTransform; lrt.pivot = new Vector2(0, .5f);
                dot = UIKit.Img("StrandEnd", fx, UIKit.Circle, col.l); dot.rectTransform.sizeDelta = new Vector2(22, 22);
                var first = StitchWorld(li, 0);
                for (float t = 0; t < .15f; t += Time.unscaledDeltaTime)
                {
                    if (my != token) yield break; float e = 1 - Mathf.Pow(1 - t / .15f, 3);
                    var a = BasketPos(id); SetLine(lrt, dot.rectTransform, a, Vector3.Lerp(a, first, e)); yield return null;
                }
                for (float t = 0; t < .5f; t += Time.unscaledDeltaTime)
                {
                    if (my != token) yield break; float p = t / .5f; prog[li] = p; DrawPicture();
                    SetLine(lrt, dot.rectTransform, BasketPos(id), StitchWorld(li, p));
                    if (v != null) { v.shown = 4 - Mathf.Min(4, Mathf.FloorToInt(p * 4.4f)); UpdateCount(v); }
                    yield return null;
                }
                prog[li] = 1; DrawPicture(); Destroy(line.gameObject); Destroy(dot.gameObject); line = null; dot = null;
                if (my != token) yield break;
                int stitches = L.picture.Count(k => k == li);
                if (v != null) StartCoroutine(RingAt(v.body.rectTransform.position, col.l));
                StartCoroutine(StitchesPop(stitches));
                if (v != null)
                    for (float t = 0; t < .2f; t += Time.unscaledDeltaTime) { float p = t / .2f; v.cg.alpha = 1 - p; v.rt.localScale = Vector3.one * Mathf.Lerp(1, .72f, p); yield return null; }
            }
            finally
            {
                busyBaskets.Remove(id); leavingBaskets.Remove(id);
                if (basketViews.TryGetValue(id, out var gone)) { Destroy(gone.rt.gameObject); basketViews.Remove(id); }
                if (line != null) Destroy(line.gameObject);
                if (dot != null) Destroy(dot.gameObject);
            }
        }
        void SetLine(RectTransform lrt, RectTransform dot, Vector3 a, Vector3 b)
        {
            var d = b - a; float len = d.magnitude / canvas.scaleFactor;
            lrt.position = a; lrt.sizeDelta = new Vector2(len, 10); lrt.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            dot.position = b;
        }

        IEnumerator RingAt(Vector3 pos, Color c)
        {
            var im = UIKit.Img("Ring", fx, UIKit.Ring, c); im.rectTransform.position = pos; im.rectTransform.sizeDelta = new Vector2(120, 120);
            for (float t = 0; t < .52f; t += Time.unscaledDeltaTime) { float p = t / .52f; im.rectTransform.localScale = Vector3.one * Mathf.Lerp(.5f, 2.2f, p); im.color = new Color(c.r, c.g, c.b, 1 - p); yield return null; }
            Destroy(im.gameObject);
        }
        IEnumerator StitchesPop(int n)
        {
            var bgI = UIKit.Img("Stitches", fx, UIKit.Rounded, UIKit.Hex("#fffaf0")); var rt = bgI.rectTransform; rt.sizeDelta = new Vector2(280, 70);
            var t0 = UIKit.Txt("T", rt, $"+{n} stitches", 36, UIKit.Ink); UIKit.Stretch(t0.rectTransform);
            var cg = bgI.gameObject.AddComponent<CanvasGroup>(); var basePos = picImg.rectTransform.position;
            for (float t = 0; t < 1.3f; t += Time.unscaledDeltaTime)
            {
                float p = t / 1.3f; rt.position = basePos + Vector3.up * Mathf.Lerp(-10, 40, p) * canvas.scaleFactor;
                cg.alpha = p < .18f ? p / .18f : p > .75f ? 1 - (p - .75f) / .25f : 1; rt.localScale = Vector3.one * (p < .18f ? Mathf.Lerp(.7f, 1.06f, p / .18f) : 1); yield return null;
            }
            Destroy(bgI.gameObject);
        }

        void AnimateRoll(List<GameEvent> ev)
        {
            foreach (var e in ev) if (e.k == EvKind.Roll) foreach (var m in e.moves)
                {
                    var v = cells[m.to]; var off = CellPos(m.from) - CellPos(m.to); float dist = off.magnitude / (cs * (1 + GAP));
                    RunPos(v.front.rectTransform, RollPart(v.front.rectTransform, off, dist, true));
                    RunPos(v.back.rectTransform, RollPart(v.back.rectTransform, off, dist, false));
                }
        }

        // ---- position animations always return to a fixed home spot, and a new one cancels the old one,
        //      so overlapping roll / rise / shake can never leave a ball stuck off-centre ----
        readonly Dictionary<RectTransform, Coroutine> posAnims = new Dictionary<RectTransform, Coroutine>();
        Vector2 Home(RectTransform rt)
        {
            foreach (var c in cells) if (c.back != null && c.back.rectTransform == rt) return new Vector2(cs * .3f, cs * .3f);
            return Vector2.zero;
        }
        void RunPos(RectTransform rt, IEnumerator co)
        {
            if (rt == null) return;
            if (posAnims.TryGetValue(rt, out var old) && old != null) StopCoroutine(old);
            SnapHome(rt); rt.localScale = Vector3.one;
            posAnims[rt] = StartCoroutine(co);
        }
        void SnapHome(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchoredPosition = Home(rt);
            var img = rt.GetComponent<Image>(); if (img != null) { var c = img.color; c.a = 1; img.color = c; }
        }
        IEnumerator RollPart(RectTransform rt, Vector2 off, float dist, bool bump)
        {
            var home = Home(rt); rt.anchoredPosition = home + off;
            yield return new WaitForSecondsRealtime(.14f);
            float dur = .2f + dist * .07f;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime) { float p = t / dur; rt.anchoredPosition = home + off * (1 - p * p); yield return null; }
            rt.anchoredPosition = home;
            if (bump) for (float t = 0; t < .2f; t += Time.unscaledDeltaTime) { rt.localScale = Vector3.one * (1 + .06f * Mathf.Sin(t / .2f * Mathf.PI)); yield return null; }
            rt.localScale = Vector3.one; posAnims.Remove(rt);
        }

        IEnumerator Rise(RectTransform rt)
        {
            var home = Home(rt); var img = rt.GetComponent<Image>();
            for (float t = 0; t < .36f; t += Time.unscaledDeltaTime)
            {
                float p = t / .36f, e = 1 - Mathf.Pow(1 - p, 3);
                rt.localScale = Vector3.one * Mathf.Lerp(.62f, 1, e); rt.anchoredPosition = home + new Vector2(cs * .2f, cs * .2f) * (1 - e);
                var c = img.color; c.a = e; img.color = c; yield return null;
            }
            rt.localScale = Vector3.one; SnapHome(rt); posAnims.Remove(rt);
        }
        IEnumerator Poof(CellView v)
        {
            var im = UIKit.Img("Poof", v.rt, KnotArt(true), Color.white); im.preserveAspect = true; im.rectTransform.sizeDelta = new Vector2(cs * .9f, cs * .9f);
            for (float t = 0; t < .38f; t += Time.unscaledDeltaTime) { float p = t / .38f; im.rectTransform.localScale = Vector3.one * (1 + .5f * p); im.rectTransform.localEulerAngles = new Vector3(0, 0, 40 * p); im.color = new Color(1, 1, 1, 1 - p); yield return null; }
            Destroy(im.gameObject);
        }
        IEnumerator IceBreak(CellView v)
        {
            var im = UIKit.Img("IceBreak", v.rt, UIKit.Rounded, new Color(.74f, .9f, 1f, .66f)); im.rectTransform.sizeDelta = new Vector2(cs * .92f, cs * .92f);
            for (float t = 0; t < .42f; t += Time.unscaledDeltaTime) { float p = t / .42f; im.rectTransform.localScale = Vector3.one * (1 + .5f * p); im.color = new Color(.74f, .9f, 1f, .66f * (1 - p)); yield return null; }
            Destroy(im.gameObject);
        }
        IEnumerator Wiggle(RectTransform rt)
        {
            for (float t = 0; t < .42f; t += Time.unscaledDeltaTime) { float p = t / .42f; rt.localEulerAngles = new Vector3(0, 0, Mathf.Sin(p * 18) * 12 * (1 - p)); rt.localScale = Vector3.one * (1 + .15f * (1 - p)); yield return null; }
            rt.localEulerAngles = Vector3.zero; rt.localScale = Vector3.one;
        }
        IEnumerator Bump(RectTransform rt, float amt = 1.1f)
        {
            if (rt == null) yield break;
            for (float t = 0; t < .26f; t += Time.unscaledDeltaTime) { if (rt == null) yield break; float p = t / .26f; rt.localScale = Vector3.one * (1 + (amt - 1) * Mathf.Sin(p * Mathf.PI)); yield return null; }
            if (rt != null) rt.localScale = Vector3.one;
        }
        IEnumerator Pop(RectTransform rt)
        {
            for (float t = 0; t < .3f; t += Time.unscaledDeltaTime) { if (rt == null) yield break; float p = t / .3f; rt.localScale = Vector3.one * Mathf.Lerp(.55f, 1, 1 - Mathf.Pow(1 - p, 3)); yield return null; }
            if (rt != null) rt.localScale = Vector3.one;
        }
        void Shake(RectTransform rt) => RunPos(rt, ShakeCo(rt));
        IEnumerator ShakeCo(RectTransform rt)
        {
            var home = Home(rt);
            for (float t = 0; t < .35f; t += Time.unscaledDeltaTime) { float p = t / .35f; rt.anchoredPosition = home + new Vector2(Mathf.Sin(p * 30) * 10 * (1 - p), 0); yield return null; }
            SnapHome(rt); posAnims.Remove(rt);
        }
        IEnumerator Delay(float s, System.Action a) { yield return new WaitForSecondsRealtime(s); a(); }

        void DrawLink(List<(int x, int y)> path, string c)
        {
            if (path == null) return; if (SkinLink(path, c)) return; var col = UIKit.Colors[c];
            var holder = UIKit.Rect("Link", board); holder.anchorMin = holder.anchorMax = new Vector2(0, 1); holder.pivot = new Vector2(0, 1); holder.sizeDelta = Vector2.zero; holder.anchoredPosition = Vector2.zero;
            float w = Mathf.Max(10, cs * .09f);
            for (int i = 0; i + 1 < path.Count; i++)
            {
                var a = GridPos(path[i].x, path[i].y); var b = GridPos(path[i + 1].x, path[i + 1].y);
                var seg = UIKit.Img("Seg", holder, null, col.c); var rt = seg.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, .5f);
                rt.anchoredPosition = a; rt.sizeDelta = new Vector2((b - a).magnitude + w * .5f, w); rt.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            }
            foreach (var p in new[] { path[0], path[path.Count - 1] })
            {
                var ring = UIKit.Img("End", holder, UIKit.Ring, col.l); ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0, 1);
                ring.rectTransform.anchoredPosition = GridPos(p.x, p.y); ring.rectTransform.sizeDelta = new Vector2(cs * .8f, cs * .8f);
            }
            StartCoroutine(FadeOut(holder, .62f));
        }
        IEnumerator FadeOut(RectTransform rt, float dur)
        {
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime) { cg.alpha = t < dur * .55f ? 1 : 1 - (t - dur * .55f) / (dur * .45f); yield return null; }
            Destroy(rt.gameObject);
        }

        void Toast(string msg)
        {
            toastText.text = msg; toastRT.gameObject.SetActive(true); SkinBubbleFit(toastRT, toastText);
            if (toastCo != null) StopCoroutine(toastCo); toastCo = StartCoroutine(ToastCo());
        }
        IEnumerator ToastCo()
        {
            var cg = toastRT.GetComponent<CanvasGroup>();
            for (float t = 0; t < .18f; t += Time.unscaledDeltaTime) { cg.alpha = t / .18f; yield return null; }
            cg.alpha = 1; yield return new WaitForSecondsRealtime(1.7f);
            for (float t = 0; t < .22f; t += Time.unscaledDeltaTime) { cg.alpha = 1 - t / .22f; yield return null; }
            toastRT.gameObject.SetActive(false);
        }
    }
}
