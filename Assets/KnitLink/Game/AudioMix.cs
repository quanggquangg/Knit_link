// AudioMix – the game's audio mixer and background music (27/10).
//   Mixer asset: Resources/Audio/KnitLinkMixer.mixer (open it in Window > Audio > Audio Mixer to listen and adjust live)
//     Master ─┬─ Music   exposed volume "MusicVolume"
//             └─ SFX     exposed volume "SfxVolume"
//   Every sound effect (Sfx.cs) plays through SFX, the background music through Music.
//   Settings: Sound switch = SFX on/off, Music switch = Music on/off (GameController.Popups.cs ApplySettings).
//   Music: Resources/Sounds/music.wav, looped, starts with the game. It is turned down (ducked) while a win / lose popup
//   is open so the win sounds stand out, and comes back when the next level starts.
//   Levels below in dB: 0 = as recorded, -80 = silent.
using UnityEngine;
using UnityEngine.Audio;

namespace KnitLink
{
    public static class AudioMix
    {
        public const float MusicLevel = -6f;     // music sits under the sound effects
        public const float MusicDucked = -16f;   // music while a win / lose popup is open
        public const float SfxLevel = 0f;
        const float Off = -80f, FadeDbPerSecond = 30f;   // music fades: -6 → -16 dB in 1/3 s

        static AudioMixer mixer; static bool loaded;
        static AudioSource music; static bool soundOn = true, musicOn = true, ducked;
        static float musicNow = MusicLevel;

        public static AudioMixer Mixer { get { if (!loaded) { loaded = true; mixer = Resources.Load<AudioMixer>("Audio/KnitLinkMixer"); if (mixer == null) KLog.Warn("Sound", "Resources/Audio/KnitLinkMixer.mixer not found - sounds play without the mixer"); } return mixer; } }
        static AudioMixerGroup Group(string name) { var m = Mixer; if (m == null) return null; var g = m.FindMatchingGroups(name); return g.Length > 0 ? g[0] : null; }
        public static AudioMixerGroup SfxGroup => Group("SFX");
        public static AudioMixerGroup MusicGroup => Group("Music");

        /// <summary>Called from Settings: Sound / Music switches.</summary>
        public static void Apply(bool sound, bool musicSwitch)
        {
            soundOn = sound; musicOn = musicSwitch;
            if (Mixer == null) { AudioListener.volume = sound ? 1 : 0; return; }   // no mixer: old behaviour (one switch for all)
            AudioListener.volume = 1;
            Mixer.SetFloat("SfxVolume", soundOn ? SfxLevel : Off);
            SetMusic(Target());
            if (musicOn) StartMusic();
        }

        /// <summary>Start the looping background music (does nothing if it already plays or the file is missing).</summary>
        public static void StartMusic()
        {
            if (!Application.isPlaying || !musicOn) return;
            if (music == null)
            {
                var clip = Resources.Load<AudioClip>("Sounds/music"); if (clip == null) { KLog.Warn("Sound", "Resources/Sounds/music not found - no background music"); return; }
                var go = new GameObject("Music"); Object.DontDestroyOnLoad(go);
                music = go.AddComponent<AudioSource>(); music.clip = clip; music.loop = true; music.playOnAwake = false; music.spatialBlend = 0;
                music.outputAudioMixerGroup = MusicGroup; music.priority = 0;
            }
            if (!music.isPlaying) music.Play();
        }

        /// <summary>Turn the music down (win / lose popup) or back up (next level).</summary>
        public static void Duck(bool on) { ducked = on; SetMusic(Target()); }

        static float Target() => !musicOn ? Off : ducked ? MusicDucked : MusicLevel;

        // smooth change of the music level (unscaled time, works while the game is paused)
        static Fader fader;
        static void SetMusic(float db)
        {
            if (Mixer == null) return;
            if (!Application.isPlaying) { Mixer.SetFloat("MusicVolume", db); musicNow = db; return; }
            if (fader == null) { var go = new GameObject("MusicFader"); Object.DontDestroyOnLoad(go); fader = go.AddComponent<Fader>(); }
            fader.to = db;
        }
        class Fader : MonoBehaviour
        {
            public float to = MusicLevel;
            void Update()
            {
                if (Mathf.Approximately(musicNow, to)) return;
                musicNow = Mathf.MoveTowards(musicNow, to, Time.unscaledDeltaTime * FadeDbPerSecond);
                Mixer.SetFloat("MusicVolume", musicNow);
            }
        }
    }
}
