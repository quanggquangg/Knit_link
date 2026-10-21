// Procedural icons for the approved UI (design canvas, 20/10). All are drawn in code so the game runs without art.
// When the real art arrives, replace the sprite returned by each property – callers do not change.
// Coordinates: u,v in 0..1 with v = 0 at the TOP (see Make).
using UnityEngine;

namespace KnitLink
{
    public static partial class UIKit
    {
        // ---------- small SDF helpers (negative = inside) ----------
        static float SegD(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay, wx = px - ax, wy = py - ay;
            float t = Mathf.Clamp01((wx * vx + wy * vy) / (vx * vx + vy * vy));
            float dx = wx - vx * t, dy = wy - vy * t; return Mathf.Sqrt(dx * dx + dy * dy);
        }
        static float BoxD(float px, float py, float cx, float cy, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(px - cx) - hx + r, qy = Mathf.Abs(py - cy) - hy + r;
            return Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }
        static float TriInside(float px, float py, Vector2 a, Vector2 b, Vector2 c)
        {
            float s(Vector2 p1, Vector2 p2) => (px - p2.x) * (p1.y - p2.y) - (p1.x - p2.x) * (py - p2.y);
            float d1 = s(a, b), d2 = s(b, c), d3 = s(c, a);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return neg && pos ? 0 : 1;
        }
        static Color Over(Color under, Color top, float a) { var c = Color.Lerp(under, top, a * top.a); c.a = Mathf.Max(under.a, a * top.a); return c; }
        const float AA = .012f;

