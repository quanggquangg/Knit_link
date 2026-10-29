// GameController – Every popup of Build 1 (layouts from the design canvas, 20/10):
//   Settings (pause) · booster offer (coins / ad) · retry confirm · new booster (Claim ×2)
//   revive (shelf full → yarn bag, no moves → open the way) · lose · win (Claim ×3, Next level after 3 s)
//   rewarded-ad stand-in (own layer, so the popup under it stays).
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
        // ---------- settings (Build 1: Sound, Music, Vibration; opening it pauses the level) ----------
        static bool SettingOn(string k) => PlayerPrefs.GetInt("kl.set." + k, 1) == 1;
        static void ApplySettings() { AudioMix.Apply(SettingOn("sound"), SettingOn("music")); }   // mixer groups SFX / Music (AudioMix.cs)
        /// <summary>Short vibration on errors / losing, when the player allows it.</summary>
        static void Buzz() => Haptics.Medium();   // errors / blocked / lose (Haptics.cs, follows the Vibration switch)
        void OpenSettings()
        {
            if (modalRT.gameObject.activeSelf) return;
            // "pause": while a popup is open the board, boosters and Retry ignore input; closing it resumes play
            System.Action close = CloseModal;
            OpenCard(c =>
            {
                c.Pill("Paused"); c.Title("Settings");
                if (c.skin)
                {   // new art (mockup setting.png): rows on inner-box, switches with ON/OFF, Continue + Retry side by side
                    SkinSettingRow(c, "sound", "Sound", "Setting/icon-sound"); SkinSettingRow(c, "music", "Music", "Setting/icon-music"); SkinSettingRow(c, "vibe", "Vibration", "Setting/icon-haptic");
                    if (LocText.LanguageChoice) LanguageRow(c);
                    SkinSettingButtons(c); return;
                }
                foreach (var (key, label, icon) in new[] { ("sound", "Sound", UIKit.IconSpeaker), ("music", "Music", UIKit.IconNote), ("vibe", "Vibration", UIKit.IconPhone) })
                {
                    var row = c.Row(160, 28); var bg = UIKit.Img("Bg", row, UIKit.Rounded, UIKit.Hex("#efe2c6")); UIKit.Stretch(bg.rectTransform);
                    var ic = UIKit.Img("Icon", row, icon, CardSoft); var irt = ic.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0, .5f); irt.sizeDelta = new Vector2(76, 76); irt.anchoredPosition = new Vector2(78, 0);
                    var t = UIKit.Txt("Label", row, label, 50, UIKit.Ink, TextAnchor.MiddleLeft); var trt = t.rectTransform; UIKit.Stretch(trt); trt.offsetMin = new Vector2(150, 0);
                    Switch(row, key);
                }
                if (LocText.LanguageChoice) LanguageRow(c);
                c.BigButton("Continue", close);
            }, close);
        }
        /// <summary>Settings row "Language" with 2 buttons. The button names are never translated. Changing it rebuilds the popup.</summary>
        void LanguageRow(Card c)
        {
            var row = c.Row(c.skin ? 150 : 160, c.skin ? 24 : 28); var bg = UIKit.Img("Bg", row, UIKit.Rounded, UIKit.Hex("#efe2c6")); UIKit.Stretch(bg.rectTransform);
            if (c.skin) { bg.sprite = Skin.Sliced("Setting/inner-box", 40, 40, 40, 40); bg.type = Image.Type.Sliced; bg.color = Color.white; }
            var t = UIKit.Txt("Label", row, "Language", c.skin ? 44 : 50, c.skin ? SkinPop.RowText : UIKit.Ink, TextAnchor.MiddleLeft); var trt = t.rectTransform; UIKit.Stretch(trt); trt.offsetMin = new Vector2(40, 0);
            float x = -38;
            foreach (var (lang, name, w) in c.skin ? new[] { (Loc.Vi, "Tiếng Việt", 200f), (Loc.En, "English", 168f) } : new[] { (Loc.Vi, "Tiếng Việt", 250f), (Loc.En, "English", 210f) })
            {
                bool on = Loc.Lang == lang;
                var b = UIKit.Img("Lang_" + lang, row, UIKit.Rounded, on ? UIKit.Hex("#2f8a3d") : c.skin ? UIKit.Hex("#f1c79c") : Color.white, true); float bh = c.skin ? 80 : 94; Pill(b, bh); var brt = b.rectTransform;
                brt.anchorMin = brt.anchorMax = new Vector2(1, .5f); brt.pivot = new Vector2(1, .5f); brt.sizeDelta = new Vector2(w, bh); brt.anchoredPosition = new Vector2(x, 0); x -= w + 16;
                var bt = UIKit.Txt("Text", brt, name, c.skin ? 32 : 38, on ? Color.white : UIKit.Ink); UIKit.Stretch(bt.rectTransform); LocText.Fixed(bt);
                b.gameObject.AddComponent<Button>().WithHaptic().onClick.AddListener(() =>
                {
                    if (Loc.Lang == lang) return;
                    LocText.SetLang(lang); CloseModal(); OpenSettings();   // rebuild so pill widths fit the new words
                });
            }
        }
        void Switch(RectTransform row, string key)
        {
            var track = UIKit.Img("Switch", row, UIKit.Rounded, Color.white, true); Pill(track, 94); var rt = track.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, .5f); rt.pivot = new Vector2(1, .5f); rt.sizeDelta = new Vector2(166, 94); rt.anchoredPosition = new Vector2(-38, 0);
            var knob = UIKit.Img("Knob", rt, UIKit.Circle, Color.white); knob.rectTransform.sizeDelta = new Vector2(72, 72);
            System.Action draw = () =>
            {
                bool on = SettingOn(key); track.color = on ? UIKit.Hex("#2f8a3d") : UIKit.Hex("#b3a891");
                knob.rectTransform.anchoredPosition = new Vector2(on ? 36 : -36, 0);
            };
            draw();
            track.gameObject.AddComponent<Button>().WithHaptic().onClick.AddListener(() =>
            {
                PlayerPrefs.SetInt("kl.set." + key, SettingOn(key) ? 0 : 1); ApplySettings(); Sfx.Play(Sfx.Tap); if (key == "vibe") Buzz(); draw();
                KLog.Info("Config", $"Setting {key} = {(SettingOn(key) ? "on" : "off")}");
            });
        }

        // ---------- boosters ----------
        void OfferBooster(string k)
        {
            int price = BPrice[k];
            System.Action give = () => { CloseModal(); SetInv(k, Inv(k) + 1); RenderBoosters(); StartCoroutine(Bump((RectTransform)bButtons[k].transform, 1.16f)); };
            OpenCard(c =>
            {
                c.Pill("Out of " + BName[k]); c.Title(BName[k]);
                c.BigIcon(ic => { if (c.skin) SkinBoosterBig(k, ic); else { var b = BoosterIcon(k, ic); b.sizeDelta = new Vector2(230, 230); } Badge(ic, "×1"); });
                c.Lead(BDesc[k]);
                c.Choice(price, () => { if (Coins < price) return; Coins -= price; RenderCoins(); KLog.Info("Booster", $"Bought {k} for {price} coins"); give(); }, () => PlayAd(give));
            }, CloseModal, art: SkinPop.Booster);
        }
        /// <summary>Orange count badge on the bottom-right of a big icon ("×1", "×2").</summary>
        void Badge(RectTransform icon, string text)
        {
            if (SK) { SkinBadge(icon, text); return; }
            var b = UIKit.Img("Badge", icon, UIKit.Rounded, UIKit.Hex("#f08a3c")); Pill(b, 76); var rt = b.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(1, 0); rt.sizeDelta = new Vector2(124, 76); rt.anchoredPosition = new Vector2(0, 16);
            var t = UIKit.Display(UIKit.Txt("T", rt, text, 52, Color.white)); UIKit.Stretch(t.rectTransform);
        }
        void BoosterIntro(string k)
        {
            OpenCard(c =>
            {
                c.Pill("New booster", PillStyle.Blue); c.Title(BName[k]);
                c.BigIcon(ic => { if (c.skin) SkinBoosterBig(k, ic); else { var b = BoosterIcon(k, ic); b.sizeDelta = new Vector2(230, 230); } Badge(ic, "×" + Cfg.boostersOnUnlock); });
                c.Lead(BDesc[k]); c.BigButton("Claim", () => ClaimBooster(k));
            }, art: SkinPop.Booster);
        }
        /// <summary>Stand-in for a rewarded ad: a full-screen layer for a moment, then the reward.</summary>
        void PlayAd(System.Action reward)
        {
            foreach (Transform t in adLayer) Destroy(t.gameObject); adLayer.gameObject.SetActive(true);
            var dim = UIKit.Img("Dim", adLayer, null, new Color(.03f, .05f, .11f, .92f), true); UIKit.Stretch(dim.rectTransform);
            var t1 = UIKit.Txt("T", adLayer, "Ad playing…", 80, Pale); UIKit.TopLeft(t1.rectTransform, 90, 980, 900, 120);
            var t2 = UIKit.Txt("S", adLayer, "(demo) A real rewarded video plays here.", 40, Sky); UIKit.TopLeft(t2.rectTransform, 90, 1100, 900, 80);
            KLog.Info("Ads", "Rewarded ad shown");
            StartCoroutine(DelayUnscaled(Cfg.adSeconds, () => { adLayer.gameObject.SetActive(false); reward(); }));
        }
        IEnumerator DelayUnscaled(float s, System.Action a) { yield return new WaitForSecondsRealtime(s); a(); }

        // ---------- retry ----------
        void AskRestart()
        {
            if (S == null || S.Won || modalRT.gameObject.activeSelf) return;
            if (moves == 0) { StartLevel(levelIndex); return; }
            OpenCard(c =>
            {
                c.Pill("Retry"); c.Title("Start again?"); c.Lead("Your progress in this picture will be lost.");
                var row = c.Row(150, 40); float w = (Card.Inner - 30) / 2;
                var keep = UIKit.Rect("Keep", row); keep.anchorMin = new Vector2(0, 0); keep.anchorMax = new Vector2(0, 1); keep.pivot = new Vector2(0, .5f); keep.sizeDelta = new Vector2(w, 0);
                FlatButton(keep, "Keep playing", UIKit.Hex("#f6c94f"), UIKit.Hex("#b47b1f"), UIKit.Hex("#4a2e0c"), 46, CloseModal);
                var go = UIKit.Rect("Go", row); go.anchorMin = new Vector2(0, 0); go.anchorMax = new Vector2(0, 1); go.pivot = new Vector2(0, .5f); go.sizeDelta = new Vector2(w, 0); go.anchoredPosition = new Vector2(w + 30, 0);
                FlatButton(go, "Retry", UIKit.Hex("#efe3c9"), PaleShadow, UIKit.Ink, 46, () => { CloseModal(); StartLevel(levelIndex); });
            }, CloseModal);
        }

        // ---------- revive / lose ----------
        void ShowFail() { EndCoach(); Buzz(); AudioMix.Duck(true); Sfx.Play(Sfx.Fail); ShowRevive(false); }
        void ShowStuck() { EndCoach(); ShowRevive(true); }
        void ShowRevive(bool stuck)
        {
            KLog.Info("Revive", $"{(stuck ? "Out of moves" : "Out of space")} on level {L.num} ({L.file}), revives used {revives}");
            if (revives >= Cfg.maxRevives || (!stuck && (history.Count == 0 || lastMove.a < 0))) { ShowLose(stuck); return; }
            bool first = revives == 0; int price = first ? Cfg.revive1Coins : Cfg.revive2Coins; bool adOk = first ? Cfg.revive1ByAd : Cfg.revive2ByAd;
            System.Action go = () => DoRevive(stuck);
            OpenCard(c =>
            {
                c.Pill(stuck ? "Out of moves" : "Out of space", PillStyle.Red); c.Title(stuck ? "No moves left" : c.skin ? "Out of Slot" : "The shelf is full");
                // shelf full: the 4 oldest shelf balls (the ones that will move) keep flying into the bag.
                // New art (29/10): a small shelf + the sack in the panel's well (ReviveBagLoop, GameController.ReviveBag.cs)
                c.BigIcon(ic => { if (stuck) OpenWayIcon(ic); else if (!(c.skin && ReviveBagLoop(ic))) BagLoopIcon(ic, S.shelf.Take(Cfg.reviveBagSize).ToList()); });
                c.Lead(stuck ? "Take 2 pairs for your basket." : first ? $"Move {Cfg.reviveBagSize} shelf balls\ninto Yarn Bag" : $"Move {Cfg.reviveBagSize} more shelf balls\ninto Yarn Bag");
                c.Progress(KnittedPercent());
                c.Choice(price, () => { if (Coins < price) return; Coins -= price; RenderCoins(); go(); }, adOk ? (System.Action)(() => PlayAd(go)) : null, c.skin ? "Continue" : null);   // mockup: "Continue" above the price
                c.Link("Give up and try again", () => { CloseModal(); StartLevel(levelIndex); });
            }, art: SkinPop.Revive);
            AddHoldToSee();   // 28/10: hold the dark area to look at the board
        }
        void ShowLose(bool stuck)
        {
            OpenCard(c =>
            {
                c.Pill($"Level {L.num} failed", PillStyle.Red); c.Title(stuck ? "Out of moves" : "Out of space");
                if (c.skin) SkinLosePicture(c); else {
                var row = c.Row(290, 38, 330); var ball = UIKit.Img("Yarn", row, UIKit.Ball, UIKit.C("red")); ball.rectTransform.sizeDelta = new Vector2(250, 250); ball.rectTransform.anchoredPosition = new Vector2(-10, 10);
                var strand = UIKit.Img("Strand", row, UIKit.Ring, UIKit.C("red")); strand.rectTransform.sizeDelta = new Vector2(90, 90); strand.rectTransform.anchoredPosition = new Vector2(120, -80); }
                c.Progress(KnittedPercent()); c.Lead("So close! Give this picture another go.", 46);
                var again = c.BigButton("Try again", () => { CloseModal(); StartLevel(levelIndex); }); if (c.skin) SkinRecolor(again, "btn-blue");   // demo: blue
            }, art: SkinPop.Lose);
            AddHoldToSee();
        }
        int KnittedPercent()
        {
            var order = PictureOrder(); int done = 0;
            for (int k = 0; k < PIC * PIC; k++) { int li = L.picture[k]; if (order[k].pos < Mathf.RoundToInt(prog[li] * order[k].len)) done++; }
            return Mathf.RoundToInt(100f * done / (PIC * PIC));
        }
        void OpenWayIcon(RectTransform ic)
        {
            var a = UIKit.YarnImg("A", ic, "red"); a.preserveAspect = true; a.rectTransform.sizeDelta = new Vector2(100, 100); a.rectTransform.anchoredPosition = new Vector2(-80, -70);
            var b = UIKit.YarnImg("B", ic, "red"); b.preserveAspect = true; b.rectTransform.sizeDelta = new Vector2(100, 100); b.rectTransform.anchoredPosition = new Vector2(80, 70);
            for (int i = 1; i < 6; i++)
            {
                var d = UIKit.Img("Dot", ic, UIKit.Circle, UIKit.C("green")); d.rectTransform.sizeDelta = new Vector2(18, 18);
                float p = i / 6f; d.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-80, 80, p), Mathf.Lerp(-70, 70, p) + Mathf.Sin(p * Mathf.PI * 2) * 30);
            }
            var star = UIKit.Img("Spark", ic, UIKit.Circle, UIKit.Hex("#f6c94f")); star.rectTransform.anchoredPosition = new Vector2(80, -70); star.rectTransform.sizeDelta = new Vector2(40, 40);
        }
        /// <summary>Revive icon: balls fly from both sides into the bag's mouth and drop inside, bag squashes; 3.2 s loop.</summary>
        void BagLoopIcon(RectTransform ic, List<string> cols)
        {
            const float BW = 288; var holder = UIKit.Rect("BagLoop", ic); holder.sizeDelta = new Vector2(BW, BW); holder.anchoredPosition = new Vector2(0, -6);
            var back = UIKit.Img("Back", holder, UIKit.BagBack, Color.white); UIKit.Stretch(back.rectTransform);
            var ballsRT = UIKit.Rect("Balls", holder); UIKit.Stretch(ballsRT);
            var front = UIKit.Img("Front", holder, UIKit.BagFront, Color.white); UIKit.Stretch(front.rectTransform);
            Vector2 mouth = new Vector2(0, BW * (.5f - .3f));
            if (SK && SkinBagParts(holder, back, front, BW, out var m)) mouth = m;
            Vector2[] starts = { new Vector2(-178, 0), new Vector2(-139, 72), new Vector2(139, 72), new Vector2(178, 0) };
            for (int i = 0; i < cols.Count && i < 4; i++)
            {
                var b = UIKit.YarnImg("Ball" + i, ballsRT, cols[i]); b.preserveAspect = true; var rt = b.rectTransform; rt.sizeDelta = new Vector2(72, 72);
                var s = starts[i] + mouth;
                LoopAnim.Add(b.gameObject, 3.2f, p =>
                {   // 0–6% appear · 28% top of arc · 44% at the mouth · 52% inside · then hidden until the loop restarts
                    float x = KF.At(p, 0, s.x, .28f, Mathf.Lerp(s.x, mouth.x, .55f), .44f, mouth.x, 1, mouth.x);
                    float y = KF.At(p, 0, s.y, .28f, s.y + 40, .44f, mouth.y + 16, .52f, mouth.y - 60, 1, mouth.y - 60);
                    rt.anchoredPosition = new Vector2(x, y); rt.localScale = Vector3.one * KF.At(p, 0, .9f, .28f, 1.05f, .44f, .9f, .52f, .7f);
                    rt.localEulerAngles = new Vector3(0, 0, -360 * Mathf.Clamp01(p / .52f));
                    b.color = new Color(b.color.r, b.color.g, b.color.b, p < .06f ? p / .06f : p <= .52f ? 1 : 0);
                }, i * .35f);
            }
            // back, balls and front squash TOGETHER (squashing only the front showed the back's rim under it – fixed 25/10)
            var squash = UIKit.Rect("Squash", holder); UIKit.Stretch(squash); squash.pivot = new Vector2(.5f, .05f);   // from the bottom (stretched rect: the pivot does not move it)
            back.transform.SetParent(squash, false); ballsRT.SetParent(squash, false); front.transform.SetParent(squash, false);
            LoopAnim.Add(squash.gameObject, 3.2f, p =>
            {   // a small squash each time a ball lands (balls land at 52% + 0.35 s steps)
                float bump = 0; for (int i = 0; i < 4; i++) { float c = .52f + i * .35f / 3.2f, d = Mathf.Abs(p - c - .02f); if (d < .04f) bump = Mathf.Max(bump, 1 - d / .04f); }
                squash.localScale = new Vector3(1 + .08f * bump, 1 - .1f * bump, 1);
            });
        }

        // ---------- win (Claim ×3 first, "Next level +reward" appears after WinNextDelay in its reserved space) ----------
        const float WinNextDelay = 3f;
        int winReward; bool winCollecting;
        void ShowWin()
        {
            if (winShown) return; winShown = true; EndCoach(); AudioMix.Duck(true); Confetti(); Sfx.Play(Sfx.Win);
            int reward = Cfg.WinCoins(L.kind); winReward = reward; winCollecting = false;
            KLog.Info("Win", $"Level {L.num} ({L.file}) won in {moves} moves, reward {reward} coins (not added until the player picks)");
            MarkBeaten(levelIndex);
            int next = (levelIndex + 1) % file.levels.Length; PlayerPrefs.SetInt("kl.level", next);
            Button nextBtn = null; RectTransform nextRow = null;
            OpenCard(c =>
            {
                if (c.skin) { SkinWin(c, reward, next, ref nextBtn, ref nextRow); return; }
                c.Pill($"Level {L.num} complete", PillStyle.Green); c.Title($"{L.name}\nis knitted!", 2);
                var row = c.Row(470, 34, 470); var well = UIKit.Img("Frame", row, UIKit.Rounded, UIKit.Hex("#efe2c6")); UIKit.Stretch(well.rectTransform);
                var tex = PictureRenderer.NewTexture(); PictureRenderer.Draw(tex, L, k => true, null);
                var pic = UIKit.Rect("Picture", row).gameObject.AddComponent<RawImage>(); pic.texture = tex; UIKit.Stretch(pic.rectTransform, 26);
                var coinRow = c.Row(110, 30, 300); var cbg = UIKit.Img("Bg", coinRow, UIKit.Rounded, UIKit.Hex("#fff6dc")); UIKit.Stretch(cbg.rectTransform);
                var ci = UIKit.Img("Coin", coinRow, UIKit.IconCoin, Color.white); ci.rectTransform.sizeDelta = new Vector2(84, 84); ci.rectTransform.anchoredPosition = new Vector2(-64, 0);
                var ct = UIKit.Display(UIKit.Txt("Amount", coinRow, $"+{reward}", 76, UIKit.Hex("#7a4b0e"))); ct.rectTransform.sizeDelta = new Vector2(180, 100); ct.rectTransform.anchoredPosition = new Vector2(50, 0);
                c.Note($"{moves} moves · shelf peak {peak}/{S.cap}", CardSoft);
                var claimRow = c.Row(184, 38);
                var claim = FlatButton(claimRow, "", UIKit.Hex("#2f8a3d"), UIKit.Hex("#1d5e28"), Color.white, 64, null);
                ClaimLabel((RectTransform)claim.transform.GetChild(0), reward * Cfg.winClaimMultiplier);
                claim.interactable = true; claim.onClick.AddListener(() => { if (winCollecting) return; SetCardButtons(false); PlayAd(() => StartCoroutine(CollectWin(reward * Cfg.winClaimMultiplier, claim.transform.position, next))); });
                claim.gameObject.AddComponent<Breath>();
                nextRow = c.Row(144, 26);
                nextBtn = FlatButton(nextRow, $"Next level   +{reward}", UIKit.Hex("#efe3c9"), PaleShadow, UIKit.Ink, 54, () => { if (!winCollecting) StartCoroutine(CollectWin(reward, nextBtn.transform.position, next)); });
                nextRow.gameObject.AddComponent<CanvasGroup>().alpha = 0; nextBtn.interactable = false;
            }, art: SkinPop.Win);
            int my = token;
            StartCoroutine(DelayUnscaled(WinNextDelay, () =>
            {
                if (my != token || nextRow == null) return;
                nextRow.GetComponent<CanvasGroup>().alpha = 1; nextBtn.interactable = true; StartCoroutine(Pop(nextRow));
            }));
        }
        void ClaimLabel(RectTransform face, int amount)
        {
            var ad = UIKit.Img("Ad", face, UIKit.IconAd, Color.white); ad.rectTransform.sizeDelta = new Vector2(72, 72); ad.rectTransform.anchoredPosition = new Vector2(-250, 0);
            var t = UIKit.Display(UIKit.Txt("T", face, "Claim ×3", 66, Color.white)); t.rectTransform.sizeDelta = new Vector2(330, 100); t.rectTransform.anchoredPosition = new Vector2(-30, 0);
            var chip = UIKit.Img("Amount", face, UIKit.Rounded, UIKit.Hex("#1d5e28")); Pill(chip, 84); chip.rectTransform.sizeDelta = new Vector2(190, 84); chip.rectTransform.anchoredPosition = new Vector2(230, 0);
            var ci = UIKit.Img("Coin", chip.transform, UIKit.IconCoin, Color.white); ci.rectTransform.anchorMin = ci.rectTransform.anchorMax = new Vector2(0, .5f); ci.rectTransform.sizeDelta = new Vector2(50, 50); ci.rectTransform.anchoredPosition = new Vector2(44, 0);
            var at = UIKit.Txt("N", chip.transform, amount.ToString(), 54, Color.white); UIKit.Stretch(at.rectTransform); at.rectTransform.offsetMin = new Vector2(64, 0);
        }
        void SetCardButtons(bool on) { foreach (var b in modalRT.GetComponentsInChildren<Button>()) b.interactable = on; }
        /// <summary>Coins fly from the pressed button into the coin HUD, the number counts up, then the next level starts.</summary>
        IEnumerator CollectWin(int amount, Vector3 from, int next)
        {
            if (winCollecting) yield break; winCollecting = true; SetCardButtons(false);
            int my = token, start = Coins; Coins = start + amount; float k = canvas.scaleFactor;
            KLog.Info("Win", $"+{amount} coins collected");
            const int n = 8; int landed = 0; var to = coinHud.GetChild(1).position;
            for (int i = 0; i < n; i++) StartCoroutine(FlyCoin(from + new Vector3((i % 2 == 0 ? -1 : 1) * i * 10 * k, 0, 0), to, i * .07f, () => { landed++; Sfx.Play(Sfx.Clink); RenderCoins(start + amount * landed / n); StartCoroutine(Bump(coinHud, 1.08f)); }));
            while (landed < n) { if (my != token) yield break; yield return null; }
            RenderCoins(); yield return new WaitForSecondsRealtime(.45f);
            if (my != token) yield break;
            CloseModal(); StartLevel(next);
        }
        IEnumerator FlyCoin(Vector3 from, Vector3 to, float delay, System.Action land)
        {
            yield return new WaitForSecondsRealtime(delay);
            // the same coin picture as the coin box (27/10): artist's Ingame/icon-coin, code-drawn coin without the art
            var sp = SK ? Skin.Get("Ingame/icon-coin") : null;
            var im = UIKit.Img("Coin", hudLayer, sp ?? UIKit.IconCoin, Color.white); im.preserveAspect = true; var rt = im.rectTransform;
            rt.sizeDelta = sp != null ? new Vector2(76, 76 * sp.rect.height / sp.rect.width) : new Vector2(62, 62);
            var mid = (from + to) / 2 + Vector3.up * 110 * canvas.scaleFactor;
            for (float t = 0; t < .62f; t += Time.unscaledDeltaTime)
            {
                float p = t / .62f, e = p < .5f ? 2 * p * p : 1 - Mathf.Pow(-2 * p + 2, 2) / 2;
                rt.position = Vector3.Lerp(Vector3.Lerp(from, mid, e), Vector3.Lerp(mid, to, e), e); rt.localScale = Vector3.one * KF.At(p, 0, 1, .45f, 1.2f, 1, .8f); yield return null;
            }
            Destroy(im.gameObject); land();
        }
    }
}
