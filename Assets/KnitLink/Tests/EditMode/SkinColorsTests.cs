// "The yarn ball and the knitted picture are the same color" (user 26/10).
// The picture, the strand, rings and confetti use UIKit.SkinColors; the balls on the board are the artist's pictures.
// This test measures each ball picture (Resources/Skin/.../Ingame/Asset/<color>-ball.png) the same way the table was made and
// fails when a line of UIKit.SkinColors no longer matches its ball - e.g. the artist repainted a ball and the table was not updated.
// To fix: copy the "measured" value from the failure message into UIKit.SkinColors (d = c × 0.72, l = c + 42% white).
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace KnitLink.Tests
{
    public class SkinColorsTests
    {
        const float Tolerance = 6f / 255f;   // per channel; the table is rounded to whole hex values

        static IEnumerable<string> GameColors() => ColorCodes.ByLetter.Values.OrderBy(c => c);

        [Test] public void EveryGameColorHasASkinColor()
        {
            foreach (var c in GameColors()) Assert.IsTrue(UIKit.SkinColors.ContainsKey(c), $"UIKit.SkinColors has no line for '{c}'");
        }

        [TestCaseSource(nameof(GameColors))]
        public void PictureColorMatchesBallArt(string color)
        {
            string file = "Ingame/Asset/" + Skin.ArtColor(color) + "-ball";
            var tex = Skin.Tex(file);
            if (tex == null) Assert.Ignore($"No art '{file}' in Resources/Skin - the game uses the classic colors");
            Color32[] px;
            try { px = tex.GetPixels32(); } catch { Assert.Fail($"'{file}' is not readable - check Editor/SkinImporter.cs"); return; }

            var measured = Measure(px);
            var table = UIKit.SkinColors[color].c;
            string msg = $"{color} ({file}.png): table #{ColorUtility.ToHtmlStringRGB(table)}, measured #{ColorUtility.ToHtmlStringRGB(measured)}";
            Assert.LessOrEqual(Mathf.Abs(table.r - measured.r), Tolerance, msg);
            Assert.LessOrEqual(Mathf.Abs(table.g - measured.g), Tolerance, msg);
            Assert.LessOrEqual(Mathf.Abs(table.b - measured.b), Tolerance, msg);
        }

        /// <summary>The ball's main tone: solid pixels sorted by brightness, average of the 45%..80% band
        /// (skips the dark shadow side and the bright highlights).</summary>
        static Color Measure(Color32[] px)
        {
            var solid = px.Where(p => p.a > 200).OrderBy(p => p.r + p.g + p.b).ToList();
            int from = (int)(solid.Count * .45f), to = (int)(solid.Count * .8f);
            float r = 0, g = 0, b = 0; int n = 0;
            for (int i = from; i < to; i++) { r += solid[i].r; g += solid[i].g; b += solid[i].b; n++; }
            return new Color(r / n / 255f, g / n / 255f, b / n / 255f);
        }
    }
}
