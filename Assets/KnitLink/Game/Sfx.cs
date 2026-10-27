// Sfx – the game's sound effects, made in code like the web demo (no audio files), soft woody "plucks" (26/10).
//   Each sound is a short list of notes (tone) and puffs of noise (noise), written exactly as in the demo's art.js (AU.*).
//   The clips are built once, the first time a sound plays. They play through the mixer's SFX group (AudioMix.cs, 27/10);
//   Sound on/off in Settings mutes that group.
//
//   Sfx.Play(Sfx.Tap)  … where each sound is used:
//     Tap     select / unselect a yarn ball, Settings switches, language     Link    a pair is matched
//     Clink   a ball lands in the basket, a coin lands in the coin box        Shelf   a ball lands on the shelf
//     Err     blocked: no path, tangle, ice, closed box, locked booster      Pour    a full basket starts knitting
//     Done    the basket finished knitting into the picture                  Boost   a booster is used, auto-finish, yarn bag filled
//     Unlock  booster claimed, tangle cleared, needle box opens              Win     level won        Fail   level lost
//     BagOpen the yarn bag pops up (revive)        BagLift a shelf ball lifts off towards the bag (it lands with Clink)
//     Popper  win confetti (with Win)
//     Hard / SuperHard  the warning ribbon slides in (GameController.HardBanner.cs) - new in Unity, not in the web demo
//   To change a sound: edit its notes in Build() below. Overall loudness: Master.
//   Real audio files win: a file Resources/Sounds/<name> (wav/mp3/ogg, e.g. Sounds/popper.wav) is played instead of the
//   code-made sound of that name. Delete the file to go back to the code-made one.
//   Files now (27/10): every sound above has a mixed WAV in Resources/Sounds, made by Tools/Audio/render_sounds.py (marimba /
//   wool pluck / felt drum, small reverb, matched loudness) + music.wav (background loop). popper.wav = the user's
//   "Confetti Party Popper - Free Sound Effect". The code-made notes below stay as the fallback.
using System.Collections.Generic;
using UnityEngine;

namespace KnitLink
{
    public static class Sfx
    {
        public const string Tap = "tap", Link = "link", Clink = "clink", Shelf = "shelf", Err = "err", Pour = "pour",
            Done = "done", Boost = "boost", Unlock = "unlock", Win = "win", Fail = "fail",
            BagOpen = "bagopen", BagLift = "baglift", Hard = "hard", SuperHard = "superhard", Popper = "popper";
        const int Rate = 44100;
        const float Master = 5.5f;   // the demo's volumes (~0.08) are for a browser; this brings them to a normal level in Unity (peaks ~0.45)

        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static AudioSource src;
        static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

        /// <summary>Play a sound (one of the names above). The same sound is not repeated within 30 ms (many balls landing at once).</summary>
        public static void Play(string name)
        {
            if (!Application.isPlaying) return;
            if (lastPlayed.TryGetValue(name, out float t) && Time.unscaledTime - t < .03f) return;
            lastPlayed[name] = Time.unscaledTime;
            if (src == null)
            {
                var go = new GameObject("Sfx"); Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0;
            }
            if (src.outputAudioMixerGroup == null) src.outputAudioMixerGroup = AudioMix.SfxGroup;
            EnsureListener();
            if (!clips.TryGetValue(name, out var clip)) clips[name] = clip = Resources.Load<AudioClip>("Sounds/" + name) ?? Build(name);
            if (clip != null) src.PlayOneShot(clip);
        }

        /// <summary>Unity plays no sound at all without an AudioListener in the scene (26/10: the Game scene had none, so the game
        /// was silent). If there is none, one is added to the Sfx object. Called by every Play.</summary>
        public static void EnsureListener()
        {
            if (Object.FindAnyObjectByType<AudioListener>() != null) return;
            if (src == null) { var go = new GameObject("Sfx"); Object.DontDestroyOnLoad(go); src = go.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0; }
            src.gameObject.AddComponent<AudioListener>();
            KLog.Warn("Sound", "No AudioListener in the scene - added one (Knit Link > Setup Game Scene now adds it to the camera)");
        }

