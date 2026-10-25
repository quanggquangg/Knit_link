// GameController – confetti when a level is won (25/10, same as the web demo):
//   2 cannons at the left and right screen edges, 72% down the screen, 130 pieces each, shot up and inwards; no rain from the top.
//   Pieces: 18% dots, ~20% ribbons, the rest paper squares; each one flips (width follows cos) and sways. 3.6 s, the last 0.7 s fade.
//   Colors: the picture's yarn colors + yellow #ffd257 + white + pink #ff8fb1.
//   Sound: 2 "pops" 0.07 s apart (made in code, no audio file): low thump 150→45 Hz, a paper whoosh, 4 crackles. Muted with Sound off.
// Motion numbers are the web demo's (CSS px per frame at 60 fps) × Px.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        RectTransform confettiLayer; AudioSource popperSrc; static AudioClip popperClip;

        void Confetti()
        {
            if (confettiLayer == null)
            {   // above the popups (80), under the coin HUD (90)
                confettiLayer = UIKit.Rect("Confetti", screenRT); UIKit.Stretch(confettiLayer);   // the cannons sit at the real screen edges
                var cv = confettiLayer.gameObject.AddComponent<Canvas>(); cv.overrideSorting = true; cv.sortingOrder = 85;
            }
            var cols = new List<Color>();
            if (L != null && L.baskets != null) foreach (var b in L.baskets.Distinct()) cols.Add(UIKit.C(b));
            cols.Add(UIKit.Hex("#ffd257")); cols.Add(Color.white); cols.Add(UIKit.Hex("#ff8fb1"));
            var burst = new GameObject("Burst", typeof(RectTransform)).AddComponent<ConfettiBurst>();
            var rt = (RectTransform)burst.transform; rt.SetParent(confettiLayer, false); UIKit.Stretch(rt);
            burst.Fire(cols);
            PopperSound();
        }

        void PopperSound()
        {
            if (popperSrc == null) { popperSrc = gameObject.AddComponent<AudioSource>(); popperSrc.playOnAwake = false; popperSrc.spatialBlend = 0; }
            if (popperClip == null) popperClip = MakePopperClip();
            popperSrc.PlayOneShot(popperClip, .9f);
        }
        /// <summary>Two party-popper shots, 0.07 s apart.</summary>
        static AudioClip MakePopperClip()
        {
            const int Rate = 44100; float len = .45f; int n = (int)(Rate * len); var d = new float[n];
            var rnd = new System.Random(7);
            foreach (float at in new[] { 0f, .07f })
            {
                int s0 = (int)(at * Rate); float phase = 0, lp = 0, hpPrev = 0, hpOut = 0;
                float aLp = 1 - Mathf.Exp(-2 * Mathf.PI * 2600f / Rate), aHp = Mathf.Exp(-2 * Mathf.PI * 4200f / Rate);
                for (int i = 0; s0 + i < n && i < Rate * .3f; i++)
                {
                    float t = (float)i / Rate, v = 0;
                    if (t < .16f) { float f = Mathf.Lerp(150, 45, t / .16f); phase += 2 * Mathf.PI * f / Rate; v += Mathf.Sin(phase) * Mathf.Exp(-t * 22) * .9f; }   // thump
                    float noise = (float)rnd.NextDouble() * 2 - 1;
                    if (t < .22f) { lp += aLp * (noise - lp); v += lp * Mathf.Exp(-t * 16) * .8f; }   // paper whoosh
                    hpOut = aHp * (hpOut + noise - hpPrev); hpPrev = noise;                          // crackles: short bright bursts
                    for (int k = 0; k < 4; k++) { float c = .05f + k * .045f + (k % 2) * .012f; if (t > c && t < c + .012f) v += hpOut * .6f * (1 - (t - c) / .012f); }
                    d[s0 + i] += v;
                }
            }
            float peak = .001f; foreach (var x in d) peak = Mathf.Max(peak, Mathf.Abs(x)); for (int i = 0; i < n; i++) d[i] *= .85f / peak;
            var clip = AudioClip.Create("Popper", n, 1, Rate, false); clip.SetData(d, 0); return clip;
        }
    }

    /// <summary>One confetti burst: moves its pieces every frame, removes itself at the end.</summary>
    public class ConfettiBurst : MonoBehaviour
    {
        const float Px = 2.77f, Life = 3.6f, Fade = .7f, PerCannon = 130;
        struct Piece { public RectTransform rt; public Image im; public Vector2 p, v; public float rot, vr, flip, vf, sway, swayF, w, h; public int kind; }
        readonly List<Piece> ps = new List<Piece>(); float t;

        public void Fire(List<Color> cols)
        {
            var area = ((RectTransform)transform).rect; float W = area.width, H = area.height;
            for (int side = 0; side < 2; side++)
            {
                Vector2 origin = new Vector2(side == 0 ? -8 * Px : W + 8 * Px, H * (1 - .72f));
                float baseAng = side == 0 ? -.36f * Mathf.PI : -.64f * Mathf.PI;
                for (int i = 0; i < PerCannon; i++)
                {
                    float r = Random.value; int kind = r < .18f ? 0 : r < .38f ? 1 : 2;   // 0 dot, 1 ribbon, 2 paper
                    float a = baseAng + Random.Range(-.16f, .16f), sp = Random.Range(15f, 24f) * Px;
                    float s = Random.Range(6f, 11f) * Px;
                    var pc = new Piece
                    {
                        kind = kind, p = origin, v = new Vector2(Mathf.Cos(a) * sp, -Mathf.Sin(a) * sp),   // y up in Unity
                        rot = Random.Range(0, 360f), vr = Random.Range(-12f, 12f), flip = Random.Range(0, 6.28f), vf = Random.Range(.12f, .3f),
                        sway = Random.Range(0, 6.28f), swayF = Random.Range(.05f, .11f),
                        w = kind == 0 ? s * .8f : kind == 1 ? s * .45f : s, h = kind == 0 ? s * .8f : kind == 1 ? s * 2.4f : s * Random.Range(.6f, 1f)
                    };
                    var im = UIKit.Img("Bit", transform, kind == 0 ? UIKit.Circle : null, cols[Random.Range(0, cols.Count)]); im.raycastTarget = false;
                    pc.im = im; pc.rt = im.rectTransform; pc.rt.anchorMin = pc.rt.anchorMax = Vector2.zero; pc.rt.sizeDelta = new Vector2(pc.w, pc.h);
                    ps.Add(pc);
                }
            }
            Place();
        }
        void Update()
        {
            float dt = Time.unscaledDeltaTime; t += dt; float f = Mathf.Min(dt * 60, 3);   // frames at 60 fps
            float drag = Mathf.Pow(.968f, f), g = .26f * .55f * Px * f, vMax = 3.2f * Px;
            for (int i = 0; i < ps.Count; i++)
            {
                var p = ps[i];
                p.v *= drag; p.v.y -= g; if (p.v.y < -vMax) p.v.y = -vMax;
                p.sway += p.swayF * f; p.p += p.v * f + new Vector2(Mathf.Sin(p.sway) * .6f * Px * f * (p.v.y < 0 ? 1 : .2f), 0);
                p.rot += p.vr * f; p.flip += p.vf * f; ps[i] = p;
            }
            Place();
            if (t >= Life) Destroy(gameObject);
        }
        void Place()
        {
            float a = t > Life - Fade ? Mathf.Clamp01((Life - t) / Fade) : 1;
            foreach (var p in ps)
            {
                p.rt.anchoredPosition = p.p; p.rt.localEulerAngles = new Vector3(0, 0, p.rot);
                p.rt.localScale = new Vector3(p.kind == 0 ? 1 : Mathf.Max(.08f, Mathf.Abs(Mathf.Cos(p.flip))), 1, 1);
                var c = p.im.color; c.a = a; p.im.color = c;
            }
        }
    }
}
