// Menu "Knit Link":
//   Setup Game Scene        creates Assets/Scenes/Game.unity with the GameController (first in Build Settings, portrait)
//   Create Config Assets    creates Resources/GameConfig.asset and Config/LevelList.asset (filled with every level_XX.json)
//   Validate All Levels     checks every level in the LevelList and prints one line per level in the Console
//   Level Editor            opens the level editor window (LevelEditorWindow.cs)
//   Reset Player Progress   clears saved progress, coins, boosters and tutorials (PlayerPrefs)
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KnitLink.EditorTools
{
    public static class KnitLinkSetup
    {
        public const string ScenePath = "Assets/Scenes/Game.unity";
        public const string LevelsFolder = "Assets/KnitLink/Resources/Levels";
        public const string ConfigPath = "Assets/KnitLink/Resources/GameConfig.asset";
        public const string LevelListPath = "Assets/KnitLink/Config/LevelList.asset";

        [MenuItem("Knit Link/Setup Game Scene", priority = 1)]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));   // AudioListener: without it Unity plays no sound
            cam.tag = "MainCamera";
            var c = cam.GetComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(.11f, .16f, .32f); c.orthographic = true;
            new GameObject("Game", typeof(KnitLink.GameController));
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes) if (s.path != ScenePath) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.productName = "Knit Link";
            Debug.Log("[Setup] Game scene ready: " + ScenePath + ". Set the Game view to 1080x2340 portrait and press Play.");
        }

        [MenuItem("Knit Link/Create Config Assets", priority = 2)]
        public static void CreateConfigAssets()
        {
            EnsureFolder("Assets/KnitLink/Resources"); EnsureFolder("Assets/KnitLink/Config");
            var list = AssetDatabase.LoadAssetAtPath<LevelList>(LevelListPath);
            if (list == null)
            {
                list = ScriptableObject.CreateInstance<LevelList>();
                list.levels = LevelFilesInFolder();
                AssetDatabase.CreateAsset(list, LevelListPath);
                Debug.Log($"[Config] Created {LevelListPath} with {list.levels.Length} levels");
            }
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                cfg.levelList = list;
                AssetDatabase.CreateAsset(cfg, ConfigPath);
                Debug.Log($"[Config] Created {ConfigPath}");
            }
            else if (cfg.levelList == null) { cfg.levelList = list; EditorUtility.SetDirty(cfg); }
            AssetDatabase.SaveAssets();
            GameConfig.Reload(); LevelData.ClearCache();
            Selection.activeObject = cfg;
        }

        /// <summary>All level_XX.json files in the Levels folder, in number order (level_2 before level_10).</summary>
        public static TextAsset[] LevelFilesInFolder()
        {
            return AssetDatabase.FindAssets("t:TextAsset", new[] { LevelsFolder })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".json"))
                .OrderBy(p => NumberIn(System.IO.Path.GetFileNameWithoutExtension(p))).ThenBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<TextAsset>).ToArray();
        }
        static int NumberIn(string name) { var d = new string(name.Where(char.IsDigit).ToArray()); return int.TryParse(d, out int n) ? n : int.MaxValue; }

        [MenuItem("Knit Link/Validate All Levels", priority = 20)]
        public static void ValidateAllMenu() { ValidateAll(GameConfig.Current.levelList, true); }

        /// <summary>Checks every level of a list; logs one line per level. Returns the reports.</summary>
        public static System.Collections.Generic.List<LevelReport> ValidateAll(LevelList list, bool log)
        {
            var files = list != null && list.levels != null && list.levels.Length > 0 ? list.levels : LevelFilesInFolder();
            var reports = new System.Collections.Generic.List<LevelReport>();
            var seen = new System.Collections.Generic.HashSet<TextAsset>();
            int shelf = GameConfig.Current.shelfSlots, budget = GameConfig.Current.solverBudget;
            for (int i = 0; i < files.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Validate levels", $"Level {i + 1}/{files.Length}", (float)i / files.Length);
                var f = files[i];
                if (f == null) { var e = new LevelReport { file = "(empty slot)", num = i + 1 }; e.errors.Add("empty slot in the LevelList"); reports.Add(e); continue; }
                var errs = new System.Collections.Generic.List<string>();
                var L = LevelJsonIO.Parse(f.text, f.name, errs);
                LevelReport r;
                if (L == null) r = new LevelReport { file = f.name, num = i + 1 };
                else { L.num = i + 1; r = LevelCheck.Check(L, shelf, budget); }
                r.errors.InsertRange(0, errs);
                if (!seen.Add(f)) r.warnings.Add("this file is in the list more than once");
                reports.Add(r);
            }
            EditorUtility.ClearProgressBar();
            if (log)
            {
                foreach (var r in reports)
                {
                    string line = $"[Level] Lv{r.num}: {r.Summary}" + string.Concat(r.errors.Select(e => "\n   ERROR: " + e)) + string.Concat(r.warnings.Select(w => "\n   warning: " + w));
                    if (!r.Ok) Debug.LogError(line); else if (r.warnings.Count > 0) Debug.LogWarning(line); else Debug.Log(line);
                }
                int bad = reports.Count(r => !r.Ok);
                Debug.Log($"[Level] Validated {reports.Count} levels: {reports.Count - bad} OK, {bad} with problems.");
            }
            return reports;
        }

        [MenuItem("Knit Link/Reset Player Progress", priority = 40)]
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteAll(); PlayerPrefs.Save();
            Debug.Log("[Setup] Player progress, coins, boosters and tutorials reset. Next Play starts at level 1.");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
