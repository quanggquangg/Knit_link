// GameController – "HARD" / "SUPER HARD" warning when a hard level starts (user 26/10).
//   After the level start animation (balls have dropped), before any tutorial popup:
//   the whole screen dims, a ribbon slides in from the left across the middle of the screen, the two icons pop in on each
//   side of the word and wiggle, stays 2 s, then the ribbon slides out to the right. Tap anywhere to skip. Shown every time the level starts.
// Art (Resources/Skin/UI - Knit Link/Ingame): banner-hard / banner-super-hard (ribbon, no text), icon-hard / icon-super-hard.
// The word is drawn in code (translated: Khó / Siêu khó). Without the art nothing is shown (the HARD chip by the title stays).
// Timing and sizes: HardBan below.
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        static class HardBan
        {
            public const float In = .34f, Out = .28f;   // seconds: slide in, slide out
            public static float Hold = 2f;             // seconds the ribbon stays (user 26/10: 2 s) (not const: the editor can hold it longer to take a screenshot)
            public const float DimAlpha = .55f;
            public const float RibbonH = 241;                         // ribbon height (art is 1080 × 241); stretched to the screen width
            public const float TextSize = 104, IconH = 150, IconGap = 34;   // word size; icon height; space between icon and word
            public const int Sorting = 95;                            // above popups (80), confetti (85), coin HUD (90)
        }

        /// <summary>Show the warning if this level is hard / super hard and the art is there. Waits until it is gone.</summary>
        IEnumerator HardBanner(int my)
        {
            bool super = L.kind == "superhard"; if (!SK || (!super && L.kind != "hard")) yield break;
            var ribbonSp = Skin.Get(super ? "Ingame/banner-super-hard" : "Ingame/banner-hard");
            var iconSp = Skin.Get(super ? "Ingame/icon-super-hard" : "Ingame/icon-hard");
            if (ribbonSp == null) yield break;

            // layer over the whole screen
            var layer = UIKit.Rect("HardBanner", screenRT); UIKit.Stretch(layer);
            var cv = layer.gameObject.AddComponent<Canvas>(); cv.overrideSorting = true; cv.sortingOrder = HardBan.Sorting;
            layer.gameObject.AddComponent<GraphicRaycaster>();
            var dim = UIKit.Img("Dim", layer, null, new Color(.035f, .05f, .13f, 0), true); UIKit.Stretch(dim.rectTransform);
            bool skip = false; dim.gameObject.AddComponent<Button>().onClick.AddListener(() => skip = true);

            // ribbon: full screen width, middle of the screen
            var ribbon = UIKit.Img("Ribbon", layer, ribbonSp, Color.white); var rrt = ribbon.rectTransform;
            rrt.anchorMin = new Vector2(0, .5f); rrt.anchorMax = new Vector2(1, .5f); rrt.sizeDelta = new Vector2(0, HardBan.RibbonH);
            UIKit.DropShadow(ribbon, 10, 8, new Color(0, 0, 0, .35f));

            // word + icons
            var word = UIKit.Txt("Word", rrt, super ? "Super hard" : "Hard", (int)HardBan.TextSize, Color.white);
            UIKit.Display(word); word.horizontalOverflow = HorizontalWrapMode.Overflow; UIKit.Stretch(word.rectTransform);
            LocText.Upper(word);
            UIKit.Outlined(word, UIKit.Hex(super ? "#3d1f6e" : "#7a1a28"), 6);
            UIKit.Lift(word, 6);
            float half = word.preferredWidth / 2;
            var icons = new RectTransform[2];
            if (iconSp != null)
                for (int s = 0; s < 2; s++)
                {
                    var ic = UIKit.Img(s == 0 ? "IconL" : "IconR", rrt, iconSp, Color.white); ic.preserveAspect = true; var irt = ic.rectTransform;
                    float w = HardBan.IconH * iconSp.rect.width / iconSp.rect.height;
                    irt.sizeDelta = new Vector2(w, HardBan.IconH);
                    irt.anchoredPosition = new Vector2((s == 0 ? -1 : 1) * (half + HardBan.IconGap + w / 2), 2);
                    irt.localScale = Vector3.zero; icons[s] = irt;
                    UIKit.DropShadow(ic, 5, 4, new Color(0, 0, 0, .3f));
                }

            float W = screenRT.rect.width;
            void Pose(float x, float dimA) { rrt.anchoredPosition = new Vector2(x, 0); dim.color = new Color(.035f, .05f, .13f, dimA); }
            KLog.Info("Tutorial", $"{(super ? "Super hard" : "Hard")} warning on level {L.num}");
            Sfx.Play(super ? Sfx.SuperHard : Sfx.Hard);

            // slide in (small overshoot), icons pop a little later
            for (float t = 0; t < HardBan.In && !skip; t += Time.unscaledDeltaTime)
            {
                if (my != token) { Destroy(layer.gameObject); yield break; }
                float p = t / HardBan.In, e = 1 + 2.2f * Mathf.Pow(p - 1, 3) + 1.2f * Mathf.Pow(p - 1, 2);   // ease-out-back
                Pose(Mathf.LerpUnclamped(-W, 0, e), HardBan.DimAlpha * Mathf.Clamp01(p * 1.6f));
                yield return null;
            }
            Pose(0, HardBan.DimAlpha);
            // stay: icons pop (0..0.25 s) then wiggle
            for (float t = 0; t < HardBan.Hold && !skip; t += Time.unscaledDeltaTime)
            {
                if (my != token) { Destroy(layer.gameObject); yield break; }
                float pop = Mathf.Clamp01(t / .25f), sc = pop < .7f ? Mathf.Lerp(0, 1.18f, pop / .7f) : Mathf.Lerp(1.18f, 1, (pop - .7f) / .3f);
                for (int s = 0; s < 2; s++)
                    if (icons[s] != null) { icons[s].localScale = Vector3.one * sc; icons[s].localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 9 + s * 3.1f) * 7 * pop); }
                word.rectTransform.localScale = Vector3.one * (1 + .03f * Mathf.Sin(t * 6));
                yield return null;
            }
            // slide out to the right
            for (float t = 0; t < HardBan.Out; t += Time.unscaledDeltaTime)
            {
                if (my != token) break;
                float p = t / HardBan.Out; Pose(Mathf.Lerp(0, W, p * p), HardBan.DimAlpha * (1 - p));
                yield return null;
            }
            Destroy(layer.gameObject);
        }
    }
}
