// GameController – Popup card kit (design canvas 20/10). Every popup is a cream card built top-down:
//   card.Pill("OUT OF UNDO") · card.Title("Undo") · card.BigIcon(...) · card.Lead("Take back your last move.")
//   card.Choice(price, coinAct, adAct) · card.Progress(pct) · card.BigButton(...) · card.Link(...)
// The card is centred below the coin HUD (which stays visible above the dim), springs in, and can have a red X.
// Breath (slow pulse) goes on the button we want pressed: coins when affordable, otherwise the ad; Claim ×3 on win.
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
        static readonly Color CardBg = UIKit.Hex("#f7eedc"), CardRing = UIKit.Hex("#e8d6ae"), CardText = UIKit.Hex("#5c4a3c"), CardSoft = UIKit.Hex("#6b5444");
        public enum PillStyle { Gold, Red, Green, Blue }

        class Card
        {
            public RectTransform rt; public float y = 70; public const float Pad = 60;
            // card width: 940 in the code-drawn look; the artist's panels are used at their own size (818, like the mockups)
            public static float W => SK ? SkinPop.CardW : 940;
            public static float Inner => SK ? SkinPop.CardInner : 820;
            public GameController gc; public bool skin, circle; bool titled;   // circle: the panel art has a round icon spot (revive, lose, booster)
            /// <summary>Reserve a full-width row of height h (after a gap) and return it. w &lt; 0 = full inner width.</summary>
            public RectTransform Row(float h, float gap = 38, float w = -1)
            {
                if (w < 0) w = Inner;
                y += gap; var r = UIKit.Rect("Row", rt); UIKit.TopLeft(r, (W - w) / 2, y, w, h); y += h; return r;
            }
            public void Pill(string text, PillStyle style = PillStyle.Gold)
            {
                if (skin) return;   // new art: no pill above the title
                var (bg, fg) = style == PillStyle.Red ? ("#f8d3da", "#8a2236") : style == PillStyle.Green ? ("#d7efc9", "#2c6b36") : style == PillStyle.Blue ? ("#d6e4fb", "#24478d") : ("#f0dcae", "#6a4a20");
                var t = UIKit.Txt("PillText", rt, text.ToUpper(), 33, UIKit.Hex(fg)); float w = t.preferredWidth + 80;
                var row = Row(64, y <= 70 ? 0 : 30, w); var im = UIKit.Img("Pill", row, UIKit.Rounded, UIKit.Hex(bg)); UIKit.Stretch(im.rectTransform); GameController.Pill(im, 64);
                t.transform.SetParent(row, false); UIKit.Stretch(t.rectTransform);
            }
            public Text Title(string text, int lines = 1)
            {
                if (skin && !titled) { titled = true; var st = UIKit.Display(UIKit.Txt("Title", rt, text.Replace("\n", " "), 60, Color.white)); gc.SkinTitle(this, st); return st; }   // on the ribbon
                var t = UIKit.Display(UIKit.Txt("Title", Row(98 * lines, 26), text, 86, UIKit.Ink)); UIKit.Stretch(t.rectTransform); t.lineSpacing = .9f; return t;
            }
            /// <summary>Round icon well (366 px). fill draws inside it.</summary>
            public RectTransform BigIcon(System.Action<RectTransform> fill)
            {
                if (skin && circle)
                {   // the icon sits in the panel art's own circle; the next row starts under the circle
                    var spot = UIKit.Rect("Icon", rt); UIKit.TopLeft(spot, (W - SkinPop.IconSize) / 2, SkinPop.CircleY - SkinPop.IconSize / 2, SkinPop.IconSize, SkinPop.IconSize);
                    y = Mathf.Max(y, SkinPop.CircleBottom); fill?.Invoke(spot); return spot;
                }
                var row = Row(366, 38, 366);
                if (skin) { var w = UIKit.Img("Well", row, UIKit.Circle, SkinPop.Well); UIKit.Stretch(w.rectTransform); var ii = UIKit.Rect("Icon", row); UIKit.Stretch(ii); fill?.Invoke(ii); return ii; }
                var shade = UIKit.Img("Well", row, UIKit.Circle, UIKit.Hex("#e2cfa6")); UIKit.Stretch(shade.rectTransform);
                var well = UIKit.Img("WellTop", row, UIKit.Circle, UIKit.Hex("#efe2c6")); UIKit.Stretch(well.rectTransform); well.rectTransform.offsetMin = new Vector2(0, 16);
                var inner = UIKit.Rect("Icon", row); UIKit.Stretch(inner); fill?.Invoke(inner); return inner;
            }
            public Text Lead(string text, int size = 50)
            {
                var t = UIKit.Txt("Lead", rt, text, size, skin ? SkinPop.Lead : CardText); t.rectTransform.sizeDelta = new Vector2(Inner, 10); if (skin) { t.fontSize = SkinPop.LeadSize; size = SkinPop.LeadSize; }
                float h = Mathf.Max(size * 1.3f, t.preferredHeight + 6); t.transform.SetParent(Row(h, skin ? SkinPop.LeadGap : 36), false); UIKit.Stretch(t.rectTransform); return t;   // skin: more air under the big icon
            }
            public void Note(string text, Color c) { var t = UIKit.Txt("Note", Row(46, 26), text, 39, c); UIKit.Stretch(t.rectTransform); }
            /// <summary>[USE COINS price] [WATCH AD Free]. Not enough coins: coin button greyed + red line + ad glows. adAct null = coins only.</summary>
            public Button Choice(int price, System.Action coinAct, System.Action adAct, string coinWord = null)
            {
                bool can = Coins >= price;
                if (!can) Note("Not enough coins", UIKit.Hex("#a3283f"));
                var row = skin ? Row(SkinPop.ChoiceH, can ? 48 : 22, W - 2 * SkinPop.ChoiceSide) : Row(210, can ? 40 : 22);
                float bw = adAct != null ? (row.rect.width - 33) / 2 : row.rect.width, x0 = 0, gap = 33;
                if (skin && adAct != null) { gap = SkinPop.ChoiceGap; bw = (row.rect.width - gap) / 2; }   // mockup: two wide buttons with a gap
                var coinBtn = gc.ChoiceButton(row, x0, bw, coinWord ?? "USE COINS", price.ToString(), UIKit.IconCoin, can ? UIKit.Hex("#f6c94f") : UIKit.Hex("#d9d0bf"), can ? UIKit.Hex("#b47b1f") : UIKit.Hex("#b3a891"),
                    can ? UIKit.Hex("#4a2e0c") : UIKit.Hex("#5f5649"), can ? coinAct : null);
                if (can) coinBtn.gameObject.AddComponent<Breath>();
                if (adAct != null)
                {
                    var ad = gc.ChoiceButton(row, x0 + bw + gap, bw, "WATCH AD", "Free", UIKit.IconAd, UIKit.Hex("#2f8a3d"), UIKit.Hex("#1d5e28"), Color.white, adAct);
                    if (!can)
                    {   // glow ring behind the ad button + breath
                        var glow = UIKit.Img("Glow", ad.transform, UIKit.Rounded, new Color(1f, .83f, .31f, .85f)); glow.transform.SetAsFirstSibling();
                        glow.rectTransform.anchorMin = Vector2.zero; glow.rectTransform.anchorMax = Vector2.one; glow.rectTransform.offsetMin = new Vector2(-14, -26); glow.rectTransform.offsetMax = new Vector2(14, 14);
                        ad.gameObject.AddComponent<Breath>();
                    }
                }
                return coinBtn;
            }
            public void Progress(int pct)
            {
                var head = Row(44, skin && circle && y <= SkinPop.CircleBottom ? SkinPop.LeadGap : 36);   // first row under the art circle gets the same air as the text
                var l = UIKit.Txt("L", head, "Picture", 36, skin ? SkinPop.Soft : CardSoft, TextAnchor.MiddleLeft); UIKit.Stretch(l.rectTransform);
                var r = UIKit.Txt("R", head, $"{pct}% knitted", 36, skin ? SkinPop.Soft : CardSoft, TextAnchor.MiddleRight); UIKit.Stretch(r.rectTransform);
                if (skin) { gc.SkinProgress(Row(46, 16), pct); return; }
                var bar = Row(34, 16); var bgI = UIKit.Img("Bar", bar, UIKit.Rounded, UIKit.Hex("#e2d4b6")); UIKit.Stretch(bgI.rectTransform); GameController.Pill(bgI, 34);
                if (pct > 0)
                {
                    var fill = UIKit.Img("Fill", bar, UIKit.Rounded, UIKit.Hex("#4fae5b")); GameController.Pill(fill, 34);
                    fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(pct / 100f), 1); fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                }
            }
            public Button BigButton(string label, System.Action act, bool green = false, float h = 166, float w = -1)
            {
                if (w < 0) w = Inner;
                if (skin) return gc.SkinFlatButton(Row(SkinPop.BtnH, 40, w), label, "btn-green", 58, act);   // new art: the main button is green
                var row = Row(h, 40, w);
                return gc.FlatButton(row, label, green ? UIKit.Hex("#2f8a3d") : UIKit.Hex("#f6c94f"), green ? UIKit.Hex("#1d5e28") : UIKit.Hex("#b47b1f"), green ? Color.white : UIKit.Hex("#4a2e0c"), 64, act);
            }
            public Button Link(string label, System.Action act)
            {
                var row = Row(110, 12); var t = UIKit.Txt("Link", row, label, 42, skin ? SkinPop.Soft : CardSoft); UIKit.Stretch(t.rectTransform); t.raycastTarget = true;
                var line = UIKit.Img("Underline", row, null, CardSoft); line.gameObject.SetActive(!skin);   // new art: no underline (user 25/10)
                var lrt = line.rectTransform; lrt.anchorMin = lrt.anchorMax = new Vector2(.5f, .5f); lrt.sizeDelta = new Vector2(t.preferredWidth, 3); lrt.anchoredPosition = new Vector2(0, -26);
                var b = row.gameObject.AddComponent<Button>(); b.targetGraphic = t; b.onClick.AddListener(() => act()); return b;
            }
        }

        // ---------- open / close ----------
        void CloseModal() { foreach (Transform c in modalRT) Destroy(c.gameObject); modalRT.gameObject.SetActive(false); }
        /// <summary>Build a card. onClose != null adds the red X (and tapping the dim closes it too).</summary>
        Card OpenCard(System.Action<Card> build, System.Action onClose = null, float dim = .7f, string art = null)
        {
            CloseModal(); modalRT.gameObject.SetActive(true);
            var dimI = UIKit.Img("Dim", modalRT, null, new Color(.055f, .078f, .17f, dim), true); UIKit.Stretch(dimI.rectTransform);
            if (onClose != null) dimI.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());
            var ring = UIKit.Img("CardRing", modalRT, UIKit.Rounded, CardRing); var rrt = ring.rectTransform; rrt.anchorMin = rrt.anchorMax = new Vector2(.5f, .5f);
            var card = new Card { gc = this }; var im = UIKit.Img("Card", rrt, UIKit.Rounded, CardBg); card.rt = im.rectTransform;
            card.rt.anchorMin = card.rt.anchorMax = new Vector2(.5f, 1); card.rt.pivot = new Vector2(.5f, 1); card.rt.anchoredPosition = new Vector2(0, -17);
            im.raycastTarget = true;   // taps on the card do not reach the dim
            float ppu = 1; Sprite panel = SK ? SkinPanel(art ?? SkinPop.Panel, out ppu) : null;
            if (panel != null) { card.skin = true; card.circle = SkinPop.HasCircle(art); card.y = art == SkinPop.Win ? SkinPop.WinTop : SkinPop.Top; }
            build(card);
            float h = card.y + (card.skin ? SkinPop.Bottom : 62);
            if (card.circle) h = Mathf.Max(h, SkinPop.CircleSlice + 80);   // never shorter than the fixed top of the art (ribbon + circle)
            card.rt.sizeDelta = new Vector2(Card.W, h); rrt.sizeDelta = new Vector2(Card.W + 34, h + 34);
            ring.pixelsPerUnitMultiplier = .45f; im.pixelsPerUnitMultiplier = .5f;   // big round corners
            if (card.skin) { im.sprite = panel; im.type = Image.Type.Sliced; im.color = Color.white; im.pixelsPerUnitMultiplier = ppu; ring.color = Color.clear; }
            // centre in the space below the coin HUD
            float rootH = root.rect.height, top = Lay.TopY + Lay.Btn + 60, avail = rootH - top - 60;
            rrt.anchoredPosition = new Vector2(0, rootH / 2 - top - Mathf.Max(0, (avail - rrt.sizeDelta.y) / 2) - rrt.sizeDelta.y / 2);
            if (onClose != null && card.skin) SkinClose(card.rt, onClose);
            else if (onClose != null)
            {
                var x = UIKit.Img("Close", card.rt, UIKit.Rounded, UIKit.Hex("#d9405a"), true); var xrt = x.rectTransform;
                xrt.anchorMin = xrt.anchorMax = new Vector2(1, 1); xrt.sizeDelta = new Vector2(133, 133); xrt.anchoredPosition = new Vector2(-10, -10);
                x.gameObject.AddComponent<Shadow>().effectColor = UIKit.Hex("#9d2238"); x.GetComponent<Shadow>().effectDistance = new Vector2(0, -11);
                var ic = UIKit.Img("X", xrt, UIKit.IconCross, Color.white); ic.rectTransform.sizeDelta = new Vector2(70, 70);
                x.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());
            }
            StartCoroutine(CardIn(rrt, dimI));
            return card;
        }
        IEnumerator CardIn(RectTransform rt, Image dim)
        {
            float a = dim.color.a; var home = rt.anchoredPosition;
            for (float t = 0; t < .36f; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break; float p = t / .36f;
                rt.localScale = Vector3.one * KF.At(p, 0, .85f, .6f, 1.03f, 1, 1); rt.anchoredPosition = home + new Vector2(0, -50 * (1 - KF.EaseOut(p)));
                if (dim != null) dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, a * Mathf.Min(1, p * 1.8f)); yield return null;
            }
            // always land on the final state (on a slow frame the loop can end after one step)
            if (rt != null) { rt.localScale = Vector3.one; rt.anchoredPosition = home; }
            if (dim != null) dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, a);
        }

        // ---------- buttons ----------
        /// <summary>Flat colored button with a darker "base" below (3D look of the design).</summary>
        Button FlatButton(RectTransform parent, string label, Color bg, Color shade, Color fg, int size, System.Action act)
        {
            var art = SK ? SkinBtnPath(bg) : null; if (art != null) return SkinFlatButton(parent, label, art, size, act);
            var baseI = UIKit.Img("Base", parent, UIKit.Rounded, shade); UIKit.Stretch(baseI.rectTransform);
            var im = UIKit.Img("Face", baseI.rectTransform, UIKit.Rounded, bg, true); UIKit.Stretch(im.rectTransform); im.rectTransform.offsetMin = new Vector2(0, 14);
            if (!string.IsNullOrEmpty(label)) { var t = UIKit.Display(UIKit.Txt("Label", im.transform, label, size, fg)); UIKit.Stretch(t.rectTransform, 10); }
            var b = baseI.gameObject.AddComponent<Button>(); b.targetGraphic = im; if (act != null) b.onClick.AddListener(() => act()); else b.interactable = false;
            var cb = b.colors; cb.disabledColor = Color.white; b.colors = cb;
            return b;
        }
        Button ChoiceButton(RectTransform row, float x, float w, string small, string big, Sprite icon, Color bg, Color shade, Color fg, System.Action act)
        {
            var holder = UIKit.Rect("Choice_" + small, row); holder.anchorMin = new Vector2(0, 0); holder.anchorMax = new Vector2(0, 1); holder.pivot = new Vector2(0, .5f);
            holder.sizeDelta = new Vector2(w, 0); holder.anchoredPosition = new Vector2(x, 0);
            if (SK) return SkinChoiceButton(holder, small == "WATCH AD", big, act, small == "WATCH AD" || small == "USE COINS" ? null : small);
            var b = FlatButton(holder, "", bg, shade, fg, 30, act); var face = (RectTransform)b.transform.GetChild(0);
            var s = UIKit.Txt("Small", face, small, 33, fg); var srt = s.rectTransform; srt.anchorMin = new Vector2(0, .62f); srt.anchorMax = new Vector2(1, .95f); srt.offsetMin = srt.offsetMax = Vector2.zero;
            var t = UIKit.Display(UIKit.Txt("Big", face, big, 70, fg)); var trt = t.rectTransform; trt.anchorMin = new Vector2(0, .05f); trt.anchorMax = new Vector2(1, .62f); trt.offsetMin = new Vector2(60, 0); trt.offsetMax = Vector2.zero;
            float tw = t.preferredWidth + 70; var ic = UIKit.Img("Icon", face, icon, act == null ? new Color(.85f, .82f, .76f) : Color.white); var irt = ic.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(.5f, .33f); irt.sizeDelta = new Vector2(60, 60); irt.anchoredPosition = new Vector2(-tw / 2 + 30, 0);
            trt.offsetMin = new Vector2(70, 0);
            return b;
        }
    }
}
