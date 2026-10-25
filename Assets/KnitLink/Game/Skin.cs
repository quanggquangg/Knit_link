// Skin – the artist's UI art (Drive "UI - Knit Link"), added 25/10.
// WHERE: Assets/KnitLink/Resources/Skin/UI - Knit Link/... (the artist's folders and file names, NOT changed).
//   To update art: overwrite a file with the same name. A missing file = the game keeps drawing the old in-code picture.
//   Import settings (readable, no mipmaps, no compression) are set by Editor/SkinImporter.cs.
// WHAT THIS FILE DOES: finds a file, turns it into a Sprite (cached) and makes the few pieces the art needs at runtime
//   (the source files are never modified):
//   - Ball(c): yarn ball trimmed and centred on a square, so every color sits the same (the artist's canvases differ)
//   - Basket(c) / BasketFront(c): basket with handle (back) and the front lip (no handle); the taller file is the one with the handle
//   - RopeTile(c) / RopeCorner(c): seamless straight rope (wire1 straightened, one twist period) and a 90° corner bent from it
//   - BagFront: the open bag (bag1) cut below its mouth rim, drawn over balls dropping in
//   - Sliced(path, l, t, r, b): 9-slice sprite for panels and buttons
// Game color -> artist color: teal = cyan, cream = white. Turn the skin off: PlayerPrefs "kl.skin" = "old".
using System.Collections.Generic;
using UnityEngine;

namespace KnitLink
{
    public static class Skin
    {
        const string Root = "Skin/UI - Knit Link/", RootFlat = "Skin/";
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        static int on = -1;

        /// <summary>True when the art is in the project and the skin is not turned off.</summary>
        public static bool On
        {
            get
            {
                if (on < 0) on = PlayerPrefs.GetString("kl.skin", "new") != "old" && Tex("Ingame/Asset/yellow-ball") != null ? 1 : 0;
                return on == 1;
            }
        }
        public static void Reset() { on = -1; cache.Clear(); texCache.Clear(); }

        /// <summary>Artist file name of a game color.</summary>
        public static string ArtColor(string c) => c == "teal" ? "cyan" : c == "cream" ? "white" : c;

        public static Texture2D Tex(string path)
        {
            if (texCache.TryGetValue(path, out var t)) return t;
            t = Resources.Load<Texture2D>(Root + path);
            if (t == null) t = Resources.Load<Texture2D>(RootFlat + path);   // in case the folder contents were copied one level up
            texCache[path] = t; return t;
        }

        /// <summary>The whole file as a sprite (pivot centre). null when the file is missing.</summary>
        public static Sprite Get(string path)
        {
            if (cache.TryGetValue(path, out var s)) return s;
            var t = Tex(path); s = t == null ? null : Make(t, path);
            cache[path] = s; return s;
        }
        /// <summary>9-slice sprite: borders in the file's pixels (left, top, right, bottom).</summary>
        public static Sprite Sliced(string path, float l, float top, float r, float b)
        {
            string key = $"{path}#{l},{top},{r},{b}";
            if (cache.TryGetValue(key, out var s)) return s;
            var t = Tex(path); s = t == null ? null : Make(t, key, new Vector4(l, b, r, top));
            cache[key] = s; return s;
        }
        static Sprite Make(Texture2D t, string name, Vector4 border = default)
        {
            t.wrapMode = TextureWrapMode.Clamp;
            var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
            s.name = name; return s;
        }
        static Sprite FromPixels(string key, Color32[] px, int w, int h)
        {
            // with mipmaps: the art is drawn smaller than its pixels on most screens; without them edges look jagged
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            t.SetPixels32(px); t.Apply(true, true);
            var s = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect); s.name = key; cache[key] = s; return s;
        }
        static Color32[] Pixels(Texture2D t, out int w, out int h)
        {
            w = t.width; h = t.height;
            try { return t.GetPixels32(); } catch { KLog.Warn("Skin", $"'{t.name}' is not readable - check Editor/SkinImporter.cs"); return null; }
        }

