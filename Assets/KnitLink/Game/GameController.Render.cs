// GameController – Drawing the current state: board spots (ball, stack number, tangle, ice), shelf, yarn bag, boosters, baskets, knitted picture.
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
        // ---------- rendering ----------
        void RenderAll() { RenderBoard(); RenderBaskets(true); RenderShelf(); RenderBoosters(); DrawPicture(); }

        void RenderBoard()
        {
            for (int t = 0; t < cells.Count; t++)
            {
                var v = cells[t]; var st = S.stacks[t]; int hp = S.tg[t];
                bool hit = tangleFx != null && tangleFx.Contains(t);   // this move pulled at this tangle (not a belt slide)
                if (v.shownHp > 0 && hp == 0) { StartCoroutine(Poof(v)); Sfx.Play(Sfx.Unlock); if (hit) TangleGone(v); }
                if (hp > 0 && hp < v.shownHp) { if (hit) TangleLoosen(v); else StartCoroutine(Wiggle(v.knot.rectTransform)); }
                v.knot.gameObject.SetActive(hp > 0); if (hp > 0) v.knot.sprite = KnotArt(hp < 2);
                v.shownHp = hp;
                int ice = S.Frozen(t) ? S.ice[t] : 0; bool thawed = v.shownIce > 0 && ice == 0;
                if (v.shownIce > 0 && ice == 0) StartCoroutine(SK ? IceShatter(v) : IceBreak(v));
                else if (ice > 0 && ice < v.shownIce) StartCoroutine(Wiggle(v.ice.rectTransform));
                v.ice.gameObject.SetActive(ice > 0); if (ice > 0) v.iceCount.text = ice.ToString();
                v.shownIce = ice;
                var top = S.Top(t);
                // a frozen spot shows only the ice block (no ball color, no stack number)
                v.front.gameObject.SetActive(top != null && ice == 0); v.back.gameObject.SetActive(st.Count > 1 && ice == 0);
                if (st.Count > 1) v.backCount.text = (st.Count - 1).ToString();
                v.shadow.gameObject.SetActive(hp == 0); if (SK) SkinShadow(v, top == null); else v.shadow.color = top == null ? new Color(.63f, .5f, .35f, .06f) : new Color(.63f, .5f, .35f, .12f);
                v.selRing.gameObject.SetActive(sel == t); if (SK && sel == t && top != null) SkinSelRing(v, top.c);
                if (top != null)
                {
                    bool unveil = v.shownMystery && v.shownTop == top.id && !top.mystery;   // "?" ball shows its color now
                    MysteryLook(v, top.c, top.mystery); v.shownMystery = top.mystery;
                    if (unveil) StartCoroutine(Unveil(v, top.c));
                    bool risen = (v.shownTop > 0 && v.shownTop != top.id && v.shownCount > st.Count) || (thawed && ice == 0);
                    if (risen) RunPos(v.front.rectTransform, Rise(v.front.rectTransform));
                    v.front.rectTransform.localScale = sel == t ? Vector3.one * 1.1f : Vector3.one;
                }
                if (top == null) v.shownMystery = false;
                v.shownTop = top != null ? top.id : -1; v.shownCount = st.Count;
            }
            RenderMech();
        }

        /// <summary>Shelf: always 8 slots (fixed layout); slots at index >= S.cap show a padlock until Extra Shelf.</summary>
        void RenderShelf()
        {
            int n = S.cap;
            if (!shelfSliding) ResetSlotPositions();   // a cut-off slide never leaves a ball between two slots
            for (int i = 0; i < slots.Count; i++)
            {
                bool locked = i >= n, has = !locked && i < S.shelf.Count;
                SetSlotLocked(i, locked); if (!locked) SetSlot(i, has ? S.shelf[i] : null);
            }
            bool warn = S.shelf.Count >= n - 2;
            if (SK) { }   // new art: no shelf count (removed 25/10)
            else { shelfCount.text = $"SHELF  {S.shelf.Count}/{n}"; shelfCount.color = warn ? UIKit.Hex("#ff9aa8") : Sky; }
            RenderBag(); RenderCoins();
        }

        void RenderBoosters()
        {
            foreach (var k in BKeys)
            {
                bool lk = Locked(k), used = k == "shelf" && S != null && S.cap > Cfg.shelfSlots; int n = Inv(k);
                bButtons[k].GetComponent<Image>().color = lk || used ? UIKit.Hex("#d9d0bf") : Pale;
                bLabels[k].color = lk || used ? UIKit.Hex("#5f5649") : UIKit.Hex("#6b5444");
                bButtons[k].transform.Find("Icon").GetComponentsInChildren<Graphic>().ToList().ForEach(g => { var c = g.color; c.a = lk ? .55f : 1; g.color = c; });
                bBadges[k].color = lk ? UIKit.Hex("#6b6458") : n > 0 ? UIKit.Hex("#f08a3c") : UIKit.Hex("#4fae5b");
                bBadges[k].rectTransform.sizeDelta = new Vector2(lk ? 104 : 70, 66);
                bCounts[k].fontSize = lk ? 30 : 36;
                bCounts[k].text = lk ? $"Lv{Unlock[k]}" : n > 0 ? n.ToString() : "+";
                if (SK) { bButtons[k].GetComponent<Image>().color = lk || used ? new Color(.78f, .78f, .78f, 1) : Color.white; SkinBooster(k, lk, n); }
                if (coach != null && coach.booster == k) FreeBadge(k);   // first, coached use: "Free" instead of the count
            }
        }

        /// <summary>Green "Free" pill on a booster button (its first, coached use).</summary>
        void FreeBadge(string k)
        {
            var badge = bBadges[k]; var t = bCounts[k];
            badge.sprite = UIKit.Rounded; badge.type = Image.Type.Sliced; badge.preserveAspect = false; Pill(badge, 58); badge.color = UIKit.Hex("#4fae5b");
            t.text = "Free"; t.fontSize = 32; t.color = Color.white; UIKit.Lift(t, 2);
            float w = Mathf.Max(100, t.cachedTextGeneratorForLayout.GetPreferredWidth(Loc.T("Free"), t.GetGenerationSettings(Vector2.zero)) / t.pixelsPerUnit + 34);
            badge.rectTransform.sizeDelta = new Vector2(w, 58);
        }

        /// <summary>Basket row: a full basket waiting for its last balls, the basket taking yarn, then the next ones as a smaller preview (3 shown at most).</summary>
        void RenderBaskets(bool syncCounts)
        {
            var layout = new List<string>(busyBaskets.Where(id => !leavingBaskets.Contains(id)));
            var byId = new Dictionary<string, Basket>();
            for (int i = 0; i < S.q.Count && layout.Count < 3; i++)
            {
                if (i >= S.Active + 2) break;
                layout.Add(S.q[i].id); byId[S.q[i].id] = S.q[i];
            }
            if (layout.Count > 3) layout.RemoveRange(3, layout.Count - 3);
            var keep = new HashSet<string>(layout); keep.UnionWith(leavingBaskets);
            foreach (var id in basketViews.Keys.ToList())
                if (!keep.Contains(id)) { Destroy(basketViews[id].rt.gameObject); basketViews.Remove(id); }
            basketOrder.Clear(); basketOrder.AddRange(layout);
            float x = 0; float prevW = 0;
            for (int i = 0; i < layout.Count; i++)
            {
                string id = layout[i]; bool preview = byId.TryGetValue(id, out var j) && S.q.IndexOf(j) >= S.Active;
                var v = EnsureBasket(id, j != null ? j.c : basketViews.TryGetValue(id, out var ov) ? ov.color : "cream");
                v.preview = preview; v.targetScale = SK ? (preview ? SkinLay.BasketNext : SkinLay.Basket) : preview ? .66f : 1f; v.targetAlpha = preview && !SK ? .6f : 1f;
                if (SK) SkinBasketDim(v, preview);   // new art: the next baskets are smaller and darker, not see-through
                if (v.label != null) v.label.SetActive(!preview);
                float w = 200 * v.targetScale;
                x = i == 0 ? (SK ? w / 2 + 8 : 110) : x + prevW / 2 + w / 2 + (SK ? 6 : 18); prevW = w; v.targetX = x;
                if (syncCounts && j != null) { v.shown = j.fill; UpdateCount(v); }
            }
            basketsLeft.text = S.q.Count > 0 ? $"{S.q.Count} LEFT" : "ALL KNITTED";
        }

        BasketView EnsureBasket(string id, string c)
        {
            if (basketViews.TryGetValue(id, out var v)) return v;
            v = new BasketView { color = c };
            v.rt = UIKit.Rect("Basket_" + id, basketRow); v.rt.anchorMin = v.rt.anchorMax = new Vector2(0, .5f); v.rt.sizeDelta = new Vector2(200, 200);
            v.cg = v.rt.gameObject.AddComponent<CanvasGroup>(); v.cg.alpha = 0;
            if (SK && SkinBasket(v, c)) { v.targetX = 640; v.rt.anchoredPosition = new Vector2(660, SkinLay.BasketBottomY); v.rt.localScale = Vector3.one * .4f; basketViews[id] = v; return v; }
            var handle = UIKit.Img("Handle", v.rt, UIKit.Ring, UIKit.Colors[c].d); handle.rectTransform.sizeDelta = new Vector2(130, 130); handle.rectTransform.anchoredPosition = new Vector2(0, 30);
            // yarn inside the basket: sits in the mouth, its lower half hidden by the rim/body drawn after it.
            // Pile order: left, right, middle, top (BasketFillPos). One ball per yarn that landed.
            for (int i = 0; i < 4; i++)
            {
                var bl = UIKit.Img("Yarn" + i, v.rt, UIKit.Ball, UIKit.C(c)); bl.rectTransform.sizeDelta = new Vector2(64, 64);
                bl.rectTransform.anchoredPosition = BasketFillPos[i]; bl.gameObject.SetActive(false); v.fill[i] = bl;
            }
            v.body = UIKit.Img("Body", v.rt, UIKit.Rounded, UIKit.C(c)); v.body.rectTransform.sizeDelta = new Vector2(190, 120); v.body.rectTransform.anchoredPosition = new Vector2(0, -30);
            v.rim = UIKit.Img("Rim", v.rt, UIKit.Rounded, UIKit.Colors[c].l); v.rim.rectTransform.sizeDelta = new Vector2(200, 30); v.rim.rectTransform.anchoredPosition = new Vector2(0, 30);
            var lab = UIKit.Img("Label", v.body.transform, UIKit.Rounded, UIKit.Hex("#fbf3e3")); lab.rectTransform.sizeDelta = new Vector2(110, 60); lab.rectTransform.anchoredPosition = new Vector2(0, -6);
            v.label = lab.gameObject;
            v.count = UIKit.Txt("Count", lab.transform, "0/4", 38, UIKit.Colors[c].d); UIKit.Stretch(v.count.rectTransform);
            v.targetX = 640; v.rt.anchoredPosition = new Vector2(660, -10); v.rt.localScale = Vector3.one * .4f;
            basketViews[id] = v;
            return v;
        }
        static readonly Vector2[] BasketFillPos = { new Vector2(-48, 46), new Vector2(48, 46), new Vector2(0, 42), new Vector2(0, 80) };
        void UpdateCount(BasketView v)
        {
            int n = Mathf.Clamp(v.shown, 0, 4); v.count.text = $"{n}/4";
            for (int i = 0; i < 4; i++) if (v.fill[i] != null) v.fill[i].gameObject.SetActive(i < n && !v.preview);
        }

        void DrawPicture()
        {
            var open = new HashSet<int>(); for (int i = 0; i < S.Active && i < S.q.Count; i++) open.Add(S.q[i].li);
            var listPos = PictureOrder(); int done = 0;
            System.Func<int, bool> isDone = k => listPos[k].pos < Mathf.RoundToInt(prog[L.picture[k]] * listPos[k].len);
            for (int k = 0; k < PIC * PIC; k++) if (isDone(k)) done++;
            PictureRenderer.Draw(picTex, L, isDone, open);
            knittedText.text = $"{Mathf.RoundToInt(100f * done / (PIC * PIC))}% knitted";
        }
        (int pos, int len)[] picOrder; LevelDef picOrderFor;
        /// <summary>Stitch order per layer: bottom rows first (as in the web demo).</summary>
        (int pos, int len)[] PictureOrder()
        {
            if (picOrderFor == L && picOrder != null) return picOrder;
            picOrder = new (int pos, int len)[PIC * PIC];
            for (int li = 0; li < L.baskets.Length; li++)
            {
                int layer = li;
                var list = Enumerable.Range(0, PIC * PIC).Where(k => L.picture[k] == layer).OrderByDescending(k => k / PIC).ThenBy(k => Hash(k)).ToList();
                for (int i = 0; i < list.Count; i++) picOrder[list[i]] = (i, list.Count);
            }
            picOrderFor = L; return picOrder;
        }
        static float Hash(int k) { unchecked { uint h = (uint)(k + 1) * 374761393u; h = (h ^ (h >> 13)) * 1274126177u; return (h ^ (h >> 16)) / 4294967296f; } }
        Vector3 StitchWorld(int li, float p)
        {
            var order = PictureOrder(); int len = 0, best = -1;
            for (int k = 0; k < PIC * PIC; k++) if (L.picture[k] == li) len = order[k].len;
            int target = Mathf.Clamp(Mathf.RoundToInt(p * len), 0, Mathf.Max(0, len - 1));
            for (int k = 0; k < PIC * PIC; k++) if (L.picture[k] == li && order[k].pos == target) { best = k; break; }
            var r = picImg.rectTransform; var corners = new Vector3[4]; r.GetWorldCorners(corners);
            if (best < 0) return (corners[0] + corners[2]) / 2;
            float u = (best % PIC + .5f) / PIC, v = 1 - (best / PIC + .5f) / PIC;
            return new Vector3(Mathf.Lerp(corners[0].x, corners[2].x, u), Mathf.Lerp(corners[0].y, corners[2].y, v), 0);
        }
    }
}
