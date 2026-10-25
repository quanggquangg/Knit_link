// LocText – keeps one UI Text in the chosen language (added 23/10). UIKit.Txt adds it to every Text it makes.
//   The game code keeps writing English into text.text; LocText notices the change (LateUpdate), remembers that English
//   and shows Loc.T(english) instead. When the language changes it re-translates (or restores the English).
//   Texts that must never be translated (the "English" / "Tiếng Việt" buttons): call LocText.Fixed(text).
//   Translations live in Core/Loc.cs. Language is saved in PlayerPrefs "kl.lang".
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    [DisallowMultipleComponent]
    public class LocText : MonoBehaviour
    {
        public const string PrefKey = "kl.lang";
        Text t; string src, shown; int ver = -1; bool upper;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadLang() => Loc.Set(PlayerPrefs.GetString(PrefKey, Loc.En));

        /// <summary>Change and save the language. Texts on screen update on the next frame.</summary>
        public static void SetLang(string lang)
        {
            Loc.Set(lang); PlayerPrefs.SetString(PrefKey, Loc.Lang); PlayerPrefs.Save();
            KLog.Info("Config", $"Language = {Loc.Lang}");
        }

        /// <summary>Called by UIKit.Txt right after the Text is made: translate at once so sizes measured right away are right.</summary>
        public static void Attach(Text text)
        {
            var l = text.gameObject.AddComponent<LocText>(); l.t = text; l.Apply(text.text);
        }
        /// <summary>Show this Text in CAPITALS (after translating), e.g. the picture name under "Level N".</summary>
        public static void Upper(Text text) { var l = text.GetComponent<LocText>(); if (l != null) { l.upper = true; l.Apply(text.text != l.shown ? text.text : l.src); } }
        /// <summary>This Text is never translated.</summary>
        public static void Fixed(Text text) { var l = text.GetComponent<LocText>(); if (l != null) { text.text = l.src ?? text.text; l.enabled = false; Destroy(l); } }

        void Apply(string english) { src = english; ver = Loc.Version; shown = upper ? Loc.T(src).ToUpperInvariant() : Loc.T(src); if (!ReferenceEquals(t.text, shown)) t.text = shown; }

        void LateUpdate()
        {
            if (t == null) return;
            var now = t.text;
            if (!ReferenceEquals(now, shown) && now != shown) Apply(now);   // the game wrote new (English) text
            else if (ver != Loc.Version) Apply(src);                        // language changed
        }
    }
}
