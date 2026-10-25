// UIKit – the game's fonts (25/10, same as the web demo): Nunito ExtraBold for all text, Fredoka Bold for titles, buttons and
// big numbers ("display" text). Files: Resources/Fonts (free fonts, OFL licence files next to them).
//   - The weight is in the font file, so these texts use FontStyle.Normal (Unity's fake bold would make them too thick).
//   - Fredoka has no Vietnamese letters: in Vietnamese the display texts use Nunito Black instead (DisplayFont switches by itself).
//   - Missing font files: the built-in Unity font is used, as before.
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public static partial class UIKit
    {
        static Font body, display, displayVi; static bool fontsLoaded;
        static void LoadFonts()
        {
            if (fontsLoaded) return; fontsLoaded = true;
            body = Resources.Load<Font>("Fonts/Nunito-ExtraBold");
            display = Resources.Load<Font>("Fonts/Fredoka-Bold");
            displayVi = Resources.Load<Font>("Fonts/Nunito-Black");
            if (body == null) KLog.Warn("Config", "Font Resources/Fonts/Nunito-ExtraBold not found – using the built-in font");
            if (display == null) KLog.Warn("Config", "Font Resources/Fonts/Fredoka-Bold not found – titles use the body font");
        }
        /// <summary>Font for ordinary text (null = no font file, keep the built-in one).</summary>
        public static Font BodyFont { get { LoadFonts(); return body; } }
        /// <summary>Font for titles / buttons / big numbers in the current language.</summary>
        public static Font DisplayFontNow { get { LoadFonts(); return Loc.Lang == Loc.Vi ? (displayVi ?? body) : (display ?? body); } }

        /// <summary>Make t a title / button / big-number text (Fredoka; Nunito Black in Vietnamese).</summary>
        public static Text Display(Text t)
        {
            if (t != null && DisplayFontNow != null && t.GetComponent<DisplayFont>() == null) t.gameObject.AddComponent<DisplayFont>().Init(t);
            return t;
        }
    }

    /// <summary>Keeps one Text in the display font of the current language.</summary>
    [DisallowMultipleComponent]
    public class DisplayFont : MonoBehaviour
    {
        Text t; int ver = -1;
        public void Init(Text text) { t = text; Apply(); }
        void Apply() { ver = Loc.Version; var f = UIKit.DisplayFontNow; if (f != null && t != null) { t.font = f; if (t.fontStyle == FontStyle.Bold) t.fontStyle = FontStyle.Normal; } }
        void LateUpdate() { if (ver != Loc.Version) Apply(); }
    }
}