        // ---------- the sounds (same notes as the web demo) ----------
        static AudioClip Build(string name)
        {
            var s = new Synth();
            switch (name)
            {
                case Tap: s.Tone(520, .08f, Wave.Triangle, .08f); break;
                case Link: s.Tone(440, .1f, Wave.Sine, .09f); s.Tone(660, .16f, Wave.Sine, .08f, .06f); break;
                case Clink: s.Tone(330, .12f, Wave.Triangle, .08f); s.Noise(.12f, .05f, 900); break;
                case Shelf: s.Tone(260, .1f, Wave.Triangle, .08f); s.Noise(.1f, .04f, 700); break;
                case Pour: foreach (float w in new[] { 0f, .08f, .16f, .24f }) s.Noise(.1f, .05f, 1200, w); break;
                case Done: Arp(s, new[] { 523f, 659, 784 }, .22f, .08f, .08f); break;
                case Unlock: Arp(s, new[] { 880f, 1175 }, .16f, .06f, .07f); break;
                case Boost: Arp(s, new[] { 392f, 523, 659, 880 }, .14f, .07f, .05f); break;
                case Err: s.Tone(200, .16f, Wave.Triangle, .06f, 0, 150); break;
                case Fail: Arp(s, new[] { 392f, 311, 247 }, .28f, .08f, .15f); break;
                case Win: Arp(s, new[] { 523f, 659, 784, 1047, 1319 }, .3f, .08f, .09f); break;
                // win confetti (26/10, replaces the old harsh party popper): two soft "pop"s (short pitch drop, no hiss),
                // a quiet paper rustle, then a light sparkle of high notes
                case Popper:
                    foreach (float w in new[] { 0f, .08f }) { s.Tone(240, .1f, Wave.Sine, .13f, w, 70); s.Noise(.04f, .02f, 2500, w); }
                    s.Noise(.45f, .018f, 2200, .06f);
                    Arp(s, new[] { 1568f, 2093, 2637, 3136 }, .16f, .022f, .05f, .14f);
                    break;
                // yarn bag (26/10): a soft rising "pop" when the bag appears; a light pluck as each ball leaves the shelf
                case BagOpen: s.Tone(300, .2f, Wave.Sine, .1f, 0, 620); s.Noise(.16f, .05f, 1500); break;
                case BagLift: s.Tone(620, .07f, Wave.Triangle, .06f, 0, 760); break;
                // hard warnings (26/10): a whoosh as the ribbon slides in, a low drum, then falling "dun-dun" notes
                case Hard:
                    s.Noise(.28f, .05f, 1800); s.Tone(120, .32f, Wave.Sine, .1f, .12f, 60);
                    s.Tone(392, .22f, Wave.Triangle, .08f, .16f); s.Tone(294, .34f, Wave.Triangle, .08f, .38f);
                    break;
                // super hard: same idea, stronger - two drum hits, three falling notes and a short alarm glide on top
                case SuperHard:
                    s.Noise(.3f, .055f, 2000); s.Tone(110, .3f, Wave.Sine, .1f, .12f, 55); s.Tone(110, .36f, Wave.Sine, .1f, .5f, 50);
                    Arp(s, new[] { 466f, 392, 311 }, .26f, .08f, .2f, .16f);
                    s.Tone(700, .3f, Wave.Sine, .045f, .2f, 1000); s.Tone(700, .3f, Wave.Sine, .045f, .55f, 1000);
                    break;
                default: KLog.Warn("Sound", $"Unknown sound '{name}'"); return null;
            }
            return s.ToClip(name);
        }
        /// <summary>Notes one after another, step seconds apart.</summary>
        static void Arp(Synth s, float[] notes, float len, float vol, float step, float start = 0)
        {
            for (int i = 0; i < notes.Length; i++) s.Tone(notes[i], len, Wave.Triangle, vol, start + i * step);
        }

        enum Wave { Sine, Triangle }

        /// <summary>Mixes notes and noise into one mono buffer (same envelopes as Web Audio's exponential ramps).</summary>
        class Synth
        {
            readonly List<float> buf = new List<float>();
            readonly System.Random rnd = new System.Random(11);

            /// <summary>Make the buffer long enough for a sound from `when` lasting d (+20 ms of silence at the end).</summary>
            void Reserve(float when, float d)
            {
                int end = (int)(when * Rate) + (int)(d * Rate) + Rate / 50;
                while (buf.Count < end) buf.Add(0);
            }
            static float Env(float t, float attack, float d, float vol)
            {   // .0001 → vol in `attack` seconds, then → .0001 at d (exponential, like exponentialRampToValueAtTime)
                if (t < attack) return .0001f * Mathf.Pow(vol / .0001f, t / attack);
                return vol * Mathf.Pow(.0001f / vol, (t - attack) / (d - attack));
            }

            /// <summary>A note: frequency f (gliding to f2 if set), length d, starting at `when`.</summary>
            public void Tone(float f, float d, Wave wave, float vol, float when = 0, float f2 = 0)
            {
                Reserve(when, d); int s0 = (int)(when * Rate), n = (int)(d * Rate); double phase = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate, freq = f2 > 0 ? f * Mathf.Pow(f2 / f, t / d) : f;
                    phase += freq / Rate; float p = (float)(phase - System.Math.Floor(phase));
                    float v = wave == Wave.Sine ? Mathf.Sin(2 * Mathf.PI * p) : 1 - 4 * Mathf.Abs(p - .5f);
                    buf[s0 + i] += v * Env(t, .012f, d, vol);
                }
            }
            /// <summary>A puff of noise through a low-pass filter (cut-off freq), length d.</summary>
            public void Noise(float d, float vol, float freq, float when = 0)
            {
                Reserve(when, d); int s0 = (int)(when * Rate), n = (int)(d * Rate); float lp = 0, a = 1 - Mathf.Exp(-2 * Mathf.PI * freq / Rate);
                for (int i = 0; i < n; i++)
                {
                    lp += a * ((float)rnd.NextDouble() * 2 - 1 - lp);
                    buf[s0 + i] += lp * 1.6f * Env((float)i / Rate, .03f, d, vol);
                }
            }
            public AudioClip ToClip(string name)
            {
                var d = buf.ToArray(); for (int i = 0; i < d.Length; i++) d[i] = Mathf.Clamp(d[i] * Master, -1, 1);
                var clip = AudioClip.Create("Sfx_" + name, d.Length, 1, Rate, false); clip.SetData(d, 0); return clip;
            }
        }
    }
}
