// Runtime level model + loading.
// Levels live as one JSON file each (Resources/Levels/level_XX.json, format in LevelJson.cs).
// Their ORDER is the LevelList asset (drag files in the Inspector). Level number = position in the list (1-based).
// If no LevelList is set up, all files in Resources/Levels are used, sorted by file name.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KnitLink
{
    [Serializable] public class LevelFile { public LevelDef[] levels; }

    /// <summary>One level as the game uses it (built from a JSON file by LevelJsonIO.Parse).</summary>
    [Serializable] public class LevelDef
    {
        public int num;                          // level number = position in the LevelList (1-based)
        public string file;                      // JSON file name, for logs and analytics
        public string kind;                      // easy / medium / hard / superhard
        public string intro, introKey;           // tutorial: howto | tip (shelf/gaps) | mech (hidden/tangles/deep/ice) | roll (direction)
        public string name, tag, roll;           // roll: "" or down/up/left/right
        public string[] shape;                   // '#' spot, '.' gap
        public string[] baskets, basketNames;    // basket colors in knitting order
        public BoardDef board;
        public int[] picture;                    // 24x24 (LevelJsonIO.PictureSize), basket index per stitch
    }
    [Serializable] public class BoardDef { public StackDef[] stacks; public int[] tangles; public int[] ice; }
    [Serializable] public class StackDef { public string[] balls; }

    public static class LevelData
    {
        static LevelFile cache;
        /// <summary>Problems found while loading (shown in the Console with the [Level] tag).</summary>
        public static readonly List<string> LoadErrors = new List<string>();

        public static LevelFile Load()
        {
            if (cache != null) return cache;
            LoadErrors.Clear();
            var files = LevelFiles();
            var list = new List<LevelDef>();
            foreach (var f in files)
            {
                if (f == null) { LoadErrors.Add("LevelList has an empty slot"); continue; }
                var errs = new List<string>();
                var L = LevelJsonIO.Parse(f.text, f.name, errs);
                foreach (var e in errs) LoadErrors.Add(e);
                if (L == null) continue;
                L.num = list.Count + 1; list.Add(L);
            }
            foreach (var e in LoadErrors) KLog.Error("Level", e);
            KLog.Info("Level", $"Loaded {list.Count} levels" + (LoadErrors.Count > 0 ? $" with {LoadErrors.Count} problems" : ""));
            return cache = new LevelFile { levels = list.ToArray() };
        }

        /// <summary>Call after editing level files in the Editor so the next Load() reads them again.</summary>
        public static void ClearCache() { cache = null; }

        /// <summary>The level files in play order: the LevelList from GameConfig, else every file in Resources/Levels by name.</summary>
        public static TextAsset[] LevelFiles()
        {
            var cfg = GameConfig.Current;
            if (cfg.levelList != null && cfg.levelList.levels != null && cfg.levelList.levels.Length > 0) return cfg.levelList.levels;
            return Resources.LoadAll<TextAsset>("Levels").OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
        }
    }
}
