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
            public RectTransform rt; public float y = 70; public const float W = 940, Pad = 60, Inner = W - 2 * Pad;
            public GameController gc;
            /// <summary>Reserve a full-width row of height h (after a gap) and return it.</summary>
            public RectTransform Row(float h, float gap = 38, float w = Inner)
            {
                y += gap; var r = UIKit.Rect("Row", rt); UIKit.TopLeft(r, (W - w) / 2, y, w, h); y += h; return r;
            }
            public void Pill(string text, PillStyle style = PillStyle.Gold)
            {
                var (bg, fg) = style == PillStyle.Red ? ("#f8d3da", "#8a2236") : style == PillStyle.Green ? ("#d7efc9", "#2c6b36") : style == PillStyle.Blue ? ("#d6e4fb", "#24478d") : ("#f0dcae", "#6a4a20");
                var t = UIKit.Txt("PillText", rt, text.ToUpper(), 33, UIKit.Hex(fg)); float w = t.preferredWidth + 80;
                var row = Row(64, y <= 70 ? 0 : 30, w); var im = UIKit.Img("Pill", row, UIKit.Rounded, UIKit.Hex(bg)); UIKit.Stretch(im.rectTransform); GameController.Pill(im, 64);
                t.transform.SetParent(row, false); UIKit.Stretch(t.rectTransform);
            }
            public Text Title(string text, int lines = 1)
            {
                var t = UIKit.Txt("Title", Row(98 * lines, 26), text, 86, UIKit.Ink); UIKit.Stretch(t.rectTransform); t.lineSpacing = .9f; return t;
            }
            /// <summary>Round icon well (366 px). fill draws inside it.</summary>
            public RectTransform BigIcon(System.Action<RectTransform> fill)
            {
                var row = Row(366, 38, 366);
                var shade = UIKit.Img("Well", row, UIKit.Circle, UIKit.Hex("#e2cfa6")); UIKit.Stretch(shade.rectTransform);
                var well = UIKit.Img("WellTop", row, UIKit.Circle, UIKit.Hex("#efe2c6")); UIKit.Stretch(well.rectTransform); well.rectTransform.offsetMin = new Vector2(0, 16);
                var inner = UIKit.Rect("Icon", row); UIKit.Stretch(inner); fill?.Invoke(inner); return inner;
            }
            public Text Lead(string text, int size = 50)
            {
                var t = UIKit.Txt("Lead", rt, text, size, CardText); t.rectTransform.sizeDelta = new Vector2(Inner, 10);
                float h = Mathf.Max(size * 1.3f, t.preferredHeight + 6); t.transform.SetParent(Row(h, 36), false); UIKit.Stretch(t.rectTransform); return t;
            }
            public void Note(string text, Color c) { var t = UIKit.Txt("Note", Row(46, 26), text, 39, c); UIKit.Stretch(t.rectTransform); }
            /// <summary>[USE COINS price] [WATCH AD Free]. Not enough coins: coin button greyed + red line + ad glows. adAct null = coins only.</summary>
            public Button Choice(int price, System.Action coinAct, System.Action adAct)
            {
                bool can = Coins >= price;
                if (!can) Note("Not enough coins", UIKit.Hex("#a3283f"));
                var row = Row(210, can ? 40 : 22); float bw = adAct != null ? (Inner - 33) / 2 : Inner;
                var coinBtn = gc.ChoiceButton(row, 0, bw, "USE COINS", price.ToString(), UIKit.IconCoin, can ? UIKit.Hex("#f6c94f") : UIKit.Hex("#d9d0bf"), can ? UIKit.Hex("#b47b1f") : UIKit.Hex("#b3a891"),
                    can ? UIKit.Hex("#4a2e0c") : UIKit.Hex("#5f5649"), can ? coinAct : null);
                if (can) coinBtn.gameObject.AddComponent<Breath>();
                if (adAct != null)
                {
                    var ad = gc.ChoiceButton(row, bw + 33, bw, "WATCH AD", "Free", UIKit.IconAd, UIKit.Hex("#2f8a3d"), UIKit.Hex("#1d5e28"), Color.white, adAct);
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
                var head = Row(44, 36);
                var l = UIKit.Txt("L", head, "Picture", 36, CardSoft, TextAnchor.MiddleLeft); UIKit.Stretch(l.rectTransform);
                var r = UIKit.Txt("R", head, $"{pct}% knitted", 36, CardSoft, TextAnchor.MiddleRight); UIKit.Stretch(r.rectTransform);
                var bar = Row(34, 16); var bgI = UIKit.Img("Bar", bar, UIKit.Rounded, UIKit.Hex("#e2d4b6")); UIKit.Stretch(bgI.rectTransform); GameController.Pill(bgI, 34);
                if (pct > 0)
                {
                    var fill = UIKit.Img("Fill", bar, UIKit.Rounded, UIKit.Hex("#4fae5b")); GameController.Pill(fill, 34);
                    fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(pct / 100f), 1); fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                }
            }
            public Button BigButton(string label, System.Action act, bool green = false, float h = 166)
            {
                var row = Row(h, 40);
                return gc.FlatButton(row, label, green ? UIKit.Hex("#2f8a3d") : UIKit.Hex("#f6c94f"), green ? UIKit.Hex("#1d5e28") : UIKit.Hex("#b47b1f"), green ? Color.white : UIKit.Hex("#4a2e0c"), 64, act);
            }
            public Button Link(string label, System.Action act)
            {
                var row = Row(110, 12); var t = UIKit.Txt("Link", row, label, 42, CardSoft); UIKit.Stretch(t.rectTransform); t.raycastTarget = true;
                var line = UIKit.Img("Underline", row, null, CardSoft); var lrt = line.rectTransform; lrt.anchorMin = lrt.anchorMax = new Vector2(.5f, .5f); lrt.sizeDelta = new Vector2(t.preferredWidth, 3); lrt.anchoredPosition = new Vector2(0, -26);
                var b = row.gameObject.AddComponent<Button>(); b.targetGraphic = t; b.onClick.AddListener(() => act()); return b;
            }
        }

        // ---------- open / close ----------
        void CloseModal() { foreach (Transform c in modalRT) Destroy(c.gameObject); modalRT.gameObject.SetActive(false); }
        /// <summary>Build a card. onClose != null adds the red X (and tapping the dim closes it too).</summary>
        Card OpenCard(System.Action<Card> build, System.Action onClose = null, float dim = .7f)
        {
            CloseModal(); modalRT.gameObject.SetActive(true);
            var dimI = UIKit.Img("Dim", modalRT, null, new Color(.055f, .078f, .17f, dim), true); UIKit.Stretch(dimI.rectTransform);
            if (onClose != null) dimI.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());
            var ring = UIKit.Img("CardRing", modalRT, UIKit.Rounded, CardRing); var rrt = ring.rectTransform; rrt.anchorMin = rrt.anchorMax = new Vector2(.5f, .5f);
            var card = new Card { gc = this }; var im = UIKit.Img("Card", rrt, UIKit.Rounded, CardBg); card.rt = im.rectTransform;
            card.rt.anchorMin = card.rt.anchorMax = new Vector2(.5f, 1); card.rt.pivot = new Vector2(.5f, 1); card.rt.anchoredPosition = new Vector2(0, -17);
            im.raycastTarget = true;   // taps on the card do not reach the dim
            build(card);
            float h = card.y + 62; card.rt.sizeDelta = new Vector2(Card.W, h); rrt.sizeDelta = new Vector2(Card.W + 34, h + 34);
            ring.pixelsPerUnitMultiplier = .45f; im.pixelsPerUnitMultiplier = .5f;   // big round corners
            // centre in the space below the coin HUD
            float rootH = root.rect.height, top = Lay.TopY + Lay.Btn + 60, avail = rootH - top - 60;
            rrt.anchoredPosition = new Vector2(0, rootH / 2 - top - Mathf.Max(0, (avail - rrt.sizeDelta.y) / 2) - rrt.sizeDelta.y / 2);
            if (onClose != null)
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
            var baseI = UIKit.Img("Base", parent, UIKit.Rounded, shade); UIKit.Stretch(baseI.rectTransform);
            var im = UIKit.Img("Face", baseI.rectTransform, UIKit.Rounded, bg, true); UIKit.Stretch(im.rectTransform); im.rectTransform.offsetMin = new Vector2(0, 14);
            if (!string.IsNullOrEmpty(label)) { var t = UIKit.Txt("Label", im.transform, label, size, fg); UIKit.Stretch(t.rectTransform, 10); }
            var b = baseI.gameObject.AddComponent<Button>(); b.targetGraphic = im; if (act != null) b.onClick.AddListener(() => act()); else b.interactable = false;
            var cb = b.colors; cb.disabledColor = Color.white; b.colors = cb;
            return b;
        }
        Button ChoiceButton(RectTransform row, float x, float w, string small, string big, Sprite icon, Color bg, Color shade, Color fg, System.Action act)
        {
            var holder = UIKit.Rect("Choice_" + small, row); holder.anchorMin = new Vector2(0, 0); holder.anchorMax = new Vector2(0, 1); holder.pivot = new Vector2(0, .5f);
            holder.sizeDelta = new Vector2(w, 0); holder.anchoredPosition = new Vector2(x, 0);
            var b = FlatButton(holder, "", bg, shade, fg, 30, act); var face = (RectTransform)b.transform.GetChild(0);
            var s = UIKit.Txt("Small", face, small, 33, fg); var srt = s.rectTransform; srt.anchorMin = new Vector2(0, .62f); srt.anchorMax = new Vector2(1, .95f); srt.offsetMin = srt.offsetMax = Vector2.zero;
            var t = UIKit.Txt("Big", face, big, 70, fg); var trt = t.rectTransform; trt.anchorMin = new Vector2(0, .05f); trt.anchorMax = new Vector2(1, .62f); trt.offsetMin = new Vector2(60, 0); trt.offsetMax = Vector2.zero;
            float tw = t.preferredWidth + 70; var ic = UIKit.Img("Icon", face, icon, act == null ? new Color(.85f, .82f, .76f) : Color.white); var irt = ic.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(.5f, .33f); irt.sizeDelta = new Vector2(60, 60); irt.anchoredPosition = new Vector2(-tw / 2 + 30, 0);
            trt.offsetMin = new Vector2(70, 0);
            return b;
        }
    }
}
