// GameController – Builds the in-game screen in code (approved layout, design canvas 20/10):
//   [Settings][Coins] ............ [Retry]
//              Level N
//   [picture]  BASKET · NEXT   N LEFT
//              [basket][next][next]
//   SHELF 0/6 ............ [yarn bag chip]
//   [ 8 shelf slots, the last 2 locked ]
//   [ board ]
//   [Undo] [Hint] [Extra Shelf]
// FIXED LAYOUT RULE: nothing on this screen changes size or position during a level.
// All numbers are in reference pixels (1080 × 2340); Lay holds them so they are easy to tweak in one place.
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
        static class Lay
        {
            public const float Side = 44, TopY = 110, Btn = 133;
            public const float CoinX = 199, CoinW = 321, CoinH = 105;
            public const float TitleY = 266, TitleH = 92;
            public const float PicY = 379, Pic = 310, BasketsX = 387, BasketsW = 649;
            public const float ShelfHeadY = 717, ShelfHeadH = 94, ShelfY = 833, ShelfH = 144, SlotSize = 94;
            public const float BoardTop = 1011, BoostersH = 172, BoostersBottom = 50, BoostersGap = 28, BoardBottom = BoostersBottom + BoostersH + 39;
            public const float Width = 1080 - 2 * Side;
        }
        static readonly Color Sky = UIKit.Hex("#aab6d8"), Pale = UIKit.Hex("#f7eedc"), PaleShadow = UIKit.Hex("#cdb791");

        // ---------- UI construction ----------
        void BuildUI()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
            var cgo = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // full screen (screenRT) + the centred 1080-wide game column (root); scale and notch: GameController.ScreenFit.cs
            root = BuildScreen(cgo);
            var bg = UIKit.Img("Background", screenRT, null, UIKit.Hex("#2c3b66")); UIKit.Stretch(bg.rectTransform); bg.transform.SetAsFirstSibling();

            // ---- top bar: [Settings][coin spot] ... [Retry] ----
            topRow = UIKit.Rect("TopRow", root); UIKit.TopLeft(topRow, 0, Lay.TopY, 1080, Lay.Btn);
            btnSettings = IconButton("Settings", topRow, UIKit.IconGear, Lay.Side, () => { if (coach == null) OpenSettings(); });
            btnRetry = IconButton("Retry", topRow, UIKit.IconRetry, 1080 - Lay.Side - Lay.Btn, () => { if (coach == null) AskRestart(); });
            title = UIKit.Display(UIKit.Txt("Title", root, "Level 1", 83, Pale)); UIKit.TopLeft(title.rectTransform, 140, Lay.TitleY, 800, Lay.TitleH);
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, .25f);

            // ---- picture ----
            var frame = UIKit.Img("PictureFrame", root, UIKit.Rounded, Pale); picFrame = frame.rectTransform; UIKit.TopLeft(picFrame, Lay.Side, Lay.PicY, Lay.Pic, Lay.Pic);
            frame.gameObject.AddComponent<Shadow>().effectColor = PaleShadow; frame.GetComponent<Shadow>().effectDistance = new Vector2(0, -11);
            picTex = PictureRenderer.NewTexture();
            picImg = UIKit.Rect("Picture", frame.transform).gameObject.AddComponent<RawImage>(); picImg.texture = picTex; picImg.raycastTarget = false;
            UIKit.Stretch(picImg.rectTransform, 22);
            var kbg = UIKit.Img("KnittedBg", frame.transform, UIKit.Rounded, UIKit.Hex("#3d2f28")); Pill(kbg, 50);
            kbg.rectTransform.anchorMin = kbg.rectTransform.anchorMax = new Vector2(.5f, 0); kbg.rectTransform.sizeDelta = new Vector2(220, 50); kbg.rectTransform.anchoredPosition = new Vector2(0, -16);
            knittedText = UIKit.Txt("Knitted", kbg.transform, "0% knitted", 28, Pale); UIKit.Stretch(knittedText.rectTransform);

            // ---- baskets ----
            basketsHead = UIKit.Rect("BasketsHead", root); UIKit.TopLeft(basketsHead, Lay.BasketsX, Lay.PicY, Lay.BasketsW, 44);
            var bl = UIKit.Txt("BasketsLabel", basketsHead, "BASKET  ·  NEXT", 30, Sky, TextAnchor.MiddleLeft); UIKit.Stretch(bl.rectTransform);
            basketsLeft = UIKit.Txt("BasketsLeft", basketsHead, "", 30, Sky, TextAnchor.MiddleRight); UIKit.Stretch(basketsLeft.rectTransform);
            basketRow = UIKit.Rect("Baskets", root); UIKit.TopLeft(basketRow, Lay.BasketsX, Lay.PicY + 50, Lay.BasketsW, Lay.Pic - 50);

            // ---- shelf header (fixed height): "SHELF 0/6" + yarn bag chip on the right ----
            shelfHead = UIKit.Rect("ShelfHead", root); UIKit.TopLeft(shelfHead, Lay.Side, Lay.ShelfHeadY, Lay.Width, Lay.ShelfHeadH);
            shelfCount = UIKit.Txt("ShelfCount", shelfHead, "SHELF 0/6", 30, Sky, TextAnchor.MiddleLeft); UIKit.Stretch(shelfCount.rectTransform, 6);
            BuildBagChip();

            // ---- shelf: always 8 slots (the last 2 locked until Extra Shelf) ----
            var shelf = UIKit.Img("Shelf", root, UIKit.Rounded, UIKit.Hex("#9b6b45")); shelfRow = shelf.rectTransform; UIKit.TopLeft(shelfRow, Lay.Side, Lay.ShelfY, Lay.Width, Lay.ShelfH);
            int nSlots = Cfg.shelfSlots + Cfg.extraShelfSlots; float colW = (Lay.Width - 66) / nSlots;
            for (int i = 0; i < nSlots; i++)
            {
                var im = UIKit.Img("Slot" + i, shelfRow, UIKit.Circle, UIKit.Hex("#5c381f")); var rt = im.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, .5f); rt.sizeDelta = new Vector2(Lay.SlotSize, Lay.SlotSize); rt.anchoredPosition = new Vector2(33 + colW * (i + .5f), 0);
                var lk = UIKit.Img("Lock", rt, UIKit.IconLock, UIKit.Hex("#c9a77f")); lk.rectTransform.sizeDelta = new Vector2(46, 46);
                slots.Add(im); slotLocks.Add(lk);
                AddLockPlus(lk);   // "+" instead of the padlock, tap = Extra Shelf (GameController.Feedback.cs)
            }

            // ---- board ----
            var bp = UIKit.Img("BoardPanel", root, UIKit.Rounded, Pale);
            boardPanel = bp.rectTransform; boardPanel.anchorMin = new Vector2(0, 0); boardPanel.anchorMax = new Vector2(1, 1);
            boardPanel.offsetMin = new Vector2(Lay.Side, Lay.BoardBottom); boardPanel.offsetMax = new Vector2(-Lay.Side, -Lay.BoardTop);
            bp.gameObject.AddComponent<Shadow>().effectColor = PaleShadow; bp.GetComponent<Shadow>().effectDistance = new Vector2(0, -11);
            board = UIKit.Rect("Board", boardPanel); board.anchorMin = board.anchorMax = new Vector2(.5f, .5f);
            var chip = UIKit.Img("RollChip", boardPanel, UIKit.Rounded, UIKit.Hex("#fffaf0")); Pill(chip, 64);
            chip.rectTransform.anchorMin = chip.rectTransform.anchorMax = new Vector2(1, 1); chip.rectTransform.pivot = new Vector2(1, .5f);
            chip.rectTransform.anchoredPosition = new Vector2(-30, 0); chip.rectTransform.sizeDelta = new Vector2(300, 64);
            rollChip = UIKit.Txt("Text", chip.transform, "", 30, UIKit.Ink); UIKit.Stretch(rollChip.rectTransform);

            // toast
            var toast = UIKit.Img("Toast", boardPanel, UIKit.Rounded, UIKit.Hex("#fbe1e4"));
            toastRT = toast.rectTransform; toastRT.anchorMin = toastRT.anchorMax = new Vector2(.5f, 1); toastRT.pivot = new Vector2(.5f, 1);
            toastRT.anchoredPosition = new Vector2(0, -24); toastRT.sizeDelta = new Vector2(900, 120);
            toastText = UIKit.Txt("Text", toastRT, "", 34, UIKit.Hex("#9b2238")); UIKit.Stretch(toastText.rectTransform, 18);
            toastRT.gameObject.AddComponent<CanvasGroup>(); toastRT.gameObject.SetActive(false);

            // ---- boosters ----
            System.Action[] acts = { UseUndo, UseHint, UseShelf };
            float bw = (Lay.Width - 2 * Lay.BoostersGap) / 3;
            for (int i = 0; i < 3; i++)
            {
                string key = BKeys[i];
                var b = UIKit.Btn(BName[key], root, BName[key], Pale, UIKit.Hex("#6b5444"), 33, acts[i]);
                var rt = (RectTransform)b.transform; rt.anchorMin = rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(.5f, .5f);
                rt.sizeDelta = new Vector2(bw, Lay.BoostersH); rt.anchoredPosition = new Vector2(Lay.Side + bw / 2 + i * (bw + Lay.BoostersGap), Lay.BoostersBottom + Lay.BoostersH / 2);
                b.gameObject.AddComponent<Shadow>().effectColor = PaleShadow; b.GetComponent<Shadow>().effectDistance = new Vector2(0, -11);
                var lab = b.GetComponentInChildren<Text>(); lab.alignment = TextAnchor.LowerCenter; lab.rectTransform.offsetMin = new Vector2(0, 22);
                var icon = BoosterIcon(key, rt); icon.anchoredPosition = new Vector2(0, 22); icon.sizeDelta = new Vector2(78, 78);
                var badge = UIKit.Img("Badge", rt, UIKit.Rounded, UIKit.Hex("#f08a3c")); Pill(badge, 66);
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(1, 1); badge.rectTransform.sizeDelta = new Vector2(70, 66); badge.rectTransform.anchoredPosition = new Vector2(-6, -8);
                var ct = UIKit.Display(UIKit.Txt("Count", badge.transform, "0", 36, Color.white)); UIKit.Stretch(ct.rectTransform);
                bButtons[key] = b; bLabels[key] = lab; bCounts[key] = ct; bBadges[key] = badge;
            }

            fx = UIKit.Rect("FX", root); UIKit.Stretch(fx);
            BuildCoach();
            modalRT = UIKit.Rect("Modal", screenRT); UIKit.Stretch(modalRT); modalRT.gameObject.SetActive(false);   // popups + dim cover the whole screen
            var mc = modalRT.gameObject.AddComponent<Canvas>(); mc.overrideSorting = true; mc.sortingOrder = 80; modalRT.gameObject.AddComponent<GraphicRaycaster>();

            // ---- coin HUD: above the game AND above every popup ----
            hudLayer = UIKit.Rect("HudLayer", root); UIKit.Stretch(hudLayer);
            var hc = hudLayer.gameObject.AddComponent<Canvas>(); hc.overrideSorting = true; hc.sortingOrder = 90;
            BuildCoinHud();
            // rewarded ad stand-in: its own full-screen layer, so the popup under it stays as it is
            adLayer = UIKit.Rect("AdLayer", screenRT); UIKit.Stretch(adLayer); adLayer.gameObject.SetActive(false);
            var ac = adLayer.gameObject.AddComponent<Canvas>(); ac.overrideSorting = true; ac.sortingOrder = 95; adLayer.gameObject.AddComponent<GraphicRaycaster>();
            SkinUI();
            ApplyScreenFit();   // scale, notch and home bar before the first board is laid out (after SkinUI: it sizes the orange bar)   // the artist's art over the code-drawn screen (GameController.Skin.cs); does nothing without the art
        }

        // ---------- pieces ----------
        Button IconButton(string name, RectTransform parent, Sprite icon, float x, System.Action act)
        {
            var im = UIKit.Img(name, parent, UIKit.Rounded, Pale, true); var rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, .5f); rt.pivot = new Vector2(0, .5f); rt.sizeDelta = new Vector2(Lay.Btn, Lay.Btn); rt.anchoredPosition = new Vector2(x, 0);
            im.gameObject.AddComponent<Shadow>().effectColor = PaleShadow; im.GetComponent<Shadow>().effectDistance = new Vector2(0, -11);
            var ic = UIKit.Img("Icon", rt, icon, UIKit.Hex("#3d2f28")); ic.rectTransform.sizeDelta = new Vector2(70, 70);
            var b = im.gameObject.AddComponent<Button>().WithHaptic(); b.onClick.AddListener(() => act()); return b;
        }
        /// <summary>Make a Rounded image fully round at its current height (pill / capsule).</summary>
        static void Pill(Image im, float h)
        {
            im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 64f / Mathf.Max(20, h);
        }
        void BuildCoinHud()
        {
            var ring = UIKit.Img("CoinHud", hudLayer, UIKit.Rounded, UIKit.Hex("#e7aa34")); coinHud = ring.rectTransform;
            UIKit.TopLeft(coinHud, Lay.CoinX, Lay.TopY + (Lay.Btn - Lay.CoinH) / 2, Lay.CoinW, Lay.CoinH); Pill(ring, Lay.CoinH);
            var inner = UIKit.Img("Inner", coinHud, UIKit.Rounded, UIKit.Hex("#1f2b4f")); UIKit.Stretch(inner.rectTransform, 6); Pill(inner, Lay.CoinH - 12);
            var coin = UIKit.Img("Coin", coinHud, UIKit.IconCoin, Color.white); var crt = coin.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0, .5f); crt.sizeDelta = new Vector2(72, 72); crt.anchoredPosition = new Vector2(16 + 36, 0);
            // the number is centred in the space right of the coin; the box never changes size
            coinText = UIKit.Display(UIKit.Txt("Value", coinHud, "1,000", 52, UIKit.Hex("#ffe7a3"))); var trt = coinText.rectTransform;
            trt.anchorMin = new Vector2(0, 0); trt.anchorMax = new Vector2(1, 1); trt.offsetMin = new Vector2(96, 0); trt.offsetMax = new Vector2(-24, 0);
            coinText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        void RenderCoins(int? shown = null) { if (coinText != null) coinText.text = (shown ?? Coins).ToString("N0", System.Globalization.CultureInfo.InvariantCulture); }

        /// <summary>Booster icons built from simple shapes (placeholders until the art arrives).</summary>
        RectTransform BoosterIcon(string key, RectTransform parent)
        {
            var holder = UIKit.Rect("Icon", parent);
            if (key == "undo")
            {
                var c = UIKit.Img("Disc", holder, UIKit.Circle, UIKit.Hex("#6aa0ef")); UIKit.Stretch(c.rectTransform);
                var a = UIKit.Img("Arrow", holder, UIKit.IconRetry, Color.white); UIKit.Stretch(a.rectTransform, 12); a.rectTransform.localScale = new Vector3(-1, 1, 1);
            }
            else if (key == "hint")
            {
                var c = UIKit.Img("Bulb", holder, UIKit.Circle, UIKit.Hex("#ffd34f")); UIKit.Stretch(c.rectTransform, 4);
                var t = UIKit.Txt("Q", holder, "?", 56, UIKit.Hex("#a87518")); UIKit.Stretch(t.rectTransform);
            }
            else
            {
                var bar = UIKit.Img("Bar", holder, UIKit.Rounded, UIKit.Hex("#a87650")); bar.rectTransform.anchorMin = new Vector2(0, .3f); bar.rectTransform.anchorMax = new Vector2(1, .65f); bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
                var plus = UIKit.Img("Plus", holder, UIKit.Rounded, UIKit.Hex("#4fae5b")); Pill(plus, 34); plus.rectTransform.anchorMin = plus.rectTransform.anchorMax = new Vector2(.75f, .12f); plus.rectTransform.sizeDelta = new Vector2(48, 34);
                var t = UIKit.Txt("T", plus.transform, "+2", 24, Color.white); UIKit.Stretch(t.rectTransform);
            }
            return holder;
        }
    }
}