        /// <summary>Settings gear (white mask).</summary>
        public static Sprite IconGear => Make("gear", 128, (u, v) =>
        {
            float x = u - .5f, y = v - .5f, d = Mathf.Sqrt(x * x + y * y), ang = Mathf.Atan2(y, x);
            float teeth = Mathf.Cos(ang * 8) > .2f ? .43f : .33f;
            float a = Edge(d, teeth, AA) * (1 - Edge(d, .15f, AA));
            return new Color(1, 1, 1, a);
        });
        /// <summary>Retry: circular arrow (white mask).</summary>
        public static Sprite IconRetry => Make("retry", 128, (u, v) =>
        {
            float x = u - .5f, y = v - .5f, d = Mathf.Sqrt(x * x + y * y), ang = Mathf.Atan2(-y, x) * Mathf.Rad2Deg;   // ang: 0 = right, 90 = up
            float ring = Edge(Mathf.Abs(d - .3f), .065f, AA);
            if (ang > 95 && ang < 150) ring = 0;            // gap at the top-left where the head sits
            float head = TriInside(u, v, new Vector2(.12f, .22f), new Vector2(.42f, .2f), new Vector2(.22f, .48f));
            return new Color(1, 1, 1, Mathf.Max(ring, head));
        });
        /// <summary>Close cross (white mask).</summary>
        public static Sprite IconCross => Make("cross", 96, (u, v) =>
        {
            float d = Mathf.Min(SegD(u, v, .26f, .26f, .74f, .74f), SegD(u, v, .74f, .26f, .26f, .74f));
            return new Color(1, 1, 1, Edge(d, .075f, AA));
        });
        /// <summary>Padlock for locked shelf slots (white mask).</summary>
        public static Sprite IconLock => Make("lock", 96, (u, v) =>
        {
            float body = Edge(BoxD(u, v, .5f, .66f, .3f, .21f, .06f), 0, AA) * (1 - Edge(BoxD(u, v, .5f, .66f, .22f, .13f, .03f), 0, AA));
            float sx = u - .5f, sy = v - .4f, sd = Mathf.Sqrt(sx * sx + sy * sy);
            float shackle = v < .45f ? Edge(Mathf.Abs(sd - .17f), .05f, AA) : (Mathf.Abs(Mathf.Abs(u - .5f) - .17f) < .05f && v < .5f ? 1 : 0);
            return new Color(1, 1, 1, Mathf.Max(body, shackle));
        });
        /// <summary>Rewarded-ad icon: white rounded screen with a green play triangle.</summary>
        public static Sprite IconAd => Make("ad", 96, (u, v) =>
        {
            float box = Edge(BoxD(u, v, .5f, .5f, .45f, .36f, .14f), 0, AA); if (box <= 0) return Color.clear;
            float tri = TriInside(u, v, new Vector2(.4f, .32f), new Vector2(.4f, .68f), new Vector2(.68f, .5f));
            var c = Color.Lerp(Color.white, Hex("#2f8a3d"), tri); c.a = box; return c;
        });
        /// <summary>Gold coin with a darker rim.</summary>
        public static Sprite IconCoin => Make("coin", 96, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)); float a = Edge(d, .47f, AA); if (a <= 0) return Color.clear;
            var c = d > .37f ? Hex("#c58b26") : Color.Lerp(Hex("#ffe9a0"), Hex("#f6c94f"), Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(.38f, .34f)) * 2.2f));
            c.a = a; return c;
        });
        /// <summary>Down arrow, dark outline + white core (rotate it for other directions).</summary>
        public static Sprite IconArrowDown => Make("arrowdown", 128, (u, v) =>
        {
            float d = Mathf.Min(SegD(u, v, .5f, .12f, .5f, .74f), Mathf.Min(SegD(u, v, .25f, .52f, .5f, .77f), SegD(u, v, .75f, .52f, .5f, .77f)));
            float outer = Edge(d, .1f, AA), inner = Edge(d, .05f, AA); if (outer <= 0) return Color.clear;
            var c = Color.Lerp(Hex("#3d2f28"), Color.white, inner); c.a = outer; return c;
        });
        public static Sprite IconSpeaker => Make("speaker", 96, (u, v) =>
        {
            float box = Edge(BoxD(u, v, .22f, .5f, .1f, .13f, .02f), 0, AA);
            float cone = TriInside(u, v, new Vector2(.22f, .5f), new Vector2(.5f, .18f), new Vector2(.5f, .82f));
            float x = u - .5f, y = v - .5f, d = Mathf.Sqrt(x * x + y * y);
            float waves = u > .58f ? Mathf.Max(Edge(Mathf.Abs(d - .2f), .035f, AA), Edge(Mathf.Abs(d - .34f), .035f, AA)) * (Mathf.Abs(y) < (u - .5f) * 1.3f ? 1 : 0) : 0;
            return new Color(1, 1, 1, Mathf.Max(Mathf.Max(box, cone), waves));
        });
        public static Sprite IconNote => Make("note", 96, (u, v) =>
        {
            float h1 = Edge(Vector2.Distance(new Vector2(u, v), new Vector2(.3f, .74f)), .12f, AA), h2 = Edge(Vector2.Distance(new Vector2(u, v), new Vector2(.72f, .66f)), .12f, AA);
            float s1 = Edge(SegD(u, v, .4f, .2f, .4f, .74f), .04f, AA), s2 = Edge(SegD(u, v, .82f, .14f, .82f, .66f), .04f, AA), bar = Edge(SegD(u, v, .4f, .22f, .82f, .14f), .06f, AA);
            return new Color(1, 1, 1, Mathf.Max(Mathf.Max(h1, h2), Mathf.Max(Mathf.Max(s1, s2), bar)));
        });
        public static Sprite IconPhone => Make("phone", 96, (u, v) =>
        {
            float body = Edge(BoxD(u, v, .5f, .5f, .2f, .38f, .07f), 0, AA) * (1 - Edge(BoxD(u, v, .5f, .5f, .13f, .3f, .03f), 0, AA));
            float w1 = Edge(SegD(u, v, .14f, .36f, .14f, .64f), .035f, AA), w2 = Edge(SegD(u, v, .86f, .36f, .86f, .64f), .035f, AA);
            return new Color(1, 1, 1, Mathf.Max(body, Mathf.Max(w1, w2)));
        });

        // ---------- yarn bag (two layers so balls can drop INTO it: back = handle + mouth, front = body + rim) ----------
        static readonly Color BagDark = Hex("#6c4aa3"), BagMouth = Hex("#4a2f6e"), BagBody = Hex("#9670cf"), BagRim = Hex("#c2a8ea"), BagStitch = Hex("#f3e6ff");
        /// <summary>Back of the yarn bag: handle arc + dark mouth. Same box as BagFront (120×120 design units).</summary>
        public static Sprite BagBack => Make("bagback", 192, (u, v) =>
        {
            float x = u * 120, y = v * 120; var col = Color.clear;
            float hd = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(60, 36)) - 20); if (y < 37) col = Over(col, BagDark, Edge(hd, 3.5f, 1.2f));
            float e = ((x - 60) / 35) * ((x - 60) / 35) + ((y - 40) / 9) * ((y - 40) / 9); col = Over(col, BagMouth, Edge(e, 1, .06f));
            return col;
        });
        /// <summary>Front of the yarn bag: body with dashed stitches and a light rim band.</summary>
        public static Sprite BagFront => Make("bagfront", 192, (u, v) =>
        {
            float x = u * 120, y = v * 120, dx = (x - 60) / 36f;
            float top = 41 + 12 * (1 - dx * dx), half = 36 - (y - 41) * .09f;
            float inside = Mathf.Min(Edge(Mathf.Abs(x - 60), half, 1.2f), Edge(top - y, 0, 1.2f) * Edge(y, 115, 1.2f));
            if (y > 103) inside *= Edge(Vector2.Distance(new Vector2(Mathf.Clamp(x, 43, 77), y), new Vector2(Mathf.Clamp(x, 43, 77), 103)), 12, 1.2f);
            if (inside <= 0) return Color.clear;
            var c = BagBody;
            float edgeD = Mathf.Min(half - Mathf.Abs(x - 60), 115 - y); if (edgeD < 3) c = BagDark;
            if (y - top < 5) c = BagRim;
            bool stitch = (Mathf.Abs(y - (64 + 4 * (1 - dx * dx))) < 1.2f || Mathf.Abs(y - (88 + 4 * (1 - dx * dx))) < 1.2f) && Mathf.Repeat(x, 10) < 5 && Mathf.Abs(x - 60) < 26;
            if (stitch) c = Color.Lerp(c, BagStitch, .75f);
            c.a = inside; return c;
        });
        /// <summary>Small bag icon for the chip next to "SHELF".</summary>
        public static Sprite BagChipIcon => Make("bagchip", 96, (u, v) =>
        {
            float x = u * 64, y = v * 64; var col = Color.clear;
            float hd = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(32, 20)) - 12); if (y < 21) col = Over(col, BagStitch, Edge(hd, 2.5f, 1));
            float half = 22 - (y - 22) * .12f; float a = Edge(Mathf.Abs(x - 32), half, 1) * Edge(22 - y, 0, 1) * Edge(y, 59, 1);
            col = Over(col, Mathf.Abs(x - 32) > half - 3 || y > 56 || y < 25 ? BagStitch : BagRim, a);
            return col;
        });
    }
}
