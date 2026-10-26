// GameController – the artist's popups (25/10, same as the web demo "New art"). Used only when the art is present (Skin.On);
// the card kit in GameController.Popup.cs calls these, every popup keeps its content and buttons.
//   Panel:   9-slice of the artist's panel (pop-setting / pop-booster / pop-revive / pop-lose / pop-tutorial), title on the ribbon,
//            no small pill above the title, round X (btn-cancel) at the top-right corner
//   Buttons: btn-green (main) · btn-yellow (watch ad, Claim ×3) · btn-blue (Retry) · btn-grey (not enough coins); white text
//   Win:     plain panel pop-win + banner "Completed" over its top edge + soft light behind the banner (clipped above the panel)
//            + picture in level-board-big + big coin "+20" + Claim ×3 (yellow) and Next (green, after 3 s) + confetti
// Sizes are reference pixels; the web demo's CSS pixels × 3.09 (popup 304 css px = card 940).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        static class SkinPop
        {
            public const string Panel = "Setting/pop-setting", Booster = "Booster/pop-booster", Revive = "Win-Lose-Revive/pop-revive",
                                 Lose = "Win-Lose-Revive/pop-lose", Tutorial = "Tutorial/pop-tutorial", Win = "Win-Lose-Revive/pop-win";
            // The panels are drawn at the art's own size (818 px wide, as in the mockups, e.g. Demo/out slot.png). All values in art pixels.
            public const float CardW = 818, CardInner = 660;
            public const float RibbonY = 57, RibbonH = 110, Top = 170, WinTop = 175, Bottom = 90;   // ribbon = rows 2–112 of the art; content starts under it
            // pop-revive / pop-lose / pop-booster have a round icon spot drawn in the art: centre 390 px from the top, radius 216.
            // Their 9-slice keeps the top 620 px fixed so the circle is never stretched.
            public const float CircleY = 390, CircleBottom = 606, CircleSlice = 620, IconSize = 366;
            public const int LeadSize = 46; public const float LeadGap = 50;                  // text under the icon (mockup: orange-brown, ~50 px under the circle)
            public const float BtnH = 140, SmallBtnW = 420, KnobX = 56;       // KnobX: switch knob centre, left (OFF) / right (ON) of the track centre
            public const float ChoiceH = 158, ChoiceSide = 58, ChoiceGap = 36;  // [coins] [ad]: two btn-*-big of ~333 × 158 with a 36 px gap, 58 px from the panel edges (user 25/10: bigger)
            public const float CloseSize = 112, CloseX = -19, CloseY = -21;    // round X: centre near the top-right corner
            public static bool HasCircle(string art) => art == Revive || art == Lose || art == Booster;
            public static readonly Color Ribbon = UIKit.Hex("#b8691c"), Lead = UIKit.Hex("#ba703e"), Soft = UIKit.Hex("#9a6a46"), RowText = UIKit.Hex("#9a5a2c"),
                                         Well = UIKit.Hex("#f6d8b5"), WinInk = UIKit.Hex("#c06a12"), Peach = UIKit.Hex("#f8dcc0");
        }

        /// <summary>Panel sprite for a popup: the ribbon panels share one 9-slice (top 160 keeps the ribbon whole), pop-win has a plain border.</summary>
        static Sprite SkinPanel(string art, out float ppu)
        {
            ppu = 1;   // the art's own size
            if (art == SkinPop.Win) return Skin.Sliced(art, 50, 50, 50, 50);
            return Skin.Sliced(art, 58, SkinPop.HasCircle(art) ? SkinPop.CircleSlice : 160, 58, 58);
        }

        // ---------- buttons ----------
        /// <summary>Artist button for one of the old button colors (null = keep the old drawing).</summary>
        static string SkinBtnPath(Color bg)
        {
            switch (ColorUtility.ToHtmlStringRGB(bg))
            {
                case "2F8A3D": return "btn-green";
                case "F6C94F": return "btn-yellow";
                case "D9D0BF": return "btn-grey";
                case "EFE3C9": return "btn-blue";
                default: return null;
            }
        }
        static Color SkinBtnInk(string path) => path == "btn-yellow" ? UIKit.Hex("#b8691c") : path == "btn-blue" ? UIKit.Hex("#2a5ea8") : path == "btn-grey" ? UIKit.Hex("#77706a") : UIKit.Hex("#1d6b2a");
        /// <summary>Same structure as FlatButton (Base with the Button, Face child for the content) so callers work unchanged.</summary>
        Button SkinFlatButton(RectTransform parent, string label, string path, int size, System.Action act)
        {
            var baseI = UIKit.Img("Base", parent, null, Color.white); UIKit.Stretch(baseI.rectTransform);
            var sp = Skin.Sliced(path, 70, 60, 70, 60); baseI.sprite = sp; baseI.type = Image.Type.Sliced;
            // scale the corners with the button height, so a lower button keeps the art's round ends (no squashed corners)
            float hgt = parent.rect.height; if (sp != null && hgt > 1) baseI.pixelsPerUnitMultiplier = sp.rect.height / hgt;
            var face = UIKit.Img("Face", baseI.rectTransform, null, new Color(1, 1, 1, 0), true); UIKit.Stretch(face.rectTransform); face.rectTransform.offsetMin = new Vector2(0, 14);
            if (!string.IsNullOrEmpty(label)) { var t = UIKit.Display(UIKit.Txt("Label", face.transform, label, size, Color.white)); UIKit.Stretch(t.rectTransform, 10); ButtonTextShadow(t); }
            var b = baseI.gameObject.AddComponent<Button>(); b.targetGraphic = baseI; if (act != null) b.onClick.AddListener(() => act()); else b.interactable = false;
            var cb = b.colors; cb.disabledColor = Color.white; b.colors = cb;
            return b;
        }
        /// <summary>[coin price] green (grey when not enough coins) · [ad Free] yellow, the wide "-big" buttons (mockup out slot.png).
        /// topWord (e.g. "Continue") is written small above the price; without it the price is centred.</summary>
        Button SkinChoiceButton(RectTransform holder, bool ad, string big, System.Action act, string topWord = null)
        {
            string path = ad ? "btn-yellow-big" : act != null ? "btn-green-big" : "btn-grey";
            var b = SkinFlatButton(holder, "", path, 30, act); var face = (RectTransform)b.transform.GetChild(0);
            bool two = !ad && !string.IsNullOrEmpty(topWord);
            var t = UIKit.Display(UIKit.Txt("Big", face, big, two ? 60 : 64, Color.white)); var trt = t.rectTransform; UIKit.Stretch(trt); ButtonTextShadow(t);
            float iconS = two ? 62 : 76, tw = t.preferredWidth + iconS + 10, lineY = two ? -22 : 0;
            trt.offsetMin = new Vector2(iconS + 10, lineY); trt.offsetMax = new Vector2(0, lineY);
            if (two)
            {
                var top = UIKit.Display(UIKit.Txt("Top", face, topWord, 36, Color.white)); var tr = top.rectTransform; UIKit.Stretch(tr); tr.offsetMin = new Vector2(0, 62); tr.offsetMax = new Vector2(0, -10); ButtonTextShadow(top);
            }
            var ic = UIKit.SkinImg("Icon", face, ad ? "icon-ads" : "Ingame/icon-coin"); ic.preserveAspect = true; var irt = ic.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(.5f, .5f); irt.sizeDelta = new Vector2(iconS, iconS); irt.anchoredPosition = new Vector2(-tw / 2 + iconS / 2, lineY);
            if (act == null) ic.color = new Color(.85f, .85f, .85f, 1);
            return b;
        }

        // ---------- pieces ----------
        void SkinTitle(Card c, Text t)
        {
            var trt = t.rectTransform; trt.SetParent(c.rt, false); trt.anchorMin = trt.anchorMax = new Vector2(.5f, 1); trt.pivot = new Vector2(.5f, .5f);
            trt.sizeDelta = new Vector2(Card.W - 300, SkinPop.RibbonH); trt.anchoredPosition = new Vector2(0, -SkinPop.RibbonY);
            t.color = Color.white; t.fontSize = 58; t.resizeTextForBestFit = true; t.resizeTextMinSize = 32; t.resizeTextMaxSize = 58; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIKit.Outlined(t, SkinPop.Ribbon, 4);
        }
        void SkinClose(RectTransform card, System.Action onClose)
        {
            var x = UIKit.SkinImg("Close", card, "btn-cancel", true); var xrt = x.rectTransform;
            xrt.anchorMin = xrt.anchorMax = new Vector2(1, 1); xrt.sizeDelta = new Vector2(SkinPop.CloseSize, SkinPop.CloseSize); xrt.anchoredPosition = new Vector2(SkinPop.CloseX, SkinPop.CloseY);
            x.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());
        }
        void SkinProgress(RectTransform bar, int pct)
        {
            var bg = UIKit.SkinImg("Bar", bar, "Win-Lose-Revive/level-progress1"); UIKit.Stretch(bg.rectTransform);
            var fill = UIKit.SkinImg("Fill", bar, "Win-Lose-Revive/level-progress2"); UIKit.Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(10, 9); fill.rectTransform.offsetMax = new Vector2(-10, -9);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = Mathf.Clamp01(pct / 100f);
        }
        /// <summary>Big booster picture for the booster popups.</summary>
        RectTransform SkinBoosterBig(string k, RectTransform parent)
        {
            var im = UIKit.SkinImg("Booster", parent, k == "undo" ? "Booster/booster-undo-big" : k == "hint" ? "Booster/booster-hint-big" : "Booster/booster-extra-shelf-big");
            im.preserveAspect = true; im.rectTransform.sizeDelta = new Vector2(250, 250); return im.rectTransform;
        }
        void SkinBadge(RectTransform icon, string text)
        {
            var b = UIKit.SkinImg("Badge", icon, "Booster/count-box-big"); var rt = b.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(1, 0); rt.sizeDelta = new Vector2(124, 128); rt.anchoredPosition = new Vector2(0, 10);
            var t = UIKit.Display(UIKit.Txt("T", rt, text, 56, Color.white)); UIKit.Stretch(t.rectTransform); UIKit.Outlined(t, UIKit.Hex("#8e2a1c"), 3);
            UIKit.Lift(t, 12);   // count-box-big has a lip at the bottom: its red face is above the picture centre
        }

        // ---------- settings ----------
        void SkinSettingRow(Card c, string key, string label, string icon)
        {
            var row = c.Row(150, 24); var bg = UIKit.Img("Bg", row, Skin.Sliced("Setting/inner-box", 40, 40, 40, 40), Color.white); bg.type = Image.Type.Sliced; UIKit.Stretch(bg.rectTransform);
            var ic = UIKit.SkinImg("Icon", row, icon); ic.preserveAspect = true; var irt = ic.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0, .5f); irt.sizeDelta = new Vector2(80, 80); irt.anchoredPosition = new Vector2(82, 0);
            var t = UIKit.Txt("Label", row, label, 50, SkinPop.RowText, TextAnchor.MiddleLeft); var trt = t.rectTransform; UIKit.Stretch(trt); trt.offsetMin = new Vector2(150, 0);
            SkinSwitch(row, key);
        }
        /// <summary>Switch from check-box1 (track) + check-box3 (green knob, ON) / check-box2 (grey knob, OFF), ON / OFF written on the track.</summary>
        void SkinSwitch(RectTransform row, string key)
        {
            var track = UIKit.SkinImg("Switch", row, "Setting/check-box1", true); var rt = track.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, .5f); rt.pivot = new Vector2(1, .5f); rt.sizeDelta = new Vector2(196, 70); rt.anchoredPosition = new Vector2(-40, 0);
            var word = UIKit.Txt("Word", rt, "ON", 30, Color.white); UIKit.Stretch(word.rectTransform); LocText.Fixed(word);
            var knob = UIKit.Img("Knob", rt, null, Color.white); knob.preserveAspect = true; knob.rectTransform.sizeDelta = new Vector2(80, 70);
            System.Action draw = () =>
            {
                bool on = SettingOn(key); knob.sprite = Skin.Get(on ? "Setting/check-box3" : "Setting/check-box2");   // check-box3 = green knob (ON), check-box2 = grey knob (OFF)
                knob.rectTransform.anchoredPosition = new Vector2(on ? SkinPop.KnobX : -SkinPop.KnobX, 0);
                word.text = on ? "ON" : "OFF"; word.alignment = on ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                word.rectTransform.offsetMin = new Vector2(on ? 24 : 0, 0); word.rectTransform.offsetMax = new Vector2(on ? 0 : -22, 0);
            };
            draw();
            Coroutine slide = null;
            track.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                PlayerPrefs.SetInt("kl.set." + key, SettingOn(key) ? 0 : 1); ApplySettings(); Sfx.Play(Sfx.Tap); if (key == "vibe") Buzz();
                if (slide != null) StopCoroutine(slide);
                slide = StartCoroutine(SwitchSlide(knob, word, SettingOn(key), draw));
                KLog.Info("Config", $"Setting {key} = {(SettingOn(key) ? "on" : "off")}");
            });
        }
        /// <summary>Continue (green) and Retry (blue) side by side at the bottom of Settings; Retry replaces the old ↻ button on the screen.</summary>
        void SkinSettingButtons(Card c)
        {
            var row = c.Row(SkinPop.BtnH, 40); float w = (Card.Inner - 30) / 2;
            var a = UIKit.Rect("Continue", row); a.anchorMin = new Vector2(0, 0); a.anchorMax = new Vector2(0, 1); a.pivot = new Vector2(0, .5f); a.sizeDelta = new Vector2(w, 0);
            SkinFlatButton(a, "Continue", "btn-green", 54, CloseModal);
            var b = UIKit.Rect("Retry", row); b.anchorMin = new Vector2(0, 0); b.anchorMax = new Vector2(0, 1); b.pivot = new Vector2(0, .5f); b.sizeDelta = new Vector2(w, 0); b.anchoredPosition = new Vector2(w + 30, 0);
            SkinFlatButton(b, "Retry", "btn-blue", 54, () => { CloseModal(); AskRestart(); });
        }

        // ---------- lose ----------
        void SkinLosePicture(Card c)
        {
            // in the round spot of pop-lose (like the yarn bag in pop-revive)
            c.BigIcon(ic => { var im = UIKit.SkinImg("Tangled", ic, "Win-Lose-Revive/img-tangled"); im.preserveAspect = true; UIKit.Stretch(im.rectTransform, 20); });
        }

        // ---------- win ----------
        /// <summary>Win content in the new art. Fills nextBtn / nextRow so ShowWin can show Next after WinNextDelay.</summary>
        void SkinWin(Card c, int reward, int next, ref Button nextBtn, ref RectTransform nextRow)
        {
            // all sizes are the art's own pixels (panel 818 wide); the banner is wider than the panel (911)
            const float BannerUp = 108, TitleY = 61;   // banner top above the panel top; "Completed" centre under the banner top (face = rows 2–120 of 193)
            // light behind the banner, only above the panel's top edge (RectMask2D box ends at the panel top)
            var clip = UIKit.Rect("LightClip", c.rt); clip.anchorMin = clip.anchorMax = new Vector2(.5f, 1); clip.pivot = new Vector2(.5f, 0);
            clip.sizeDelta = new Vector2(1400, 800); clip.anchoredPosition = Vector2.zero; clip.gameObject.AddComponent<RectMask2D>();
            WinLight(clip, new Vector2(0, BannerUp - TitleY));
            // banner over the top edge, "Completed" on its face
            var banner = UIKit.SkinImg("Banner", c.rt, "Win-Lose-Revive/pop-win-banner"); var brt = banner.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(.5f, 1); brt.pivot = new Vector2(.5f, 1); brt.sizeDelta = new Vector2(911, 193); brt.anchoredPosition = new Vector2(0, BannerUp);
            var title = UIKit.Display(UIKit.Txt("Title", brt, "Completed", 70, Color.white)); var trt = title.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(.5f, 1); trt.sizeDelta = new Vector2(700, 118); trt.anchoredPosition = new Vector2(0, -TitleY);
            UIKit.Outlined(title, SkinPop.WinInk, 4);
            // picture in its frame
            var row = c.Row(425, 0, 410); var frame = UIKit.SkinImg("Frame", row, "Win-Lose-Revive/level-board-big"); UIKit.Stretch(frame.rectTransform);
            var tex = PictureRenderer.NewTexture(); PictureRenderer.Draw(tex, L, k => true, null);
            var pic = UIKit.Rect("Picture", row).gameObject.AddComponent<RawImage>(); pic.texture = tex; UIKit.Stretch(pic.rectTransform, 35);
            // big coin + reward
            var coinRow = c.Row(110, 20, 360);
            var ci = UIKit.SkinImg("Coin", coinRow, "Win-Lose-Revive/icon-coin-big"); ci.preserveAspect = true; ci.rectTransform.sizeDelta = new Vector2(96, 109); ci.rectTransform.anchoredPosition = new Vector2(-62, 0);
            var ct = UIKit.Display(UIKit.Txt("Amount", coinRow, $"+{reward}", 76, Color.white)); ct.rectTransform.sizeDelta = new Vector2(200, 110); ct.rectTransform.anchoredPosition = new Vector2(52, 0); UIKit.Outlined(ct, SkinPop.WinInk, 4);
            // Claim ×3 (yellow, with the ad icon) and Next (green, after WinNextDelay): the wide "-big" buttons at their own size
            var claimRow = c.Row(144, 26, 402);
            var claim = SkinFlatButton(claimRow, "", "btn-yellow-big", 54, null); var face = (RectTransform)claim.transform.GetChild(0);
            var ad = UIKit.SkinImg("Ad", face, "icon-ads"); ad.preserveAspect = true; ad.rectTransform.sizeDelta = new Vector2(64, 64); ad.rectTransform.anchoredPosition = new Vector2(-112, 0);
            var ctx = UIKit.Display(UIKit.Txt("T", face, "Claim ×3", 56, Color.white)); ctx.rectTransform.sizeDelta = new Vector2(290, 90); ctx.rectTransform.anchoredPosition = new Vector2(32, 0); ButtonTextShadow(ctx);
            claim.interactable = true; claim.onClick.AddListener(() => { if (winCollecting) return; SetCardButtons(false); PlayAd(() => StartCoroutine(CollectWin(reward * Cfg.winClaimMultiplier, claim.transform.position, next))); });
            claim.gameObject.AddComponent<Breath>();
            nextRow = c.Row(144, 16, 402); Button nb = null;
            nb = SkinFlatButton(nextRow, "Next", "btn-green-big", 58, () => { if (!winCollecting) StartCoroutine(CollectWin(reward, nb.transform.position, next)); });
            nextBtn = nb; nextRow.gameObject.AddComponent<CanvasGroup>().alpha = 0; nb.interactable = false;
        }
        /// <summary>Soft light (agreed 25/10): 2 short counter-turning sunbursts, a breathing glow, 5 small twinkles. Off with reduce motion? (no such setting yet).</summary>
        void WinLight(RectTransform parent, Vector2 centre)
        {
            var holder = UIKit.Rect("WinLight", parent); holder.anchorMin = holder.anchorMax = new Vector2(.5f, 0); holder.sizeDelta = Vector2.zero; holder.anchoredPosition = centre;
            var r1 = UIKit.Img("Rays1", holder, Skin.Rays(10, 17, new Color(1, .886f, .47f, .26f)), Color.white); r1.rectTransform.sizeDelta = new Vector2(620, 620);
            var r2 = UIKit.Img("Rays2", holder, Skin.Rays(8, 12, new Color(1, .98f, .86f, .16f)), Color.white); r2.rectTransform.sizeDelta = new Vector2(620, 620); r2.rectTransform.localEulerAngles = new Vector3(0, 0, 11);
            LoopAnim.Add(r1.gameObject, 30, p => r1.rectTransform.localEulerAngles = new Vector3(0, 0, -360 * p));
            LoopAnim.Add(r2.gameObject, 44, p => r2.rectTransform.localEulerAngles = new Vector3(0, 0, 11 + 360 * p));
            var glow = UIKit.SkinImg("Glow", holder, "Ingame/light-fx"); glow.rectTransform.sizeDelta = new Vector2(590, 590);
            LoopAnim.Add(glow.gameObject, 2.4f, p => { float s = .5f - .5f * Mathf.Cos(p * Mathf.PI * 2); glow.color = new Color(1, 1, 1, Mathf.Lerp(.45f, .65f, s)); glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(.96f, 1.02f, s); });
            (Vector2 pos, float size, float delay)[] spk = { (new Vector2(-318, 91), 44, 0), (new Vector2(-188, 210), 59, .45f), (new Vector2(-37, 279), 44, .9f), (new Vector2(118, 231), 54, .25f), (new Vector2(258, 124), 44, .7f) };
            foreach (var s in spk)
            {
                var im = UIKit.Img("Sparkle", holder, Skin.Sparkle(), Color.white); var rt = im.rectTransform; rt.sizeDelta = new Vector2(s.size, s.size); rt.anchoredPosition = s.pos;
                LoopAnim.Add(im.gameObject, 2.6f, p =>
                {
                    rt.localScale = Vector3.one * KF.At(p, 0, 0, .45f, .75f, .7f, .3f, 1, 0); rt.localEulerAngles = new Vector3(0, 0, KF.At(p, 0, 0, .45f, 45, .7f, 70, 1, 90));
                    im.color = new Color(1, 1, 1, KF.At(p, 0, 0, .45f, .8f, .7f, .4f, 1, 0));
                }, s.delay);
            }
        }

        // ---------- yarn bag in the revive popup ----------
        /// <summary>The artist's open bag (bag1) behind the balls, its cut-out front over them. Returns false without the art.</summary>
        bool SkinBagLoop(RectTransform holder, Image back, Image front, ref Vector2 mouth, float bw)
        {
            var b1 = Skin.Get("Ingame/bag1"); var lip = Skin.BagFront(); if (b1 == null || lip == null) return false;
            float bh = bw * b1.rect.height / b1.rect.width; holder.sizeDelta = new Vector2(bw, bh);
            back.sprite = b1; front.sprite = lip; mouth = new Vector2(0, bh * (.5f - .15f)); return true;
        }

        // ---------- item intro ----------
        Image SkinDemoBadge(RectTransform parent, string n, Vector2 pos)
        {
            var b = UIKit.SkinImg("Badge", parent, "Tutorial/hidden-count-big"); b.rectTransform.sizeDelta = new Vector2(96, 95); b.rectTransform.anchoredPosition = pos;
            var t = UIKit.Txt("N", b.transform, n, 50, UIKit.Hex("#3c4a7a")); UIKit.Stretch(t.rectTransform); return b;
        }

        /// <summary>Switch animation (0.18 s): the knob slides across, changes color halfway, the ON/OFF word fades out and back in.</summary>
        IEnumerator SwitchSlide(Image knob, Text word, bool on, System.Action draw)
        {
            const float Dur = .18f; float from = on ? -SkinPop.KnobX : SkinPop.KnobX, to = -from; bool swapped = false;
            for (float t = 0; t < Dur; t += Time.unscaledDeltaTime)
            {
                if (knob == null) yield break;
                float p = t / Dur, e = KF.EaseOut(p);
                knob.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(from, to, e), 0);
                knob.rectTransform.localScale = new Vector3(1 + .12f * Mathf.Sin(p * Mathf.PI), 1 - .06f * Mathf.Sin(p * Mathf.PI), 1);   // a little stretch while moving
                if (!swapped && p >= .5f) { swapped = true; knob.sprite = Skin.Get(on ? "Setting/check-box3" : "Setting/check-box2"); }
                var c = word.color; c.a = Mathf.Abs(p - .5f) * 2; word.color = c;
                if (p >= .5f && word.text != (on ? "ON" : "OFF")) { var pos = knob.rectTransform.anchoredPosition; draw(); knob.rectTransform.anchoredPosition = pos; }
                yield return null;
            }
            if (knob == null) yield break;
            knob.rectTransform.localScale = Vector3.one; var wc = word.color; wc.a = 1; word.color = wc; draw();
        }

        /// <summary>Text on the artist's buttons: white with a soft dark line under it (demo: text-shadow 0 2px 0 rgba(0,0,0,.25)), no outline.</summary>
        static void ButtonTextShadow(Text t)
        {
            var s = t.gameObject.AddComponent<SoftShadow>(); s.color = new Color(0, 0, 0, .28f); s.offset = new Vector2(0, -5); s.blur = 0;
        }
        /// <summary>Change an artist button to another color (e.g. "Try again" is blue in the demo).</summary>
        static void SkinRecolor(Button b, string path)
        {
            var im = b != null ? b.GetComponent<Image>() : null; var sp = Skin.Sliced(path, 70, 0, 70, 0); if (im != null && sp != null) im.sprite = sp;
        }
    }
}
