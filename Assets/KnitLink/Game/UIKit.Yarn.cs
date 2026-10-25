// UIKit – yarn pictures that follow the skin (25/10): with the artist's art (Game/Skin.cs) a ball is the artist's ball of
// that color (white tint); without it, the code-drawn grey ball tinted with the color (as before).
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public static partial class UIKit
    {
        /// <summary>Make im show a yarn ball of color c.</summary>
        public static void Yarn(Image im, string c)
        {
            var s = Skin.On ? Skin.Ball(c) : null;
            if (s != null) { im.sprite = s; im.type = Image.Type.Simple; im.color = new Color(1, 1, 1, im.color.a); }
            else { im.sprite = Ball; var k = C(c); k.a = im.color.a; im.color = k; }
        }
        public static Image YarnImg(string name, Transform parent, string c)
        {
            var im = Img(name, parent, Ball, Color.white); Yarn(im, c); return im;
        }
        /// <summary>Image of a skin file (null sprite when the file is missing).</summary>
        public static Image SkinImg(string name, Transform parent, string path, bool raycast = false)
        {
            var im = Img(name, parent, Skin.Get(path), Color.white, raycast); return im;
        }
        /// <summary>White text with a colored outline (titles on ribbons, "+20").</summary>
        public static void Outlined(Text t, Color outline, float px = 5)
        {
            // round outline (UIKit.Effects.cs) + a darker hard shadow below it, like the web demo's text-stroke + text-shadow
            var o = t.gameObject.AddComponent<RoundOutline>(); o.color = outline; o.width = px;
            var sh = t.gameObject.AddComponent<SoftShadow>(); sh.color = new Color(outline.r * .7f, outline.g * .7f, outline.b * .7f, 1); sh.offset = new Vector2(0, -px * .9f); sh.blur = px * .25f;
        }
    }
}
