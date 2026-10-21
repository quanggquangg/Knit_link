// Knitted picture renderer (decided 20/10, same look as the web demo):
//   - each stitch keeps its V shape (two "petals"), but the edges and the gaps use the yarn's own hue
//   - a dark contour is drawn only where two different colors meet, so the shape reads first, even when small
//   - stitches not knitted yet show their real color faded ("ghost"); baskets on the row show a bit stronger
// One texture of PictureSize × CellPx pixels; the stitch pattern is computed once and reused.
using System.Collections.Generic;
using UnityEngine;

namespace KnitLink
{
    public static class PictureRenderer
    {
        public const int CellPx = 12;                         // texture pixels per stitch
        public const float Ghost = .36f, GhostNext = .55f;    // faded strength of unknitted stitches
        static readonly Color Base = UIKit.Hex("#e9dfca"), Ink = UIKit.Hex("#3d2f28");
        enum Part : byte { Gap, Petal, PetalEdge, Shine }
        static Part[] pattern;

        public static Texture2D NewTexture()
        {
            int n = LevelJsonIO.PictureSize * CellPx;
            return new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        }

        static void BuildPattern()
        {
            pattern = new Part[CellPx * CellPx];
            for (int py = 0; py < CellPx; py++)
                for (int px = 0; px < CellPx; px++)
                {
                    float x = (px + .5f) / CellPx, y = (py + .5f) / CellPx;   // y = 0 at the top of the stitch
                    var part = Part.Gap;
                    foreach (int side in new[] { -1, 1 })
                    {   // petal: ellipse rx .27, ry .56 around (.5 ± .22, .5), tilted ±.5 rad
                        float cx = .5f + side * .22f, a = side * .5f, dx = x - cx, dy = y - .5f;
                        float lx = dx * Mathf.Cos(a) + dy * Mathf.Sin(a), ly = -dx * Mathf.Sin(a) + dy * Mathf.Cos(a);
                        float r = (lx / .27f) * (lx / .27f) + (ly / .56f) * (ly / .56f);
                        if (r <= 1)
                        {
                            var p = r > .7f ? Part.PetalEdge : Part.Petal;
                            float hx = (lx + .06f) / .07f, hy = (ly + .18f) / .2f;
                            if (hx * hx + hy * hy <= 1) p = Part.Shine;
                            if (part == Part.Gap || p == Part.Petal || p == Part.Shine) part = p;
                        }
                    }
                    pattern[py * CellPx + px] = part;
                }
        }

        /// <summary>Draw the picture. done(k) = stitch k is knitted; next = layers whose basket is on the row.</summary>
        public static void Draw(Texture2D tex, LevelDef L, System.Func<int, bool> done, HashSet<int> next)
        {
            if (pattern == null) BuildPattern();
            int N = LevelJsonIO.PictureSize, W = N * CellPx;
            var px = new Color32[W * W];
            var colOf = new string[N * N]; var isDone = new bool[N * N];
            for (int k = 0; k < N * N; k++) { colOf[k] = L.baskets[L.picture[k]]; isDone[k] = done(k); }
            for (int k = 0; k < N * N; k++)
            {
                var col = UIKit.Colors[colOf[k]]; bool f = isDone[k]; float g = next != null && next.Contains(L.picture[k]) ? GhostNext : Ghost;
                Color gap = Color.Lerp(col.c, col.d, .55f), edge = Color.Lerp(col.c, col.d, .5f), shine = f ? Color.Lerp(col.c, Color.white, .22f) : col.c;
                Color Fade(Color c) => f ? c : Color.Lerp(Base, c, g);
                Color32 cGap = Fade(gap), cPetal = Fade(col.c), cEdge = Fade(edge), cShine = Fade(shine);
                int sx = (k % N) * CellPx, sy = (k / N) * CellPx;
                for (int py = 0; py < CellPx; py++)
                {
                    int row = (W - 1 - (sy + py)) * W + sx;   // texture rows go bottom-up
                    for (int qx = 0; qx < CellPx; qx++)
                    {
                        var part = pattern[py * CellPx + qx];
                        px[row + qx] = part == Part.Gap ? cGap : part == Part.PetalEdge ? cEdge : part == Part.Shine ? cShine : cPetal;
                    }
                }
            }
            // contour where two colors meet: darken the pixel row/column on both sides of the shared edge
            for (int k = 0; k < N * N; k++)
            {
                int x = k % N, y = k / N;
                if (x + 1 < N && colOf[k + 1] != colOf[k]) Contour(px, W, (x + 1) * CellPx, y * CellPx, true, isDone[k] && isDone[k + 1]);
                if (y + 1 < N && colOf[k + N] != colOf[k]) Contour(px, W, x * CellPx, (y + 1) * CellPx, false, isDone[k] && isDone[k + N]);
            }
            tex.SetPixels32(px); tex.Apply();
        }
        static void Contour(Color32[] px, int W, int ex, int ey, bool vertical, bool strong)
        {
            float a = strong ? .62f : .2f;
            for (int i = 0; i < CellPx; i++)
                for (int side = -1; side <= 0; side++)
                {
                    int x = vertical ? ex + side : ex + i, y = vertical ? ey + i : ey + side;
                    if (x < 0 || y < 0 || x >= W || y >= W) continue;
                    int idx = (W - 1 - y) * W + x; px[idx] = Color.Lerp(px[idx], Ink, a);
                }
        }
    }
}