        // ---------- yarn ----------
        /// <summary>Yarn ball trimmed and centred on a square canvas (row 0 = bottom in Unity).</summary>
        public static Sprite Ball(string c)
        {
            string key = "ball#" + c; if (cache.TryGetValue(key, out var s)) return s;
            var t = Tex("Ingame/Asset/" + ArtColor(c) + "-ball"); if (t == null) { cache[key] = null; return null; }
            var px = Pixels(t, out int w, out int h); if (px == null) return cache[key] = Get("Ingame/Asset/" + ArtColor(c) + "-ball");
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (px[y * w + x].a > 8) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
            int bw = x1 - x0 + 1, bh = y1 - y0 + 1, S = Mathf.Max(bw, bh) + 4;
            var o = new Color32[S * S]; int ox = (S - bw) / 2, oy = (S - bh) / 2;
            for (int y = 0; y < bh; y++) for (int x = 0; x < bw; x++) o[(y + oy) * S + x + ox] = px[(y + y0) * w + x + x0];
            // the round body without the loose strand: centre of the solid pixels, radius from their area
            long n = 0; double sx = 0, sy = 0;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) if (o[y * S + x].a > 128) { n++; sx += x; sy += y; }
            if (n > 0) body[c] = (new Vector2((float)(sx / n + .5) / S - .5f, (float)(sy / n + .5) / S - .5f), Mathf.Sqrt(n / Mathf.PI) / S);
            return FromPixels(key, o, S, S);
        }
        static readonly Dictionary<string, (Vector2 centre, float radius)> body = new Dictionary<string, (Vector2, float)>();
        /// <summary>Where the round body of the Ball(c) picture is (the strand of yarn makes the picture off-centre):
        /// centre from the picture centre and radius, both as a fraction of the picture size. Used to centre the selection ring.</summary>
        public static (Vector2 centre, float radius) BallBody(string c)
        {
            if (!body.ContainsKey(c)) Ball(c);
            return body.TryGetValue(c, out var b) ? b : (Vector2.zero, .5f);
        }
        public static Sprite Dot(string c) => Get("Ingame/Asset/" + ArtColor(c) + "-color");
        /// <summary>Basket with the handle (the taller of basket1 / basket2).</summary>
        public static Sprite Basket(string c) { var (a, b) = BasketPair(c); return a; }
        /// <summary>Front lip of the basket (no handle), drawn over the yarn inside.</summary>
        public static Sprite BasketFront(string c) { var (a, b) = BasketPair(c); return b; }
        static (Sprite handle, Sprite front) BasketPair(string c)
        {
            var a = Get("Ingame/Asset/" + ArtColor(c) + "-basket1"); var b = Get("Ingame/Asset/" + ArtColor(c) + "-basket2");
            if (a == null || b == null) return (b ?? a, null);
            return a.rect.height / a.rect.width > b.rect.height / b.rect.width ? (a, b) : (b, a);
        }

        // ---------- rope (yarn line between two matched balls) ----------
        /// <summary>Rope core thickness (px) in RopeTile / RopeCorner, and the tile size (both square).</summary>
        public const int RopeCoreTop = 18, RopeCoreBottom = 34, RopeTileSize = 54, RopeCenter = 26;
        /// <summary>Corner piece: 50×50, comes in from the left along row 12, turns down around a quarter circle (radius 22) and leaves at the bottom along column 38. Bend point (38, 12) from the top-left.</summary>
        public const int CornerSize = 50, CornerBendX = 38, CornerBendY = 12, CornerRadius = 22;

        /// <summary>The straight wire file (wider than tall). The artist's red files have the two names swapped, so pick by shape.</summary>
        static Texture2D WireStraight(string c, out Texture2D corner)
        {
            var a = Tex("Ingame/Asset/" + ArtColor(c) + "-wire1"); var b = Tex("Ingame/Asset/" + ArtColor(c) + "-wire2");
            if (a == null || b == null) { corner = null; return a ?? b; }
            if (a.width / (float)a.height >= b.width / (float)b.height) { corner = b; return a; }
            corner = a; return b;
        }
        public static Sprite RopeTile(string c)
        {
            string key = "rope#" + c; if (cache.TryGetValue(key, out var s)) return s;
            var t = WireStraight(c, out _); if (t == null) { cache[key] = null; return null; }
            var px = Pixels(t, out int w, out int h); if (px == null) { cache[key] = null; return null; }
            var tile = SeamlessTile(px, w, h);
            return tile == null ? (cache[key] = null) : FromPixels(key, tile, RopeTileSize, RopeTileSize);
        }
        public static Sprite RopeCorner(string c)
        {
            string key = "corner#" + c; if (cache.TryGetValue(key, out var s)) return s;
            var t = WireStraight(c, out _); if (t == null) { cache[key] = null; return null; }
            var px = Pixels(t, out int w, out int h); if (px == null) { cache[key] = null; return null; }
            var tile = SeamlessTile(px, w, h); if (tile == null) { cache[key] = null; return null; }
            return FromPixels(key, Bend(tile), CornerSize, CornerSize);
        }
        // the artist's wire1 bends up in the middle and its ends do not continue each other:
        // keep only the rope (alpha > 238, no baked glow), straighten it on row 26 (quadratic fit of its centre line),
        // cut one twist period (54 px, from column 10) and cross-fade 8 px so tile after tile shows no cut. Same as prep_skin.py.
        static Color32[] SeamlessTile(Color32[] src, int w, int h)
        {
            const int X0 = 10, L = RopeTileSize, OV = 8, C = RopeCenter;
            if (w < X0 + L + OV) return null;
            // premultiplied float image, top row first, glow removed
            var img = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    var p = src[(h - 1 - y) * w + x]; float a = Mathf.Clamp01((p.a - 238f) / 17f);
                    img[y * w + x] = new Color(p.r / 255f * a, p.g / 255f * a, p.b / 255f * a, a);
                }
            // centre line per column, quadratic fit
            var xs = new List<float>(); var cs = new List<float>();
            for (int x = 0; x < w; x++)
            {
                int top = -1, bot = -1; for (int y = 0; y < h; y++) if (img[y * w + x].a > .5f) { if (top < 0) top = y; bot = y; }
                if (top >= 0) { xs.Add(x); cs.Add((top + bot) / 2f); }
            }
            var fit = Quad(xs, cs);
            var straight = new Color[w * RopeTileSize];
            for (int x = 0; x < w; x++)
            {
                float shift = C - (fit[0] + fit[1] * x + fit[2] * x * x);
                for (int y = 0; y < RopeTileSize; y++)
                {
                    float sy = y - shift; int y0 = Mathf.FloorToInt(sy); float f = sy - y0;
                    Color a = y0 >= 0 && y0 < h ? img[y0 * w + x] : Color.clear, b = y0 + 1 >= 0 && y0 + 1 < h ? img[(y0 + 1) * w + x] : Color.clear;
                    straight[y * w + x] = Color.Lerp(a, b, f);
                }
            }
            var o = new Color32[L * RopeTileSize];
            for (int y = 0; y < RopeTileSize; y++)
                for (int x = 0; x < L; x++)
                {
                    Color p = straight[y * w + X0 + x];
                    if (x < OV) { float k = (x + .5f) / OV; p = p * k + straight[y * w + X0 + L + x] * (1 - k); }
                    o[(RopeTileSize - 1 - y) * L + x] = Unpremul(p);
                }
            return o;
        }
        // quarter-circle corner made from the straight tile, so thickness, centre and twist match exactly
        static Color32[] Bend(Color32[] tile)
        {
            int T = RopeTileSize, S = CornerSize; float R = CornerRadius, arc = Mathf.PI / 2 * R;
            var o = new Color32[S * S];
            for (int yy = 0; yy < S; yy++)
                for (int xx = 0; xx < S; xx++)
                {
                    float px = xx + .5f - CornerBendX, py = yy + .5f - CornerBendY, u, v;   // py grows downwards
                    if (px <= -R) { u = px + R; v = py; }
                    else if (py >= R) { u = arc + (py - R); v = -px; }
                    else { float qx = px + R, qy = py - R, th = Mathf.Clamp(Mathf.Atan2(qy, qx), -Mathf.PI / 2, 0); u = R * (th + Mathf.PI / 2); v = R - Mathf.Sqrt(qx * qx + qy * qy); }
                    float sx = Mathf.Repeat(u, T), sy = RopeCenter + v;
                    o[(S - 1 - yy) * S + xx] = SampleTop(tile, T, sx, sy);
                }
            return o;
        }
        static Color32 SampleTop(Color32[] tile, int T, float x, float y)
        {   // tile is stored bottom row first; x wraps, y does not
            if (y < 0 || y > T - 1) return new Color32(0, 0, 0, 0);
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y); float fx = x - x0, fy = y - y0; int x1 = (x0 + 1) % T, y1 = Mathf.Min(y0 + 1, T - 1); x0 %= T;
            Color P(int xi, int yi) { var c = (Color)tile[(T - 1 - yi) * T + xi]; return new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a); }
            var c0 = Color.Lerp(P(x0, y0), P(x1, y0), fx); var c1 = Color.Lerp(P(x0, y1), P(x1, y1), fx);
            return Unpremul(Color.Lerp(c0, c1, fy));
        }
        static Color32 Unpremul(Color p) => p.a <= 0.001f ? new Color32(0, 0, 0, 0) : (Color32)new Color(p.r / p.a, p.g / p.a, p.b / p.a, p.a);
        static float[] Quad(List<float> xs, List<float> ys)
        {   // least squares y = a + b x + c x²
            double s0 = xs.Count, s1 = 0, s2 = 0, s3 = 0, s4 = 0, t0 = 0, t1 = 0, t2 = 0;
            for (int i = 0; i < xs.Count; i++) { double x = xs[i], y = ys[i], x2 = x * x; s1 += x; s2 += x2; s3 += x2 * x; s4 += x2 * x2; t0 += y; t1 += x * y; t2 += x2 * y; }
            double[,] m = { { s0, s1, s2, t0 }, { s1, s2, s3, t1 }, { s2, s3, s4, t2 } };
            for (int i = 0; i < 3; i++) { for (int j = i + 1; j < 3; j++) { double f = m[j, i] / m[i, i]; for (int k = i; k < 4; k++) m[j, k] -= f * m[i, k]; } }
            var r = new double[3]; for (int i = 2; i >= 0; i--) { double v = m[i, 3]; for (int k = i + 1; k < 3; k++) v -= m[i, k] * r[k]; r[i] = v / m[i, i]; }
            return new[] { (float)r[0], (float)r[1], (float)r[2] };
        }

        // ---------- yarn bag ----------
        /// <summary>Open bag (bag1) cut below the front rim of its mouth: drawn over the balls so they drop INTO the bag.
        /// Rim measured on bag1 at 253×266 (scaled to the real file): (60,36) (90,42) (126,59) (160,53) (190,41).</summary>
        public static Sprite BagFront()
        {
            const string key = "bagfront"; if (cache.TryGetValue(key, out var s)) return s;
            var t = Tex("Ingame/bag1"); if (t == null) { cache[key] = null; return null; }
            var px = Pixels(t, out int w, out int h); if (px == null) { cache[key] = null; return null; }
            float kx = w / 253f, ky = h / 266f;
            var xs = new List<float> { 60 * kx, 90 * kx, 126 * kx, 160 * kx, 190 * kx }; var ys = new List<float> { 36 * ky, 42 * ky, 59 * ky, 53 * ky, 41 * ky };
            var f = Quad(xs, ys); var o = (Color32[])px.Clone();
            for (int x = 0; x < w; x++)
            {
                float xc = Mathf.Clamp(x, 40 * kx, 212 * kx); int edge = Mathf.RoundToInt(f[0] + f[1] * xc + f[2] * xc * xc) - 1;   // rows from the top
                for (int yTop = 0; yTop < Mathf.Min(edge, h); yTop++) o[(h - 1 - yTop) * w + x].a = 0;
            }
            return FromPixels(key, o, w, h);
        }

        // ---------- light (win popup) ----------
        /// <summary>Sunburst: n soft rays of width wDeg, fading out from 14% to 58% of the radius (made in code).</summary>
        public static Sprite Rays(int n, float wDeg, Color col)
        {
            string key = $"rays#{n},{wDeg},{ColorUtility.ToHtmlStringRGBA(col)}"; if (cache.TryGetValue(key, out var s)) return s;
            const int S = 256; var o = new Color32[S * S]; float step = 360f / n;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + .5f) / S - .5f, dy = (y + .5f) / S - .5f, r = Mathf.Sqrt(dx * dx + dy * dy) * 2;
                    float ang = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, step);
                    float ray = Mathf.Clamp01((wDeg - ang) * 2) * Mathf.Clamp01(ang * 2);
                    float fall = r < .14f ? 1 : r < .28f ? Mathf.Lerp(1, .5f, (r - .14f) / .14f) : r < .58f ? Mathf.Lerp(.5f, 0, (r - .28f) / .3f) : 0;
                    var c = col; c.a *= ray * fall; o[y * S + x] = c;
                }
            return FromPixels(key, o, S, S);
        }
        /// <summary>Small 4-point star for sparkles.</summary>
        public static Sprite Sparkle()
        {
            const string key = "sparkle"; if (cache.TryGetValue(key, out var s)) return s;
            const int S = 64; var o = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Abs((x + .5f) / S - .5f) * 2, dy = Mathf.Abs((y + .5f) / S - .5f) * 2;
                    float star = Mathf.Clamp01(1 - (Mathf.Sqrt(dx) + Mathf.Sqrt(dy)) * .9f) * 3, core = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy) * 4);
                    float a = Mathf.Clamp01(star + core); var c = Color.Lerp(new Color(1, .94f, .67f), Color.white, core); c.a = a; o[y * S + x] = c;
                }
            return FromPixels(key, o, S, S);
        }
    }
}
