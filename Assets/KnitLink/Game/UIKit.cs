// Procedural sprites and small UI helpers so the core game runs without any art assets.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public static partial class UIKit
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = BodyFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");   // Nunito ExtraBold (UIKit.Fonts.cs)
                return font;
            }
        }

        /// <summary>Yarn colors in use: c = main, d = dark (shade, gaps), l = light (shine). With the artist's art (Skin.On) these are
        /// the colors of the artist's balls, so the picture, the strand, rings and confetti match the balls on the board (26/10).</summary>
        public static Dictionary<string, (Color c, Color d, Color l, string name)> Colors => Skin.On ? SkinColors : ClassicColors;

        /// <summary>Colors measured from the artist's ball pictures (28/10: the new set Resources/Skin/.../Item fix/&lt;color&gt;-ball.png, the middle
        /// tone of the ball). If the artist changes a ball's color, update its line here (d = c × 0.72, l = c + 42% white).
        /// Checked by Tests/EditMode/SkinColorsTests.cs: the test fails when a line no longer matches its ball picture.</summary>
        public static readonly Dictionary<string, (Color c, Color d, Color l, string name)> SkinColors = new Dictionary<string, (Color c, Color d, Color l, string name)>
        {
            { "red",    (Hex("#fa402a"), Hex("#b42e1e"), Hex("#fc9083"), "Red") },
            { "orange", (Hex("#ec854f"), Hex("#aa6039"), Hex("#f4b899"), "Orange") },
            { "yellow", (Hex("#f7d314"), Hex("#b2980e"), Hex("#fae577"), "Yellow") },
            { "green",  (Hex("#38a94a"), Hex("#287a35"), Hex("#8ccd96"), "Green") },
            { "teal",   (Hex("#3cb4f1"), Hex("#2b82ae"), Hex("#8ed4f7"), "Teal") },     // art file: cyan-ball
            { "blue",   (Hex("#3757d2"), Hex("#283f97"), Hex("#8b9ee5"), "Blue") },
            { "purple", (Hex("#9f4df4"), Hex("#7237b0"), Hex("#c798f9"), "Purple") },
            { "pink",   (Hex("#f55fb4"), Hex("#b04482"), Hex("#f9a2d4"), "Pink") },
            { "cream",  (Hex("#dcd9d6"), Hex("#9e9c9a"), Hex("#ebe9e7"), "Cream") },     // art file: white-ball
        };

        /// <summary>The old code-drawn look (no artist art, or the skin turned off).</summary>
        static readonly Dictionary<string, (Color c, Color d, Color l, string name)> ClassicColors = new Dictionary<string, (Color c, Color d, Color l, string name)>
        {
            { "red",    (Hex("#d9566b"), Hex("#a5384d"), Hex("#ef8d9e"), "Red") },
            { "orange", (Hex("#ec8a3b"), Hex("#b9631f"), Hex("#f8b77a"), "Orange") },
            { "yellow", (Hex("#e8b83a"), Hex("#b88a1c"), Hex("#f6d77f"), "Yellow") },
            { "green",  (Hex("#69a865"), Hex("#467d44"), Hex("#9fcf98"), "Green") },
            { "teal",   (Hex("#36a39c"), Hex("#20766f"), Hex("#7fd0c8"), "Teal") },
            { "blue",   (Hex("#4a7fd6"), Hex("#2f5aa4"), Hex("#8fb3ec"), "Blue") },
            { "purple", (Hex("#9670cf"), Hex("#6c4aa3"), Hex("#c2a8ea"), "Purple") },
            { "pink",   (Hex("#ec88b0"), Hex("#bf5d86"), Hex("#f6bdd3"), "Pink") },
            { "cream",  (Hex("#efe1c2"), Hex("#b59c6c"), Hex("#fff8e8"), "Cream") },
        };
        public static readonly Color Navy = Hex("#1d2a52"), Cream = Hex("#f6ecd8"), Ink = Hex("#3b2a1e"), InkSoft = Hex("#7a6450"),
            Honey = Hex("#e9a93c"), Wood = Hex("#a87650"), WoodDark = Hex("#6b4a2e"), Undyed = Hex("#b2a492"), Knot = Hex("#c4b296");

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        public static Color C(string name) => Colors.TryGetValue(name, out var v) ? v.c : Color.gray;

        static Sprite Make(string key, int size, System.Func<float, float, Color> px, Vector4 border = default)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var cols = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    cols[y * size + x] = px((x + .5f) / size, 1f - (y + .5f) / size);   // u,v with v=0 at top
            tex.SetPixels(cols); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
            s.name = key;
            cache[key] = s; return s;
        }

        static float Edge(float d, float r, float aa) => Mathf.Clamp01((r - d) / aa);

        /// <summary>Soft filled circle (white, tint with Image.color).</summary>
        public static Sprite Circle => Make("circle", 128, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f));
            return new Color(1, 1, 1, Edge(d, .49f, .012f));
        });

        /// <summary>Soft round glow: white in the middle, fading to nothing at 70% of the radius (web demo: radial-gradient puff).</summary>
        public static Sprite SoftGlow => Make("softglow", 128, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) * 2;
            return new Color(1, 1, 1, Mathf.Clamp01(1 - d / .7f) * .95f);
        });

        public static Sprite Ring => Make("ring", 128, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f));
            return new Color(1, 1, 1, Edge(d, .49f, .012f) * (1 - Edge(d, .41f, .012f)));
        });

        /// <summary>Grayscale yarn ball: light from top-left, wound grooves. Tint with the yarn color.</summary>
        public static Sprite Ball => Make("ball", 160, (u, v) =>
        {
            var p = new Vector2(u - .5f, v - .5f); float d = p.magnitude;
            float a = Edge(d, .46f, .01f); if (a <= 0) return Color.clear;
            float light = 1.12f - 0.75f * Vector2.Distance(new Vector2(u, v), new Vector2(.36f, .3f));
            float g = 0;
            for (int i = 0; i < 4; i++)          // diagonal winding grooves
            {
                float off = -.22f + i * .15f, curve = v - (off + .9f * u - .35f * u * u);
                g = Mathf.Max(g, Edge(Mathf.Abs(curve), .012f, .01f));
            }
            for (int i = 0; i < 2; i++)
            {
                float off = .28f + i * .3f, curve = u - (off + .25f * (v - .5f) * (v - .5f) * 4);
                g = Mathf.Max(g, .6f * Edge(Mathf.Abs(curve), .01f, .01f));
            }
            float rim = Mathf.Clamp01((d - .38f) / .08f) * .35f;
            float k = Mathf.Clamp01(light - g * .38f - rim);
            return new Color(k, k, k, a);
        });

        /// <summary>Undyed ball peeking out behind a front ball, with a light badge in the visible corner for the "?".</summary>
        public static Sprite BackBall => Make("backball", 128, (u, v) =>
        {
            var c = new Vector2(.5f, .5f); float d = Vector2.Distance(new Vector2(u, v), c);
            float a = Edge(d, .46f, .012f); if (a <= 0) return Color.clear;
            var col = Undyed * (1.05f - .3f * Vector2.Distance(new Vector2(u, v), new Vector2(.35f, .3f)));
            float bd = Vector2.Distance(new Vector2(u, v), new Vector2(.67f, .3f));
            if (bd < .2f) col = Color.Lerp(col, new Color(1f, .98f, .94f), Edge(bd, .19f, .015f));
            if (d > .43f) col *= .8f;
            col.a = a; return col;
        });

        /// <summary>Messy knot of undyed wool. loose = half untangled.</summary>
        public static Sprite KnotSprite(bool loose) => Make(loose ? "knot1" : "knot2", 128, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f));
            float r = loose ? .36f : .43f;
            float a = Edge(d, r, .012f); if (a <= 0) return Color.clear;
            Color baseC = loose ? Hex("#d9ccb8") : Knot, line = loose ? Hex("#ad987a") : Hex("#8a7356");
            float g = 0; int n = loose ? 3 : 6;
            for (int i = 0; i < n; i++)
            {
                float ang = i * 1.7f, cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                float x = (u - .5f) * cs - (v - .5f) * sn, y = (u - .5f) * sn + (v - .5f) * cs;
                float curve = y - .18f * Mathf.Sin(x * 9 + i);
                g = Mathf.Max(g, Edge(Mathf.Abs(curve), .018f, .012f));
            }
            var col = Color.Lerp(baseC, line, g); col.a = a; return col;
        });

        /// <summary>Tutorial hand: index finger pointing up (tip at u=.44, v=.07), white glove with a dark outline.</summary>
        public static Sprite Hand => Make("hand", 160, (u, v) =>
        {
            float Seg(float px, float py, float ax, float ay, float bx, float by, float r)
            {
                float vx = bx - ax, vy = by - ay, wx = px - ax, wy = py - ay;
                float t = Mathf.Clamp01((wx * vx + wy * vy) / (vx * vx + vy * vy));
                float dx = wx - vx * t, dy = wy - vy * t; return Mathf.Sqrt(dx * dx + dy * dy) - r;
            }
            float Box(float px, float py, float hx, float hy, float r)
            {
                float qx = Mathf.Abs(px) - hx + r, qy = Mathf.Abs(py) - hy + r;
                return Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
            }
            float d = Mathf.Min(Seg(u, v, .44f, .16f, .44f, .6f, .085f), Box(u - .57f, v - .74f, .21f, .2f, .09f));
            d = Mathf.Min(d, Seg(u, v, .6f, .5f, .6f, .62f, .07f)); d = Mathf.Min(d, Seg(u, v, .73f, .54f, .73f, .64f, .065f));
            if (d > .012f) return Color.clear;
            float a = Edge(d, 0, .012f);
            var col = d > -.03f ? Ink : new Color(1f, .98f, .94f);
            col.a = a; return col;
        });

        /// <summary>Rounded rectangle for 9-slicing (white).</summary>
        public static Sprite Rounded => Make("rounded", 96, (u, v) =>
        {
            float R = .33f, x = Mathf.Abs(u - .5f), y = Mathf.Abs(v - .5f);
            float qx = Mathf.Max(x - (.5f - R), 0), qy = Mathf.Max(y - (.5f - R), 0);
            float d = Mathf.Sqrt(qx * qx + qy * qy);
            return new Color(1, 1, 1, Edge(d, R - .01f, .012f));
        }, new Vector4(32, 32, 32, 32));

        // ---------- object helpers ----------
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = new Vector2(x + w / 2, -(y + h / 2)); rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Image Img(string name, Transform parent, Sprite sp, Color col, bool raycast = false)
        {
            var rt = Rect(name, parent); var im = rt.gameObject.AddComponent<Image>();
            im.sprite = sp; im.color = col; im.raycastTarget = raycast;
            if (sp != null && sp.name == "rounded") im.type = Image.Type.Sliced;
            return im;
        }

        public static Text Txt(string name, Transform parent, string text, int size, Color col, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var rt = Rect(name, parent); var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = col; t.alignment = anchor;
            t.fontStyle = style == FontStyle.Bold && BodyFont != null ? FontStyle.Normal : style;   // the font file is already extra bold
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            LocText.Attach(t);   // English / Tiếng Việt (Core/Loc.cs)
            return t;
        }

        public static Button Btn(string name, Transform parent, string label, Color bg, Color fg, int size, System.Action onClick)
        {
            var im = Img(name, parent, Rounded, bg, true);
            var b = im.gameObject.AddComponent<Button>().WithHaptic();
            b.onClick.AddListener(() => onClick());
            var t = Txt("Label", im.transform, label, size, fg);
            Stretch((RectTransform)t.transform);
            return b;
        }

        public static void Stretch(RectTransform rt, float pad = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }
    }
}
