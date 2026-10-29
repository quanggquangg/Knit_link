// GameController – the 28/10 feedback round (decided item by item with the designer, see claude/feedback-28-10.md):
//   item 2  full basket reward: bump + sparkles + color ring + "ting" + light haptic + Nice!/Great!/Perfect! (~0.35 s),
//           combo = baskets filled one after another with no pair sent to the shelf (the ting goes higher, the word steps up)
//   item 5  Undo plays a simple move backwards: the 2 balls fly back to their spots, the yarn line pulls back (~0.45 s);
//           moves that filled a basket, rolled the board or moved the belt keep the plain redraw (step 2 later)
//   item 6  tangle feedback: loosen = squash + wool fluff + soft rustle + light haptic;
//           untangled = yarn bits fly out + ring + sparkles + unlock sound + medium haptic + "Untangled!" (no glow on the spot)
//   item 7  "?" mystery ball: gray with a "?", can't be picked (shake + error sound + haptic + red flash), shows its color
//           when a ball next to it leaves. Placeholder art drawn in code until the artist's asset arrives (MysteryLook).
//   item 9  "Hold board to see" on the Lose and Revive popups: hold a finger on the dark area and the popup fades out
//   item 10 booster unlock: Claim +2, then a forced free first use at once (29/10, see GameController.Tutorial.cs)
//   29/10   "+" on the 2 locked Extra Shelf slots, tap = Extra Shelf (AddLockPlus)
// Every number to tune is in FB below.
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
        static class FB
        {
            public const float CheerTime = .35f;                 // basket celebration before it flies to the picture
            // the word (item 2, restyled 28/10 evening): starts this far above the basket body, game display font with a thick
            // brown outline + drop shadow, pops in tilted, floats up and fades. Colors per combo step: white, yellow, pink (+ twinkles)
            public const float WordAbove = 140, WordRise = 40, WordTime = 1.15f, WordTilt = -6, Stroke = 5f, Thicken = 1.6f;   // Stroke = brown outline, Thicken = extra letter weight
            public static readonly int[] WordSize = { 92, 102, 112 };
            public static readonly Color[] WordFill = { Color.white, UIKit.Hex("#ffd84a"), UIKit.Hex("#ff8fc8") };
            public static readonly Color WordStroke = UIKit.Hex("#6b3d1f"), WordShadow = new Color(.16f, .08f, .03f, .45f);
            public static readonly string[] CheerWords = { "Nice!", "Great!", "Perfect!" };
            // pitch of the combo "ting" per combo step, in semitones: C major pentatonic (C D E G A C D E G) so it never clashes with the music
            public static readonly int[] ComboSemitones = { 0, 2, 4, 7, 9, 12, 14, 16, 19 };
            public const float UndoFly = .38f;                   // undo: ball flight back to its spot
            public const float HoldAlpha = .04f;                 // popup opacity while "Hold board to see" is held
            public static readonly Color MysteryGray = new Color(.72f, .70f, .68f, 1f);
            // "+" on the 2 LOCKED Extra Shelf slots instead of a padlock (feedback 29/10, designer's choice): size, nudge up, color
            public const float PlusSize = 32, PlusTall = 24, PlusThick = 9, PlusUp = 0;   // width of the "+", its height, bar thickness (art px of the 94×42 slot), nudge up
            public static readonly Color PlusColor = UIKit.Hex("#b07a4c");
            // shelf balls slide left to close the gap after shelf balls flew to a basket (29/10): wait after the last one left, slide time
            public const float SlideAfter = .06f, SlideTime = .22f;
        }

        int combo;                    // baskets filled in a row without a pair going to the shelf
        static bool Coached(string k) => PlayerPrefs.GetInt("kl.coached." + k, 0) == 1;
        HashSet<int> tangleFx;        // spots whose tangle was hit by the move being drawn (so belt slides don't trigger effects)
        /// <summary>What Undo needs to play a move backwards, keyed by history.Count right after the move was pushed.</summary>
        class UndoInfo { public int a, b; public string c; public List<(int x, int y)> path; public List<GameEvent> dest; public bool complex; }
        readonly Dictionary<int, UndoInfo> undoInfo = new Dictionary<int, UndoInfo>();

        // ================= item 7: "?" mystery ball =================
        /// <summary>Gray ball with a "?" (placeholder until the artist's asset). on=false puts the normal yarn look back.</summary>
        void MysteryLook(CellView v, string c, bool on)
        {
            if (v.mq == null)
            {
                v.mq = UIKit.Txt("Mystery", v.front.transform, "?", Mathf.RoundToInt(cs * .5f), Color.white); UIKit.Stretch(v.mq.rectTransform);
                UIKit.Display(v.mq); v.mq.fontStyle = FontStyle.Bold; v.mq.raycastTarget = false;
                v.mq.gameObject.AddComponent<FollowAlpha>().Init(v.front, v.mq);   // fades with the ball (level start drop-in, undo)
                var ol = v.mq.gameObject.AddComponent<Outline>(); ol.effectColor = UIKit.Hex("#6d625a"); ol.effectDistance = new Vector2(3, -3);
            }
            var art = Skin.On ? Skin.Mystery() : null;   // the artist's "?" ball (28/10); else the gray placeholder below
            v.mq.gameObject.SetActive(on && art == null);
            if (on && art != null) { v.front.sprite = art; v.front.type = Image.Type.Simple; v.front.color = new Color(1, 1, 1, v.front.color.a); }
            else if (on) { UIKit.Yarn(v.front, "cream"); var k = FB.MysteryGray; k.a = v.front.color.a; v.front.color = Skin.On ? k : new Color(k.r * .9f, k.g * .9f, k.b * .9f, k.a); }
            else UIKit.Yarn(v.front, c);
        }
        void MysteryNudge(int t)
        {
            Buzz(); Sfx.Play(Sfx.Err); Shake(cells[t].front.rectTransform); StartCoroutine(RedFlash(cells[t]));
        }
        /// <summary>The gray ball flips to its real color.</summary>
        IEnumerator Unveil(CellView v, string c)
        {
            var rt = v.front.rectTransform; Sfx.Play(Sfx.Reveal);
            StartCoroutine(Sparkles(rt.position, UIKit.Colors[c].l, 8, cs * .6f));
            for (float t = 0; t < .38f; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break; float p = t / .38f;
                float sx = p < .4f ? p / .4f : 1, s = KF.At(p, 0, 1.1f, .6f, 1.18f, 1, 1);
                rt.localScale = new Vector3(sx * s, s, 1); yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        // ================= item 2: full basket reward + combo =================
        IEnumerator Cheer(List<GameEvent> done, int my)
        {
            foreach (var d in done)
            {
                combo++;
                Sfx.PlayPitched(Sfx.Ting, Mathf.Pow(2f, FB.ComboSemitones[Mathf.Min(combo, FB.ComboSemitones.Length) - 1] / 12f)); Haptics.Light();
                if (!basketViews.TryGetValue(d.basketId, out var v) || v.rt == null) continue;
                var col = UIKit.Colors[d.c]; var at = v.body.rectTransform.position;
                StartCoroutine(Bump(BasketBumpTarget(v), 1.18f)); StartCoroutine(RingAt(at, col.c)); StartCoroutine(Sparkles(at, col.l, 12, 130));
                int tier = Mathf.Min(combo, 3);
                StartCoroutine(CheerWord(at + Vector3.up * FB.WordAbove * canvas.scaleFactor, tier));
            }
            yield return new WaitForSecondsRealtime(FB.CheerTime);
        }

        // ================= small effects (fx layer, screen space) =================
        IEnumerator Sparkles(Vector3 pos, Color c, int n, float radius)
        {
            var dots = new List<(Image im, Vector2 dir)>();
            for (int i = 0; i < n; i++)
            {
                var im = UIKit.Img("Spark", fx, UIKit.Circle, i % 2 == 0 ? c : UIKit.Hex("#fffbe6")); im.raycastTarget = false;
                im.rectTransform.sizeDelta = new Vector2(18, 18); im.rectTransform.position = pos;
                float a = Mathf.PI * 2 * i / n + Random.Range(0f, .4f); dots.Add((im, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius * Random.Range(.55f, .9f)));
            }
            for (float t = 0; t < .52f; t += Time.unscaledDeltaTime)
            {
                float p = t / .52f, e = 1 - Mathf.Pow(1 - p, 3);
                foreach (var d in dots) { if (d.im == null) continue; d.im.rectTransform.position = pos + (Vector3)(d.dir * e * canvas.scaleFactor); d.im.rectTransform.localScale = Vector3.one * (p < .6f ? Mathf.Lerp(.4f, 1, p / .6f) : Mathf.Lerp(1, .2f, (p - .6f) / .4f)); var k = d.im.color; k.a = p < .6f ? 1 : 1 - (p - .6f) / .4f; d.im.color = k; }
                yield return null;
            }
            foreach (var d in dots) if (d.im != null) Destroy(d.im.gameObject);
        }
        /// <summary>Wool fluff (small round bits) or yarn pieces (short strands) flying out of a spot.</summary>
        IEnumerator Bits(Vector3 pos, int n, bool strands)
        {
            var bits = new List<(Image im, Vector2 dir, float rot)>();
            for (int i = 0; i < n; i++)
            {
                var im = UIKit.Img(strands ? "Strand" : "Fluff", fx, strands ? UIKit.Rounded : UIKit.Circle, strands ? UIKit.Hex("#b08a5c") : UIKit.Hex("#eadbc2")); im.raycastTarget = false;
                im.rectTransform.sizeDelta = strands ? new Vector2(cs * .3f, cs * .07f) : new Vector2(cs * .13f, cs * .13f); im.rectTransform.position = pos;
                float a = Random.Range(0, Mathf.PI * 2); bits.Add((im, new Vector2(Mathf.Cos(a), Mathf.Sin(a) + .25f) * cs * (strands ? Random.Range(.7f, 1.2f) : Random.Range(.35f, .65f)), Random.Range(0, 360f)));
            }
            float dur = strands ? .62f : .48f;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float p = t / dur, e = 1 - Mathf.Pow(1 - p, 2);
                foreach (var b in bits) { if (b.im == null) continue; b.im.rectTransform.position = pos + (Vector3)(b.dir * e * canvas.scaleFactor) + Vector3.down * 40 * p * p * canvas.scaleFactor; b.im.rectTransform.localEulerAngles = new Vector3(0, 0, b.rot + 180 * p); var k = b.im.color; k.a = 1 - p; b.im.color = k; }
                yield return null;
            }
            foreach (var b in bits) if (b.im != null) Destroy(b.im.gameObject);
        }
        /// <summary>Nice! / Great! / Perfect! above the full basket (tier 1..3).</summary>
        IEnumerator CheerWord(Vector3 pos, int tier)
        {
            // Layers (28/10 evening): Unity's Shadow/Outline effects multiply the letter mesh, so 9 of them on one Text went past
            // the 65000-vertex limit ("Mesh can not have more than 65000 vertices") and the word vanished. Now the brown outline
            // and the drop shadow are separate copies of the text drawn behind it, each with just one small Outline.
            var rt = UIKit.Rect("Cheer", fx); rt.sizeDelta = new Vector2(700, FB.WordSize[tier - 1] * 1.4f); rt.position = pos;
            var cg = rt.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false; cg.alpha = 0;
            var medium = Resources.Load<Font>("Fonts/Fredoka-Medium");   // lighter than the Fredoka Bold of titles (user 28/10)
            Text Layer(Color c, Vector2 off)
            {
                var tx = UIKit.Txt("Word", rt, FB.CheerWords[tier - 1], FB.WordSize[tier - 1], c); tx.raycastTarget = false;
                if (medium != null) tx.font = medium; else UIKit.Display(tx);
                tx.horizontalOverflow = HorizontalWrapMode.Overflow; tx.verticalOverflow = VerticalWrapMode.Overflow;
                UIKit.Stretch(tx.rectTransform); tx.rectTransform.anchoredPosition = off;
                // every layer gets the same small outline so all of them are equally "thick" (FB.Thicken)
                var o = tx.gameObject.AddComponent<Outline>(); o.effectColor = c; o.effectDistance = new Vector2(FB.Thicken, FB.Thicken);
                return tx;
            }
            Layer(FB.WordShadow, new Vector2(0, -10));                                                        // soft drop shadow
            foreach (var d in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1), new Vector2(.7f, .7f), new Vector2(-.7f, .7f), new Vector2(.7f, -.7f), new Vector2(-.7f, -.7f) })
                Layer(FB.WordStroke, d * FB.Stroke);                                                          // brown outline
            Layer(FB.WordFill[tier - 1], Vector2.zero);                                                       // the word
            // Perfect!: little 4-point twinkles around the word
            var stars = new List<Image>();
            if (tier >= 3)
                for (int i = 0; i < 5; i++)
                {
                    var st = UIKit.Img("Twinkle", rt, UIKit.Circle, UIKit.Hex("#fff6c8")); st.raycastTarget = false;
                    st.rectTransform.sizeDelta = new Vector2(16, 16); st.rectTransform.anchoredPosition = new Vector2(-230 + i * 115, (i % 2 == 0 ? 62 : -58));
                    var arm = UIKit.Img("Arm", st.rectTransform, null, UIKit.Hex("#fff6c8")); arm.rectTransform.sizeDelta = new Vector2(46, 6); arm.raycastTarget = false;
                    var arm2 = UIKit.Img("Arm", st.rectTransform, null, UIKit.Hex("#fff6c8")); arm2.rectTransform.sizeDelta = new Vector2(6, 46); arm2.raycastTarget = false;
                    stars.Add(st);
                }
            for (float t = 0; t < FB.WordTime; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break; float p = t / FB.WordTime;
                float a = p < .12f ? p / .12f : p > .72f ? 1 - (p - .72f) / .28f : 1;
                rt.position = pos + Vector3.up * KF.At(p, 0, 0, .18f, 0, 1, FB.WordRise) * canvas.scaleFactor;
                rt.localScale = Vector3.one * KF.At(p, 0, .3f, .14f, 1.22f, .26f, .94f, .36f, 1.03f, .46f, 1, 1, 1);   // pop with a little bounce
                rt.localEulerAngles = new Vector3(0, 0, KF.At(p, 0, FB.WordTilt * 2.5f, .26f, FB.WordTilt * -.6f, .46f, FB.WordTilt, 1, FB.WordTilt));
                cg.alpha = a;
                for (int i = 0; i < stars.Count; i++)
                {   // each twinkle blinks on its own beat
                    float tw = Mathf.Clamp01(Mathf.Sin((p * 3.2f + i * .23f) * Mathf.PI * 2));
                    stars[i].rectTransform.localScale = Vector3.one * (.4f + .8f * tw); stars[i].rectTransform.localEulerAngles = new Vector3(0, 0, 45 * p + i * 20);
                    foreach (var g in stars[i].GetComponentsInChildren<Image>()) { var k = g.color; k.a = tw; g.color = k; }
                }
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }
        IEnumerator FloatWord(Vector3 pos, string word, int size, Color c)
        {
            var t0 = UIKit.Display(UIKit.Txt("Word", fx, word, size, c)); t0.raycastTarget = false;
            t0.horizontalOverflow = HorizontalWrapMode.Overflow; t0.verticalOverflow = VerticalWrapMode.Overflow; t0.rectTransform.sizeDelta = new Vector2(600, size * 1.4f);
            var ol = t0.gameObject.AddComponent<Outline>(); ol.effectColor = UIKit.Hex("#6b3d1f"); ol.effectDistance = new Vector2(5, -5);
            var rt = t0.rectTransform; float dur = 1.1f;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break; float p = t / dur;
                rt.position = pos + Vector3.up * KF.At(p, 0, 0, .7f, 10, 1, 70) * canvas.scaleFactor;
                rt.localScale = Vector3.one * KF.At(p, 0, .5f, .2f, 1.15f, .35f, 1, 1, 1);
                t0.color = new Color(c.r, c.g, c.b, p < .2f ? p / .2f : p > .7f ? 1 - (p - .7f) / .3f : 1); ol.effectColor = new Color(.42f, .24f, .12f, t0.color.a);
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        // ================= item 6: tangle feedback =================
        IEnumerator KnotSquash(RectTransform rt)
        {
            for (float t = 0; t < .48f; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break; float p = t / .48f;
                rt.localScale = new Vector3(KF.At(p, 0, 1, .2f, 1.25f, .45f, .85f, .7f, 1.1f, 1, 1), KF.At(p, 0, 1, .2f, .75f, .45f, 1.2f, .7f, .92f, 1, 1), 1);
                rt.localEulerAngles = new Vector3(0, 0, KF.At(p, 0, 0, .45f, -14, .7f, 10, 1, 0)); yield return null;
            }
            if (rt != null) { rt.localScale = Vector3.one; rt.localEulerAngles = Vector3.zero; }
        }
        void TangleLoosen(CellView v) { Sfx.Play(Sfx.Rustle); Haptics.Light(); StartCoroutine(Bits(v.rt.position, 7, false)); StartCoroutine(KnotSquash(v.knot.rectTransform)); }
        void TangleGone(CellView v)
        {
            var pos = v.rt.position; Haptics.Medium();
            StartCoroutine(Bits(pos, 9, true)); StartCoroutine(RingAt(pos, UIKit.Hex("#fff3b0"))); StartCoroutine(Sparkles(pos, UIKit.Hex("#fff3b0"), 10, cs * .8f));
            StartCoroutine(FloatWord(pos + Vector3.up * cs * .35f * canvas.scaleFactor, "Untangled!", 56, UIKit.Hex("#b9f08a")));
        }

        // ================= item 5: undo backwards =================
        void RecordUndo(int a, int b, string c, List<(int x, int y)> path, List<GameEvent> ev)
        {
            undoInfo[history.Count] = new UndoInfo
            {
                a = a, b = b, c = c, path = path, dest = ev.Where(e => e.k == EvKind.Jar || e.k == EvKind.Shelf).ToList(),
                complex = ev.Any(e => e.k == EvKind.Done || e.k == EvKind.Roll || e.k == EvKind.Belt || e.k == EvKind.Over)
            };
        }
        /// <summary>Called right after Undo restored the board: the 2 balls fly back from the basket / shelf.</summary>
        void UndoBackwards(UndoInfo u)
        {
            if (u == null || u.complex || u.dest.Count < 2) return;
            int my = token;
            if (u.path != null) DrawLink(u.path, u.c);
            var spots = new[] { u.a, u.b };
            for (int i = 0; i < 2; i++)
            {
                var v = cells[spots[i]]; var d = u.dest[i];
                var from = d.k == EvKind.Jar ? BasketPos(d.basketId) : SlotPos(d.idx);
                var c = v.front.color; v.front.color = new Color(c.r, c.g, c.b, 0);
                if (d.k == EvKind.Jar && basketViews.TryGetValue(d.basketId, out var bv)) StartCoroutine(Bump(BasketBumpTarget(bv), 1.08f));
                int ii = i;
                StartCoroutine(FlyTo(from, () => v.front.rectTransform.position, u.c, i * .06f, FB.UndoFly, 1, () =>
                {
                    if (my != token) return;
                    var k = v.front.color; v.front.color = new Color(k.r, k.g, k.b, 1); StartCoroutine(Pop(v.front.rectTransform));
                    if (ii == 0) Sfx.Play(Sfx.Tap);
                }));
            }
        }

        // ================= feedback 29/10: "+" on the locked Extra Shelf slots =================
        /// <summary>Puts a "+" on the padlock of a locked shelf slot (it shows and hides with the padlock) and makes the slot a
        /// button for Extra Shelf: tap = same as the booster button (use it, or the buy popup, or "unlocks at level 15").</summary>
        void AddLockPlus(Image lk)
        {
            var old = lk.transform.Find("Plus"); if (old != null) Destroy(old.gameObject);
            if (!SK) lk.color = new Color(1, 1, 1, 0);   // old art: the "+" replaces the padlock icon
            // drawn with 2 rounded bars, not a "+" letter: a font glyph never sits exactly in the middle of the slot
            var plus = UIKit.Rect("Plus", lk.transform); plus.anchorMin = plus.anchorMax = new Vector2(.5f, .5f); plus.sizeDelta = new Vector2(FB.PlusSize, FB.PlusSize); plus.anchoredPosition = new Vector2(0, FB.PlusUp);
            foreach (var hz in new[] { true, false })
            {
                var bar = UIKit.Img(hz ? "H" : "V", plus, UIKit.Rounded, FB.PlusColor); bar.raycastTarget = false; bar.type = Image.Type.Sliced; bar.pixelsPerUnitMultiplier = 64f / FB.PlusThick;   // round ends = half the thickness
                bar.rectTransform.sizeDelta = hz ? new Vector2(FB.PlusSize, FB.PlusThick) : new Vector2(FB.PlusThick, FB.PlusTall);
            }
            lk.raycastTarget = true;
            var btn = lk.GetComponent<Button>(); if (btn == null) btn = lk.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None; btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => UseShelf());
        }

        // ================= feedback 29/10: shelf balls slide left to close gaps =================
        bool shelfSliding;            // true while SlideShelf moves shelf ball images (RenderShelf then leaves their positions alone)
        Vector2[] slotHome;           // resting positions of the shelf ball images, saved while a slide runs (null = all at rest)
        /// <summary>Puts every shelf ball image back on its own slot (after a slide, or if a slide was cut off by a level restart).</summary>
        void ResetSlotPositions()
        {
            if (slotHome == null) return;
            for (int i = 0; i < slots.Count && i < slotHome.Length; i++) slots[i].rectTransform.anchoredPosition = slotHome[i];
            slotHome = null;
        }
        /// <summary>Shelf balls that stay move left into the empty slots, in order. Only runs when what is shown matches the rules state
        /// (same colors in the same order); otherwise RenderShelf just redraws it as before.</summary>
        IEnumerator SlideShelf(float wait, int my)
        {
            yield return new WaitForSecondsRealtime(wait);
            while (shelfSliding) { if (my != token) yield break; yield return null; }   // two moves close together: one slide after the other
            if (my != token || S == null) yield break;
            var shown = new List<int>(); for (int i = 0; i < S.cap && i < slots.Count; i++) if (slotCol[i] != null) shown.Add(i);
            if (shown.Count != S.shelf.Count) yield break;
            for (int j = 0; j < shown.Count; j++) if (slotCol[shown[j]] != S.shelf[j]) yield break;
            var moves = new List<(int from, int to)>(); for (int j = 0; j < shown.Count; j++) if (shown[j] != j) moves.Add((shown[j], j));
            if (moves.Count == 0) yield break;
            slotHome = slots.Select(sl => sl.rectTransform.anchoredPosition).ToArray(); shelfSliding = true;
            for (float t = 0; t < FB.SlideTime; t += Time.unscaledDeltaTime)
            {
                if (my != token) { shelfSliding = false; yield break; }
                float p = KF.EaseOut(t / FB.SlideTime);
                foreach (var m in moves) slots[m.from].rectTransform.anchoredPosition = Vector2.Lerp(slotHome[m.from], slotHome[m.to], p);
                yield return null;
            }
            shelfSliding = false;
            // hand the colors over to the slots they slid into, then every image goes back home (same frame = no flash)
            var cols = moves.Select(m => slotCol[m.from]).ToList();
            foreach (var m in moves) SetSlot(m.from, null);
            for (int n = 0; n < moves.Count; n++) SetSlot(moves[n].to, cols[n]);
            ResetSlotPositions();
        }

        // ================= item 9: hold board to see =================
        void AddHoldToSee()
        {
            var cg = modalRT.GetComponent<CanvasGroup>(); if (cg == null) cg = modalRT.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 1;   // not "??": Unity's missing component is not C# null
            var t = UIKit.Display(UIKit.Txt("HoldToSee", modalRT, "Hold board to see", 60, Color.white)); t.raycastTarget = false;
            var ol = t.gameObject.AddComponent<Outline>(); ol.effectColor = UIKit.Hex("#3a2a22"); ol.effectDistance = new Vector2(4, -4);
            var rt = t.rectTransform; rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(.5f, 0); rt.sizeDelta = new Vector2(0, 90); rt.anchoredPosition = new Vector2(0, 150);
            LoopAnim.Add(t.gameObject, 1.6f, p => { if (t != null) { var k = t.color; k.a = .55f + .45f * Mathf.Sin(p * Mathf.PI); t.color = k; } });
            var dim = modalRT.Find("Dim"); if (dim == null) return;
            var trig = dim.gameObject.GetComponent<EventTrigger>(); if (trig == null) trig = dim.gameObject.AddComponent<EventTrigger>();
            void On(EventTriggerType type, System.Action act) { var e = new EventTrigger.Entry { eventID = type }; e.callback.AddListener(_ => act()); trig.triggers.Add(e); }
            On(EventTriggerType.PointerDown, () => { if (cg != null) cg.alpha = FB.HoldAlpha; });
            On(EventTriggerType.PointerUp, () => { if (cg != null) cg.alpha = 1; });
        }
    }

    /// <summary>Keeps a child text as see-through as its parent image (the "?" mark on a yarn ball that fades in or out).</summary>
    public class FollowAlpha : MonoBehaviour
    {
        Graphic src, dst; float baseA = 1;
        public void Init(Graphic from, Graphic to) { src = from; dst = to; baseA = to.color.a; }
        void LateUpdate() { if (src == null || dst == null) return; var c = dst.color; c.a = baseA * src.color.a; dst.color = c; }
    }
}
