// GameController – the artist's UI art on the in-game screen (25/10, same as the web demo "New art").
// BuildUI() builds the screen as before; SkinUI() then restyles it with the art from Game/Skin.cs when the art is present
// (Skin.On). Without the art nothing here runs and the game looks exactly as before.
//   Screen:   background · coin box (left) · gear (right, Retry moves into Settings) · "Level N" + picture name
//             picture frame · baskets (basket + yarn inside + front lip) · wooden shelf with holes · "2/6" pill + yarn bag
//             board frame (corner buttons) · roll tab · 3 round boosters on the orange bottom bar
//   Board:    hole shadow under each ball · artist balls · hidden-count badge · tangle (2 layers) · ice block + 5 shards
//   Line:     the yarn line made of rope pieces (Skin.RopeTile / RopeCorner), behind the balls
// Sizes are reference pixels (1080 × 2340) and live in SkinLay.
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
        static bool SK => Skin.On;
        static class SkinLay
        {
            public const float ShelfY = 712, ShelfH = 182, HoleW = 97, HoleH = 46, HoleUp = 26, SlotBall = 98;
            public const float SpotHoleW = .92f, SpotHoleH = .34f, SpotHoleY = .41f;   // shadow hole under each ball (× spot size): width, height, centre below the spot centre
            public const float BagChipY = 30, BagChipH = 80;   // yarn bag chip: centre a little below the shelf bottom edge, height
            public const float BoardPad = 88;   // minimum space between the yarn grid and the board edge, every side (inside the frame + its dashed line)
            public const float BoardTop = 960, TabY = 10;   // board top edge (from the column top); roll tab / bag chip centre above that edge
            public const float HomeSpace = 60;               // kept free at the bottom for the iPhone home bar (more if the phone reports a bigger safe area)
            public const float Booster = 196, BoosterGap = 250, BoosterY = 150, BarH = 200;
            public const float CoinTextLeft = 96, CoinTextUp = 8;   // coin HUD: the number is centred between this and the right end
            public static readonly Color CoinInk = UIKit.Hex("#8a4318");   // brown outline of the coin number
            public const float SlotBallUp = .36f;   // shelf ball centre above its hole (× ball size): the ball sits in the hole, not on the front edge
            public const float SubtitleY = 342;
            // baskets (web demo: 118 px and 52 px of a 390 px screen): the one taking yarn 1.6× the 200 px basket box (320 px wide),
            // the next ones 0.45× of that size (144 px) and darker; all stand on the same line (BasketBottomY, from the row centre)
            public const float Basket = 1.6f, BasketNext = .45f * Basket, BasketBottomY = -140, NextDim = .68f;
            public const float BoosterCountLift = 7;   // number on the booster count box: up so it sits in the middle of the red face
            public const float SelectedScale = 1.1f, SelRingGap = .16f;   // selected ball size; ring = ball body + this gap (× spot size)
        }
        // soft shadows (web demo: drop-shadow on balls, tangles, baskets, shelf yarn); colors = the demo's rgba values
        static readonly Color ShadowWarm = new Color(.31f, .22f, .12f, .35f), BallShadow = new Color(.33f, .2f, .1f, .55f), ShadowNavy = new Color(.04f, .06f, .16f, .35f);
        static readonly Color SkinCountBg = UIKit.Hex("#3a2b55"), SkinWarn = UIKit.Hex("#c2334d"), SkinBrown = UIKit.Hex("#9a5a2c"), SkinOrange = UIKit.Hex("#c06a12");
        Text subtitle; RectTransform skinBar; Image kindChip; Text kindText;
        readonly List<Image> slotHoles = new List<Image>();
        readonly string[] slotCol = new string[16];

        // ---------- screen ----------
        void SkinUI()
        {
            if (!SK) return;
            static void NoShadow(Component c) { foreach (var s in c.GetComponents<Shadow>()) Destroy(s); }
            void Art(Image im, string path) { var s = Skin.Get(path); if (s == null) return; im.sprite = s; im.type = Image.Type.Simple; im.color = Color.white; im.pixelsPerUnitMultiplier = 1; }

            Art(screenRT.Find("Background").GetComponent<Image>(), "Ingame/background");

            // top bar: coins left, gear right, Retry lives in Settings now
            var gear = btnSettings.GetComponent<Image>(); Art(gear, "Ingame/icon-setting"); NoShadow(gear); gear.preserveAspect = true; UIKit.DropShadow(gear, 8, 5, new Color(0, 0, 0, .3f));
            gear.transform.Find("Icon").gameObject.SetActive(false);
            ((RectTransform)btnSettings.transform).anchoredPosition = new Vector2(1080 - Lay.Side - Lay.Btn, 0);
            btnRetry.gameObject.SetActive(false);
            var ring = coinHud.GetComponent<Image>(); Art(ring, "coin-box");
            UIKit.TopLeft(coinHud, Lay.Side, Lay.TopY + (Lay.Btn - 108) / 2, 330, 108);
            coinHud.GetChild(0).gameObject.SetActive(false);   // inner dark pill
            var coin = (RectTransform)coinHud.GetChild(1); coin.GetComponent<Image>().sprite = Skin.Get("Ingame/icon-coin") ?? UIKit.IconCoin; coin.GetComponent<Image>().preserveAspect = true;
            // web demo: big coin over the left end of the box, the number white with a brown outline, centred in the box beside the coin
            coin.sizeDelta = new Vector2(118, 132); coin.anchoredPosition = new Vector2(44, 0); coin.SetAsLastSibling();
            UIKit.DropShadow(coin.GetComponent<Image>(), 5, 3, new Color(.35f, .2f, .05f, .3f));
            coinText.color = Color.white; coinText.fontSize = 48;
            // not bold (user 25/10): Fredoka Medium instead of the bold title font
            var df = coinText.GetComponent<DisplayFont>(); if (df != null) Destroy(df);
            var medium = Resources.Load<Font>("Fonts/Fredoka-Medium"); if (medium != null) coinText.font = medium;
            // very big amounts (1,000,000+) shrink to fit the box instead of running out of it
            coinText.horizontalOverflow = HorizontalWrapMode.Wrap; coinText.resizeTextForBestFit = true; coinText.resizeTextMinSize = 30; coinText.resizeTextMaxSize = 48; coinText.alignment = TextAnchor.MiddleCenter;
            // the box art has a lip at the bottom, so its face (where the number goes) sits above the middle: lift the number
            coinText.rectTransform.offsetMin = new Vector2(SkinLay.CoinTextLeft, SkinLay.CoinTextUp); coinText.rectTransform.offsetMax = new Vector2(-18, SkinLay.CoinTextUp);
            UIKit.Outlined(coinText, SkinLay.CoinInk, 3);   // thin outline: the number should not look heavy (user 25/10)

            // title + picture name
            // demo: white "Level N" with a dark shadow below (no outline), a pink "Hard" chip after it, the picture name in yellow capitals
            title.color = Color.white; foreach (var s in title.GetComponents<Shadow>()) Destroy(s); title.fontSize = 76;
            var ts = title.gameObject.AddComponent<SoftShadow>(); ts.color = new Color(.08f, .09f, .24f, .6f); ts.offset = new Vector2(0, -6); ts.blur = 1;
            subtitle = UIKit.Txt("Subtitle", root, "", 34, UIKit.Hex("#ffd257")); UIKit.TopLeft(subtitle.rectTransform, 140, SkinLay.SubtitleY, 800, 48);
            LocText.Upper(subtitle);
            var ss = subtitle.gameObject.AddComponent<SoftShadow>(); ss.color = new Color(0, 0, 0, .35f); ss.offset = new Vector2(0, -3); ss.blur = .5f;
            kindChip = UIKit.Img("KindChip", title.transform, UIKit.Rounded, UIKit.Hex("#d9566b")); Pill(kindChip, 50);
            var krt = kindChip.rectTransform; krt.anchorMin = krt.anchorMax = new Vector2(.5f, .5f); krt.sizeDelta = new Vector2(120, 50);
            var ks = kindChip.gameObject.AddComponent<SoftShadow>(); ks.color = UIKit.Hex("#a5384d"); ks.offset = new Vector2(0, -6); ks.blur = 0;
            kindText = UIKit.Display(UIKit.Txt("Text", krt, "Hard", 29, Color.white)); UIKit.Stretch(kindText.rectTransform); LocText.Upper(kindText);
            kindChip.gameObject.SetActive(false);

            // picture frame; "0% knitted" and "BASKET · NEXT / N LEFT" are gone in the new art
            var frame = picFrame.GetComponent<Image>(); Art(frame, "Ingame/picture-board"); NoShadow(frame);
            UIKit.Stretch(picImg.rectTransform, 34);
            knittedText.transform.parent.gameObject.SetActive(false);
            basketsHead.gameObject.SetActive(false);
            UIKit.TopLeft(basketRow, Lay.BasketsX, Lay.PicY, Lay.BasketsW, Lay.Pic);

            // shelf: wooden plank with holes, the balls sit in the holes
            var shelf = shelfRow.GetComponent<Image>(); Art(shelf, "Ingame/shelf");
            UIKit.TopLeft(shelfRow, Lay.Side, SkinLay.ShelfY, Lay.Width, SkinLay.ShelfH);
            int nSlots = slots.Count; float colW = (Lay.Width - 66) / nSlots;
            for (int i = 0; i < nSlots; i++)
            {
                var hole = UIKit.Img("Hole" + i, shelfRow, Skin.Get("Ingame/shelf-hole"), Color.white); var hrt = hole.rectTransform;
                hrt.anchorMin = hrt.anchorMax = new Vector2(0, .5f); hrt.sizeDelta = new Vector2(SkinLay.HoleW, SkinLay.HoleH); hrt.anchoredPosition = new Vector2(33 + colW * (i + .5f), SkinLay.HoleUp);
                hole.transform.SetSiblingIndex(0); slotHoles.Add(hole);
                var lk = slotLocks[i]; lk.sprite = Skin.Get("Ingame/shelf-hole2"); lk.color = Color.white; lk.transform.SetParent(shelfRow, false);
                lk.rectTransform.anchorMin = lk.rectTransform.anchorMax = new Vector2(0, .5f); lk.rectTransform.sizeDelta = new Vector2(94, 42); lk.rectTransform.anchoredPosition = hrt.anchoredPosition;
                var b = slots[i]; b.rectTransform.sizeDelta = new Vector2(SkinLay.SlotBall, SkinLay.SlotBall); b.rectTransform.anchoredPosition = new Vector2(hrt.anchoredPosition.x, SkinLay.HoleUp + SkinLay.SlotBall * SkinLay.SlotBallUp);
                b.preserveAspect = true; b.transform.SetAsLastSibling(); UIKit.DropShadow(b, 6, 5, ShadowWarm);   // demo: drop-shadow(0 2px 2px)
            }
            // no "2/6" row under the shelf any more (user 25/10): the board moves up, the space goes to the iPhone home bar.
            // The yarn bag chip moves onto the board's top-left edge (like the roll tab on the right).
            SkinBagChip();
            SkinBubble(toastRT.GetComponent<Image>(), toastText, true);
            shelfHead.gameObject.SetActive(false);

            // board frame
            boardPanel.offsetMax = new Vector2(-Lay.Side, -SkinLay.BoardTop);   // higher than before: no "2/6" row above it
            var bp = boardPanel.GetComponent<Image>(); var bs = Skin.Sliced("Ingame/board", 96, 96, 96, 96);
            if (bs != null) { bp.sprite = bs; bp.type = Image.Type.Sliced; bp.color = Color.white; bp.pixelsPerUnitMultiplier = 1.05f; }
            NoShadow(bp);
            var tab = rollChip.transform.parent.GetComponent<Image>(); Art(tab, "Ingame/board-tab");
            tab.rectTransform.pivot = new Vector2(1, .5f); tab.rectTransform.sizeDelta = new Vector2(300, 128); tab.rectTransform.anchoredPosition = new Vector2(-34, SkinLay.TabY);
            rollChip.color = Color.white; rollChip.fontSize = 36; rollChip.rectTransform.offsetMin = new Vector2(0, 26); UIKit.Outlined(rollChip, UIKit.Hex("#b5651d"), 2);

            // boosters: round buttons on the orange bottom bar
            var bar = UIKit.SkinImg("BottomBar", screenRT, "Ingame/bottom"); var brt = bar.rectTransform; skinBar = brt;   // full screen width, behind the column
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0); brt.pivot = new Vector2(.5f, 0); brt.sizeDelta = new Vector2(0, SkinLay.BarH); brt.anchoredPosition = Vector2.zero;
            bar.transform.SetSiblingIndex(1);   // just above the background, under everything in the column
            // 9-slice: on wide screens (tablets) only the middle stretches, the rounded ends keep their shape
            var barS = Skin.Sliced("Ingame/bottom", 110, 0, 110, 0); if (barS != null) { bar.sprite = barS; bar.type = Image.Type.Sliced; bar.pixelsPerUnitMultiplier = barS.rect.height / SkinLay.BarH; }
            for (int i = 0; i < BKeys.Length; i++)
            {
                string k = BKeys[i]; var b = bButtons[k]; var rt = (RectTransform)b.transform; var im = b.GetComponent<Image>();
                Art(im, "Ingame/btn-booster"); NoShadow(im); im.preserveAspect = true;
                rt.sizeDelta = new Vector2(SkinLay.Booster, SkinLay.Booster); rt.anchoredPosition = new Vector2(540 + (i - 1) * SkinLay.BoosterGap, SkinLay.BoosterY);
                bLabels[k].gameObject.SetActive(false);
                var holder = rt.Find("Icon"); foreach (Transform c in holder) c.gameObject.SetActive(false);
                var ic = UIKit.SkinImg("Art", holder, k == "undo" ? "Ingame/booster-undo" : k == "hint" ? "Ingame/booster-hint" : "Ingame/booster-extra-shelf");
                ic.preserveAspect = true; UIKit.Stretch(ic.rectTransform); UIKit.DropShadow(ic, 5, 3, new Color(0, 0, 0, .2f)); ((RectTransform)holder).anchoredPosition = new Vector2(0, 4); ((RectTransform)holder).sizeDelta = new Vector2(112, 112);
                var badge = bBadges[k]; badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(1, 0); badge.rectTransform.anchoredPosition = new Vector2(-24, 30);
                bCounts[k].color = Color.white;
            }
        }

        /// <summary>Booster badge in the new art: red count box with the number, green "+" when empty, dark "Lv5" pill when locked.</summary>
        void SkinBooster(string k, bool locked, int n)
        {
            var badge = bBadges[k];
            if (locked) { UIKit.Lift(bCounts[k], 2); badge.sprite = UIKit.Rounded; badge.type = Image.Type.Sliced; Pill(badge, 60); badge.color = UIKit.Hex("#6b5444"); badge.rectTransform.sizeDelta = new Vector2(110, 60); bCounts[k].fontSize = 30; return; }
            badge.sprite = Skin.Get(n > 0 ? "Ingame/count-box" : "Ingame/icon-more"); badge.type = Image.Type.Simple; badge.color = Color.white; badge.preserveAspect = true;
            badge.rectTransform.sizeDelta = new Vector2(78, 81); bCounts[k].fontSize = 38;
            UIKit.Lift(bCounts[k], SkinLay.BoosterCountLift);   // the red face of count-box is above the picture centre (lip at the bottom)
            if (n <= 0) bCounts[k].text = "";
        }

        // ---------- shelf slots ----------
        /// <summary>Show color c in shelf slot i (null = empty). Every change of a slot goes through here.</summary>
        void SetSlot(int i, string c)
        {
            if (i < 0 || i >= slots.Count) return;
            slotCol[i] = c; var im = slots[i];
            if (SK) { im.enabled = c != null; if (c != null) UIKit.Yarn(im, c); return; }
            if (c != null) { im.sprite = UIKit.Ball; im.color = UIKit.C(c); }
            else { im.sprite = UIKit.Circle; im.color = UIKit.Hex("#5c381f"); }
        }
        void SetSlotLocked(int i, bool locked)
        {
            slotLocks[i].gameObject.SetActive(locked);
            if (SK) { if (i < slotHoles.Count) slotHoles[i].gameObject.SetActive(!locked); if (locked) { slotCol[i] = null; slots[i].enabled = false; } }
            else if (locked) { slotCol[i] = null; slots[i].sprite = UIKit.Circle; slots[i].color = UIKit.Hex("#4a2c17"); }
        }

        // ---------- yarn bag chip ----------
        void SkinBagChip()
        {
            var chip = bagRT.GetComponent<Image>(); var s = Skin.Sliced("Ingame/bag-box", 48, 0, 48, 0);
            if (s != null) { chip.sprite = s; chip.type = Image.Type.Sliced; chip.color = Color.white; chip.pixelsPerUnitMultiplier = 1.1f; }
            var icon = (RectTransform)bagRT.GetChild(0); var ii = icon.GetComponent<Image>(); var isp = Skin.Get("Ingame/icon-bag"); if (isp != null) { ii.sprite = isp; ii.preserveAspect = true; }
            // centred under the shelf, hanging over its bottom edge (web demo); grows to both sides with the number of colors
            bagRT.SetParent(shelfRow, false); bagRT.SetAsLastSibling();
            bagRT.anchorMin = bagRT.anchorMax = new Vector2(.5f, 0); bagRT.pivot = new Vector2(.5f, .5f); bagRT.anchoredPosition = new Vector2(0, SkinLay.BagChipY);
            bagRT.sizeDelta = new Vector2(bagRT.sizeDelta.x, SkinLay.BagChipH);
            // bag icon inside the chip, left end
            icon.anchorMin = icon.anchorMax = new Vector2(0, .5f); icon.sizeDelta = new Vector2(SkinLay.BagChipH * .56f, SkinLay.BagChipH * .56f); icon.anchoredPosition = new Vector2(22 + SkinLay.BagChipH * .28f, -5);   // a little low, level with the yarn balls
        }

        // ---------- board spots ----------
        /// <summary>Restyle one spot after BuildBoard made it.</summary>
        void SkinCell(CellView v)
        {
            if (!SK) return;
            var hole = Skin.Get("Ingame/board-hole");
            if (hole != null) { v.shadow.sprite = hole; v.shadow.color = Color.white; v.shadow.rectTransform.sizeDelta = new Vector2(cs * SkinLay.SpotHoleW, cs * SkinLay.SpotHoleH); v.shadow.rectTransform.anchoredPosition = new Vector2(0, -cs * SkinLay.SpotHoleY); }   // hole shadow: wider and lower than the ball so it shows round its foot
            v.front.rectTransform.sizeDelta = new Vector2(cs * .94f, cs * .94f); v.front.preserveAspect = true;
            UIKit.DropShadow(v.front, cs * .07f, cs * .045f, BallShadow);   // a clear soft shadow under each ball (user 25/10: stronger than the demo)
            var badge = Skin.Get("Ingame/ball-hidden-count");
            if (badge != null)
            {
                v.back.sprite = badge; v.back.color = Color.white; v.back.rectTransform.sizeDelta = new Vector2(cs * .4f, cs * .4f); v.back.rectTransform.anchoredPosition = new Vector2(cs * .32f, cs * .32f);
                foreach (Transform c in v.back.transform) if (c.GetComponent<Text>() == null) c.gameObject.SetActive(false);
                // stack number (user 26/10): bold display font (Fredoka Bold) and one size bigger (.26 -> .30 of the spot);
                // overflow on: the glyph is never cut by the small badge box
                UIKit.Display(v.backCount); v.backCount.color = UIKit.Hex("#3c4a7a"); v.backCount.fontSize = Mathf.RoundToInt(cs * .30f);
                v.backCount.horizontalOverflow = HorizontalWrapMode.Overflow; v.backCount.verticalOverflow = VerticalWrapMode.Overflow;
                UIKit.Lift(v.backCount, cs * .015f, cs * .012f);   // the badge art's face sits a little right of centre
            }
            var tg = Skin.Get("Ingame/Asset 2/tangle2"); if (tg != null) { v.knot.sprite = tg; v.knot.preserveAspect = true; v.knot.rectTransform.sizeDelta = new Vector2(cs * .96f, cs * .96f); UIKit.DropShadow(v.knot, cs * .05f, cs * .035f, ShadowWarm); }
            var ice = Skin.Get("Ingame/Asset 2/ice");
            if (ice != null)
            {
                v.ice.sprite = ice; v.ice.type = Image.Type.Simple; v.ice.color = Color.white; v.ice.rectTransform.sizeDelta = new Vector2(cs * 1.02f, cs * 1.02f);
                foreach (Transform c in v.ice.transform) if (c.GetComponent<Text>() == null) c.gameObject.SetActive(false);
            }
        }
        Vector2 HomeBack => SK ? new Vector2(cs * .32f, cs * .32f) : new Vector2(cs * .3f, cs * .3f);
        /// <summary>Spot shadow: the hole under a ball, a faint ring on an empty spot.</summary>
        void SkinShadow(CellView v, bool empty)
        {
            if (!SK) return;
            var hole = Skin.Get("Ingame/board-hole"); if (hole == null) return;
            if (empty) { v.shadow.sprite = UIKit.Ring; v.shadow.color = new Color(.67f, .55f, .39f, .35f); v.shadow.rectTransform.sizeDelta = new Vector2(cs * .62f, cs * .62f); v.shadow.rectTransform.anchoredPosition = Vector2.zero; }
            else { v.shadow.sprite = hole; v.shadow.color = Color.white; v.shadow.rectTransform.sizeDelta = new Vector2(cs * SkinLay.SpotHoleW, cs * SkinLay.SpotHoleH); v.shadow.rectTransform.anchoredPosition = new Vector2(0, -cs * SkinLay.SpotHoleY); }   // hole shadow: wider and lower than the ball so it shows round its foot
        }
        Sprite KnotArt(bool loose) => SK ? Skin.Get(loose ? "Ingame/Asset 2/tangle1" : "Ingame/Asset 2/tangle2") ?? UIKit.KnotSprite(loose) : UIKit.KnotSprite(loose);

        /// <summary>Ice breaks: the artist's 5 shards fly out from the block and fade.</summary>
        IEnumerator IceShatter(CellView v)
        {
            var parts = new List<(RectTransform rt, Image im, Vector2 dir, float spin)>();
            for (int i = 1; i <= 5; i++)
            {
                var sp = Skin.Get("Ingame/Asset 2/ice-piece" + i); if (sp == null) continue;
                var im = UIKit.Img("Shard", v.rt, sp, Color.white); var rt = im.rectTransform; rt.sizeDelta = new Vector2(cs * 1.02f, cs * 1.02f);
                var c = PieceCentre(sp); parts.Add((rt, im, c.sqrMagnitude < 1e-4f ? Random.insideUnitCircle.normalized : c.normalized, Random.Range(-90f, 90f)));
            }
            for (float t = 0; t < .5f; t += Time.unscaledDeltaTime)
            {
                float p = t / .5f, e = 1 - (1 - p) * (1 - p);
                foreach (var q in parts) { if (q.rt == null) continue; q.rt.anchoredPosition = q.dir * cs * .7f * e + Vector2.down * cs * .5f * p * p; q.rt.localEulerAngles = new Vector3(0, 0, q.spin * e); q.im.color = new Color(1, 1, 1, 1 - p); }
                yield return null;
            }
            foreach (var q in parts) if (q.rt != null) Destroy(q.rt.gameObject);
        }
        static readonly Dictionary<Sprite, Vector2> pieceCentre = new Dictionary<Sprite, Vector2>();
        /// <summary>Where a shard sits in its (full-size) image, from the centre, in -0.5..0.5.</summary>
        static Vector2 PieceCentre(Sprite s)
        {
            if (pieceCentre.TryGetValue(s, out var c)) return c;
            var t = s.texture; c = Vector2.zero;
            try
            {
                var px = t.GetPixels32(); int n = 0; float sx = 0, sy = 0;
                for (int y = 0; y < t.height; y += 2) for (int x = 0; x < t.width; x += 2) if (px[y * t.width + x].a > 60) { sx += x; sy += y; n++; }
                if (n > 0) c = new Vector2(sx / n / t.width - .5f, sy / n / t.height - .5f);
            }
            catch { }
            return pieceCentre[s] = c;
        }

        // ---------- yarn line made of rope pieces (behind the balls) ----------
        /// <summary>Rope from the artist's wire: straight parts repeat the seamless tile, turns use the corner piece.
        /// Thickness 0.16 of a spot; the ends tuck under the two balls; it shows at once and fades with the balls (0.44 s).</summary>
        bool SkinLink(List<(int x, int y)> path, string c)
        {
            if (!SK) return false;
            var tile = Skin.RopeTile(c); var corner = Skin.RopeCorner(c); if (tile == null || corner == null) return false;
            var pts = path.Select(p => GridPos(p.x, p.y)).ToList();
            var P = new List<Vector2> { pts[0] };
            for (int i = 1; i < pts.Count - 1; i++) { var a = P[P.Count - 1]; var b = pts[i]; var n = pts[i + 1]; bool straight = (Mathf.Abs(a.x - b.x) < .5f && Mathf.Abs(b.x - n.x) < .5f) || (Mathf.Abs(a.y - b.y) < .5f && Mathf.Abs(b.y - n.y) < .5f); if (!straight) P.Add(b); }
            P.Add(pts[pts.Count - 1]);
            float T = Mathf.Max(8, cs * .16f), s = T / (Skin.RopeCoreBottom - Skin.RopeCoreTop + 1), H = Skin.RopeTileSize * s, endTrim = cs * .08f;
            float under = Skin.CornerRadius * s;   // the straight rope stops where the corner's curve starts
            var holder = UIKit.Rect("Link", board); holder.anchorMin = holder.anchorMax = new Vector2(0, 1); holder.pivot = new Vector2(0, 1); holder.sizeDelta = Vector2.zero; holder.anchoredPosition = Vector2.zero;
            holder.SetAsFirstSibling();   // behind every spot: the ends hide under the balls
            int nSeg = P.Count - 1;
            for (int i = 0; i < nSeg; i++)
            {
                Vector2 a = P[i], b = P[i + 1], d = (b - a).normalized; float len = (b - a).magnitude;
                float s0 = i == 0 ? endTrim : under, s1 = i == nSeg - 1 ? endTrim : under, l = len - s0 - s1; if (l <= .5f) continue;
                var im = UIKit.Img("Rope", holder, tile, Color.white); im.type = Image.Type.Tiled; im.pixelsPerUnitMultiplier = 1 / s; var rt = im.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, (Skin.RopeTileSize - Skin.RopeCenter) / (float)Skin.RopeTileSize);
                rt.anchoredPosition = a + d * s0; rt.sizeDelta = new Vector2(l + 1, H); rt.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
            for (int i = 1; i < nSeg; i++)
            {
                Vector2 di = (P[i] - P[i - 1]).normalized, dO = (P[i + 1] - P[i]).normalized;
                var im = UIKit.Img("Corner", holder, corner, Color.white); var rt = im.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.sizeDelta = new Vector2(Skin.CornerSize * s, Skin.CornerSize * s);
                rt.pivot = new Vector2(Skin.CornerBendX / (float)Skin.CornerSize, 1 - Skin.CornerBendY / (float)Skin.CornerSize);
                rt.anchoredPosition = P[i];
                // the piece is drawn "in from the left (+x), out downwards": rotate +x onto di, flip when the turn goes the other way
                float ang = Mathf.Atan2(di.y, di.x) * Mathf.Rad2Deg; rt.localEulerAngles = new Vector3(0, 0, ang);
                var down = new Vector2(Mathf.Sin(ang * Mathf.Deg2Rad), -Mathf.Cos(ang * Mathf.Deg2Rad));
                rt.localScale = new Vector3(1, Vector2.Dot(down, dO) >= 0 ? 1 : -1, 1);
            }
            StartCoroutine(RopeFade(holder));
            return true;
        }
        IEnumerator RopeFade(RectTransform rt)
        {   // the balls leave ~0.2 s after the match: the rope holds, then fades with them (0.44 s in all)
            var cg = rt.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false;
            for (float t = 0; t < .44f; t += Time.unscaledDeltaTime) { if (rt == null) yield break; float p = t / .44f; cg.alpha = p < .06f / .44f ? p * .44f / .06f : p < .45f ? 1 : 1 - (p - .45f) / .55f; yield return null; }
            if (rt != null) Destroy(rt.gameObject);
        }

        // ---------- baskets ----------
        // Two kinds of artist basket: with a handle (369×285 + front 357×141) and without (343×172 + front 343×146, new 25/10).
        // The 200 × 200 basket box keeps its bottom at y = -80; the back fills the width, the yarn sits in the mouth, the front lip on top.
        static readonly (float l, float t, float w, float rot, float dim)[] FillHandle = { (.15f, .22f, .31f, -14, 1), (.53f, .23f, .32f, 21, 1), (.35f, .17f, .29f, 8, .93f), (.34f, .28f, .33f, -32, 1) };
        // no handle: ball centre in basket pixels (of 343 × 172) and size in basket widths
        static readonly (float cx, float cy, float d, float rot, float dim)[] FillOpen = { (103, 18, .29f, -14, 1), (240, 20, .30f, 21, 1), (171, 4, .27f, 8, .93f), (171, 32, .305f, -32, 1) };
        static readonly int[] FillLayer = { 2, 0, 1, 3 };   // back to front: ball 2, 0, 1, 3, then the front lip
        bool SkinBasket(BasketView v, string c)
        {
            var backS = Skin.Basket(c); var frontS = Skin.BasketFront(c); if (backS == null || frontS == null) return false;
            const float W = 200, Bottom = -80;
            v.rt.pivot = new Vector2(.5f, (100 + Bottom) / 200f);   // scale and place from the basket's bottom edge, so big and small baskets stand on one line
            // every picture of the basket sits in one "Art" box, so the bump when yarn lands squashes back, yarn and front together
            // (bumping only the back made the two layers slide apart)
            var art = UIKit.Rect("Art", v.rt); UIKit.Stretch(art); art.pivot = v.rt.pivot;
            float bh = W * backS.rect.height / backS.rect.width; bool handle = bh > W * .65f;
            v.body = UIKit.Img("Body", art, backS, Color.white); v.body.rectTransform.sizeDelta = new Vector2(W, bh); v.body.rectTransform.anchoredPosition = new Vector2(0, Bottom + bh / 2);
            UIKit.DropShadow(v.body, 7, 5, ShadowNavy);   // demo: drop-shadow(0 4px 3px) on the basket
            float top = Bottom + bh;
            var fill = new Image[4];
            foreach (int i in FillLayer)
            {
                float d, x, y, rot, dim;
                if (handle) { var p = FillHandle[i]; d = p.w * W; x = -W / 2 + (p.l + p.w / 2) * W; y = top - p.t * bh - d / 2; rot = p.rot; dim = p.dim; }
                else { var p = FillOpen[i]; float k = W / 343f; d = p.d * W; x = (p.cx - 171.5f) * k; y = top - p.cy * k; rot = p.rot; dim = p.dim; }
                var bl = UIKit.YarnImg("Yarn" + i, art, c); bl.preserveAspect = true; bl.color = new Color(dim, dim, dim, 1);
                bl.rectTransform.sizeDelta = new Vector2(d, d); bl.rectTransform.anchoredPosition = new Vector2(x, y); bl.rectTransform.localEulerAngles = new Vector3(0, 0, rot);
                bl.gameObject.SetActive(false); fill[i] = bl;
            }
            v.fill = fill;
            float fw = handle ? W * .97f : W, fh = fw * frontS.rect.height / frontS.rect.width;
            v.rim = UIKit.Img("Front", art, frontS, Color.white); v.rim.rectTransform.sizeDelta = new Vector2(fw, fh);
            v.rim.rectTransform.anchoredPosition = handle ? new Vector2(W * -.005f, top - .495f * bh - fh / 2) : new Vector2(0, Bottom + fh / 2);
            v.count = UIKit.Txt("Count", v.rt, "0/4", 30, Color.white); v.count.gameObject.SetActive(false);   // the art shows the yarn, not a number
            v.label = null;
            return true;
        }

        // ---------- needle box + conveyor belt ----------
        /// <summary>Needle box lid = the artist's woven box; the ball and the count sit on its cream label.</summary>
        void SkinBoxLid(Image lid, Image shade, Image panel, Image label, Image ball)
        {
            var art = Skin.Get("Ingame/Asset 2/needle-box"); if (art == null) return;
            lid.sprite = art; lid.type = Image.Type.Simple; lid.color = Color.white;
            shade.gameObject.SetActive(false); panel.gameObject.SetActive(false);
            label.color = new Color(1, 1, 1, 0); label.rectTransform.anchoredPosition = new Vector2(0, boxLid.sizeDelta.y * .02f);
            boxCountText.color = SkinBrown;
        }
        /// <summary>Conveyor belt from the artist's vertical pieces turned on their side: track (conveyor1), moving chevrons
        /// (one 116 px period of conveyor2, tiled) and the two rails (conveyor5). The chevrons point down in the files.</summary>
        bool SkinBelt(Image strip, RectTransform arrows, int dir)
        {
            var track = Skin.Get("Ingame/Asset 2/conveyor1"); var chev = Skin.Get("Ingame/Asset 2/conveyor2"); var rail = Skin.Get("Ingame/Asset 2/conveyor5");
            if (!SK || track == null || chev == null) return false;
            const float Period = 116;
            arrows.gameObject.SetActive(false); strip.color = Color.white; strip.sprite = null; strip.color = new Color(1, 1, 1, 0);
            var rt = strip.rectTransform; float len = rt.sizeDelta.x, thick = rt.sizeDelta.y, s = thick / track.rect.width;
            Image Side(string name, Sprite sp, float w, float l, float y)
            {   // a vertical piece laid on its side: local width = across the belt, local height = along it
                var im = UIKit.Img(name, rt, sp, Color.white); im.raycastTarget = false; var r = im.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.sizeDelta = new Vector2(w, l); r.anchoredPosition = new Vector2(0, y);
                r.localEulerAngles = new Vector3(0, 0, dir > 0 ? 90 : -90); return im;
            }
            Side("Track", track, thick, len + 4, 0);
            var tex = chev.texture; var cr = chev.textureRect;
            var tile = Sprite.Create(tex, new Rect(cr.x, cr.y + Mathf.Floor((cr.height - Period) / 2), cr.width, Period), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            var c = Side("Chevrons", tile, thick, len + 2 * Period * s, 0); c.type = Image.Type.Tiled; c.pixelsPerUnitMultiplier = 1 / s;
            if (rail != null) { float rw = rail.rect.width * s; Side("RailA", rail, rw, len + 4, thick / 2 - rw / 2); Side("RailB", rail, rw, len + 4, -thick / 2 + rw / 2); }
            float step = Period * s; var crt = c.rectTransform;
            LoopAnim.Add(rt.gameObject, MechT.BeltLoop, p => { if (crt != null) crt.anchoredPosition = new Vector2(dir * step * p, 0); });
            return true;
        }

        // ---------- selection ring ----------
        /// <summary>The ring around the selected ball goes round the ball's body, not the whole picture (the loose strand
        /// at the bottom-right makes the picture off-centre). The ball is shown 1.1× when selected.</summary>
        void SkinSelRing(CellView v, string c)
        {
            var (centre, radius) = Skin.BallBody(c);
            float shown = v.front.rectTransform.sizeDelta.x * SkinLay.SelectedScale;
            var rt = v.selRing.rectTransform;
            rt.anchoredPosition = centre * shown;   // the ball sits at the spot centre
            float d = 2 * radius * shown + cs * SkinLay.SelRingGap;
            rt.sizeDelta = new Vector2(d, d);
        }

        /// <summary>What bounces when yarn lands in a basket: the whole basket picture in the new art, the body in the old drawing.</summary>
        RectTransform BasketBumpTarget(BasketView v) => SK && v.body != null && v.body.transform.parent != v.rt ? (RectTransform)v.body.transform.parent : v.body.rectTransform;
        /// <summary>Next baskets are shown darker (the web demo: opacity .72 on the dark background).</summary>
        void SkinBasketDim(BasketView v, bool dim)
        {
            var c = dim ? new Color(SkinLay.NextDim, SkinLay.NextDim, SkinLay.NextDim + .04f, 1) : Color.white;
            if (v.body != null) v.body.color = c;
            if (v.rim != null) v.rim.color = c;
        }

        // ---------- level title ----------
        /// <summary>New art: "Level N" white, a pink "Hard" / "Super hard" chip after it, the picture name in yellow capitals below.</summary>
        void SkinLevelTitle()
        {
            if (!SK || subtitle == null) return;
            title.color = Color.white;
            subtitle.text = L.name;   // LocText translates it and writes it in capitals
            bool hard = L.kind == "hard", super = L.kind == "superhard";
            kindChip.gameObject.SetActive(hard || super);
            if (!hard && !super) return;
            kindText.text = super ? "Super hard" : "Hard"; LocText.Upper(kindText);   // translated + capitals at once, so the width below is right
            float w = kindText.preferredWidth + 36; kindChip.rectTransform.sizeDelta = new Vector2(w, 50);
            // the title is translated on the next frame: measure the translated words now
            float tw = title.cachedTextGeneratorForLayout.GetPreferredWidth(Loc.T(title.text), title.GetGenerationSettings(Vector2.zero)) / title.pixelsPerUnit;
            kindChip.rectTransform.anchoredPosition = new Vector2(tw / 2 + 18 + w / 2, 6);
        }

        // ---------- yarn bag chip groups ----------
        // per color: the yarn ball of that color (same picture as on the board) and the count in white at its lower right (user 25/10)
        const float BagDot = 50, BagStep = 96;
        bool SkinBagGroup(string c, int n, int i)
        {
            var ball = Skin.Ball(c); if (ball == null) return false;
            var im = UIKit.Img("Group_" + c, bagGroupsRT, ball, Color.white); im.preserveAspect = true; var rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, .5f); rt.sizeDelta = new Vector2(BagDot, BagDot); rt.anchoredPosition = new Vector2(BagDot / 2 + i * BagStep, -2);   // a little low: the ball sits inside the chip
            UIKit.DropShadow(im, 3, 2, new Color(.15f, .05f, .3f, .35f));
            var t = UIKit.Display(UIKit.Txt("N", rt, n.ToString(), 34, Color.white)); var trt = t.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(1, 0); trt.sizeDelta = new Vector2(40, 40); trt.anchoredPosition = new Vector2(2, 12);   // number over the lower-right of the ball
            var sh = t.gameObject.AddComponent<SoftShadow>(); sh.color = new Color(.2f, .1f, .35f, .7f); sh.offset = new Vector2(0, -3); sh.blur = 1;
            return true;
        }
        float SkinBagChipWidth(int groups) => 12 + ChipIcon + 22 + groups * BagStep + 10;

        // ---------- message bubbles (tutorial coach, hints, toasts) ----------
        // the artist's cream pill (Setting/inner-box) with a soft shadow, bigger warm-brown text (user 25/10: the old ones looked plain and small)
        static readonly Color BubbleInk = UIKit.Hex("#8a4a1c"), BubbleWarn = UIKit.Hex("#b0303f");
        const int BubbleText = 44; const float BubblePadX = 64, BubbleMinH = 150;
        void SkinBubble(Image bg, Text t, bool warn = false)
        {
            if (!SK || bg == null) return;
            var sp = Skin.Sliced("Setting/inner-box", 76, 40, 76, 44); if (sp == null) return;
            bg.sprite = sp; bg.type = Image.Type.Sliced; bg.color = Color.white; bg.pixelsPerUnitMultiplier = 1;
            foreach (var o in bg.GetComponents<Outline>()) Destroy(o);
            UIKit.DropShadow(bg, 10, 8, new Color(.05f, .07f, .18f, .35f));
            t.fontSize = BubbleText; t.color = warn ? BubbleWarn : BubbleInk; t.lineSpacing = 1.05f;
            var r = t.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(BubblePadX, 24); r.offsetMax = new Vector2(-BubblePadX, -16);   // the art has a lip at the bottom
        }
        /// <summary>Make the bubble tall enough for its (translated) text.</summary>
        void SkinBubbleFit(RectTransform rt, Text t)
        {
            if (!SK || rt == null || t == null) return;
            float w = rt.sizeDelta.x - 2 * BubblePadX;
            var set = t.GetGenerationSettings(new Vector2(w, 0));
            float h = t.cachedTextGeneratorForLayout.GetPreferredHeight(Loc.T(t.text), set) / t.pixelsPerUnit;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, Mathf.Max(BubbleMinH, h + 64));
        }
    }
}
