// Menu: Knit Link > Level Editor
// Edit one level file at a time:
//   - info: name, tag, difficulty, intro (tutorial), roll direction
//   - baskets: color + picture part, in knitting order (reorder with the arrows)
//   - board: one token per spot (legend under the grid), add/remove rows and columns
//   - picture: pick a basket as the brush and paint the 24x24 preview, or type the digits
//   - Validate: rules + solver + bot win rate   - Save: writes the JSON file   - Play: runs this level in the Game scene
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KnitLink.EditorTools
{
    public class LevelEditorWindow : EditorWindow
    {
        class Basket { public char color = 'R'; public string part = ""; }

        // working copy of the open level
        int index = -1; TextAsset asset;
        string lvName = "", lvTag = "", difficulty = "easy", intro = "", roll = "none";
        List<Basket> baskets = new List<Basket>();
        List<List<string>> board = new List<List<string>>();
        string[] picture = new string[LevelJsonIO.PictureSize];
        bool dirty; int brush; LevelReport report; Vector2 scroll;

        static readonly char[] Letters = ColorCodes.ByLetter.Keys.ToArray();
        static readonly string[] LetterNames = ColorCodes.ByLetter.Select(kv => $"{kv.Key}  {kv.Value}").ToArray();

        [MenuItem("Knit Link/Level Editor", priority = 10)]
        public static void OpenMenu() => Open(0);
        public static void Open(int levelIndex)
        {
            var w = GetWindow<LevelEditorWindow>("Level Editor");
            w.minSize = new Vector2(620, 600);
            w.Load(levelIndex);
        }

        static TextAsset[] Files()
        {
            var list = GameConfig.Current.levelList;
            return list != null && list.levels != null && list.levels.Length > 0 ? list.levels : KnitLinkSetup.LevelFilesInFolder();
        }

        // ------------------------------------------------------------------ load / save
        void Load(int i)
        {
            var files = Files();
            if (files.Length == 0) { index = -1; asset = null; return; }
            index = Mathf.Clamp(i, 0, files.Length - 1); asset = files[index]; report = null; dirty = false;
            if (asset == null) return;
            var j = JsonUtility.FromJson<LevelJson>(asset.text) ?? new LevelJson();
            lvName = j.name ?? ""; lvTag = j.tag ?? ""; difficulty = string.IsNullOrEmpty(j.difficulty) ? "easy" : j.difficulty;
            intro = j.intro ?? ""; roll = string.IsNullOrEmpty(j.roll) ? "none" : j.roll;
            baskets = (j.baskets ?? new BasketJson[0]).Select(b => new Basket { color = string.IsNullOrEmpty(b.color) ? 'R' : char.ToUpper(b.color[0]), part = b.part ?? "" }).ToList();
            var rows = (j.board ?? new string[0]).Select(r => (r ?? "").Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).ToList()).ToList();
            int w = rows.Count == 0 ? 5 : rows.Max(r => r.Count);
            foreach (var r in rows) while (r.Count < w) r.Add(".");
            board = rows.Count > 0 ? rows : Enumerable.Range(0, 5).Select(_ => Enumerable.Repeat("_", 5).ToList()).ToList();
            picture = new string[LevelJsonIO.PictureSize];
            for (int y = 0; y < picture.Length; y++)
                picture[y] = j.picture != null && y < j.picture.Length && j.picture[y] != null ? j.picture[y].PadRight(LevelJsonIO.PictureSize, '0').Substring(0, LevelJsonIO.PictureSize) : new string('0', LevelJsonIO.PictureSize);
        }

        string ToJson()
        {
            var sb = new StringBuilder("{\n");
            sb.Append($"  \"name\": {Q(lvName)},\n  \"tag\": {Q(lvTag)},\n  \"difficulty\": {Q(difficulty)},\n  \"intro\": {Q(intro)},\n  \"roll\": {Q(roll)},\n");
            sb.Append("  \"baskets\": [\n");
            sb.Append(string.Join(",\n", baskets.Select(b => $"    {{ \"color\": \"{b.color}\", \"part\": {Q(b.part)} }}")));
            sb.Append("\n  ],\n  \"board\": [\n");
            int cols = board.Max(r => r.Count);
            var width = Enumerable.Range(0, cols).Select(x => board.Max(r => x < r.Count ? Tok(r[x]).Length : 1)).ToArray();
            sb.Append(string.Join(",\n", board.Select(r => "    " + Q(string.Join("  ", Enumerable.Range(0, cols).Select(x => (x < r.Count ? Tok(r[x]) : ".").PadRight(width[x]))).TrimEnd()))));
            sb.Append("\n  ],\n  \"picture\": [\n");
            sb.Append(string.Join(",\n", picture.Select(p => "    " + Q(p))));
            sb.Append("\n  ]\n}\n");
            return sb.ToString();
        }
        static string Tok(string s) { s = (s ?? "").Trim(); return s == "" ? "_" : s; }
        static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        void Save()
        {
            if (asset == null) return;
            string path = AssetDatabase.GetAssetPath(asset);
            File.WriteAllText(path, ToJson());
            AssetDatabase.ImportAsset(path);
            LevelData.ClearCache(); dirty = false;
            Debug.Log($"[Level] Saved {path}");
        }

        void Validate()
        {
            var errs = new List<string>();
            var L = LevelJsonIO.Parse(ToJson(), asset != null ? asset.name : "level", errs);
            if (L == null) { report = new LevelReport { file = asset != null ? asset.name : "level", num = index + 1 }; }
            else { L.num = index + 1; report = LevelCheck.Check(L, GameConfig.Current.shelfSlots, GameConfig.Current.solverBudget, 300); }
            report.errors.InsertRange(0, errs);
        }

        void PlayThis()
        {
            if (dirty) Save();
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            if (Object.FindFirstObjectByType<GameController>() == null && File.Exists(KnitLinkSetup.ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(KnitLinkSetup.ScenePath);
            }
            PlayerPrefs.SetInt(GameController.DevStartLevelKey, index); PlayerPrefs.Save();
            LevelData.ClearCache();
            EditorApplication.isPlaying = true;
        }

        void NewLevel()
        {
            var list = GameConfig.Current.levelList;
            int n = KnitLinkSetup.LevelFilesInFolder().Length + 1; string path;
            do { path = $"{KnitLinkSetup.LevelsFolder}/level_{n:00}.json"; n++; } while (File.Exists(path));
            File.WriteAllText(path, ToJson());   // a copy of the open level as a starting point
            AssetDatabase.ImportAsset(path);
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (list != null)
            {
                Undo.RecordObject(list, "Add level");
                list.levels = list.levels.Concat(new[] { ta }).ToArray(); EditorUtility.SetDirty(list); AssetDatabase.SaveAssets();
            }
            LevelData.ClearCache();
            Load(Files().ToList().IndexOf(ta));
            Debug.Log($"[Level] Created {path} (copy of the previous level) at the end of the LevelList");
        }

        // ------------------------------------------------------------------ GUI
        void OnGUI()
        {
            var files = Files();
            if (files.Length == 0) { EditorGUILayout.HelpBox("No level files. Run Knit Link > Create Config Assets.", MessageType.Warning); return; }
            if (index < 0 || index >= files.Length || asset != files[index]) Load(Mathf.Clamp(index, 0, files.Length - 1));

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var names = files.Select((f, i) => $"Lv{i + 1}  {(f != null ? f.name : "(empty)")}").ToArray();
                int pick = EditorGUILayout.Popup(index, names, EditorStyles.toolbarPopup, GUILayout.Width(200));
                if (pick != index && ConfirmDiscard()) Load(pick);
                if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(24)) && index > 0 && ConfirmDiscard()) Load(index - 1);
                if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(24)) && index < files.Length - 1 && ConfirmDiscard()) Load(index + 1);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton)) Validate();
                if (GUILayout.Button(dirty ? "Save *" : "Save", EditorStyles.toolbarButton)) Save();
                if (GUILayout.Button("Revert", EditorStyles.toolbarButton) && (!dirty || EditorUtility.DisplayDialog("Revert", "Discard your changes?", "Discard", "Keep"))) Load(index);
                if (GUILayout.Button("Play", EditorStyles.toolbarButton)) PlayThis();
                if (GUILayout.Button("New level", EditorStyles.toolbarButton)) NewLevel();
                if (GUILayout.Button("Ping file", EditorStyles.toolbarButton)) EditorGUIUtility.PingObject(asset);
            }
            if (asset == null) { EditorGUILayout.HelpBox("This slot of the LevelList is empty.", MessageType.Error); return; }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUI.BeginChangeCheck();
            DrawInfo(); DrawBaskets(); DrawBoard(); DrawPicture();
            if (EditorGUI.EndChangeCheck()) { dirty = true; report = null; }
            DrawReport();
            EditorGUILayout.EndScrollView();
        }

        bool ConfirmDiscard() => !dirty || EditorUtility.DisplayDialog("Unsaved changes", "Discard the changes to this level?", "Discard", "Stay");

        void DrawInfo()
        {
            EditorGUILayout.LabelField($"Level {index + 1}   ·   {AssetDatabase.GetAssetPath(asset)}", EditorStyles.boldLabel);
            lvName = EditorGUILayout.TextField("Name", lvName);
            lvTag = EditorGUILayout.TextField("Tag (subtitle)", lvTag);
            difficulty = Popup("Difficulty", difficulty, LevelJsonIO.Difficulties);
            intro = Popup("Intro (tutorial)", intro, LevelJsonIO.Intros);
            roll = Popup("Roll", roll, LevelJsonIO.Rolls);
        }
        static string Popup(string label, string value, string[] options)
        {
            var shown = options.Select(o => o == "" ? "(none)" : o).ToArray();
            int i = System.Array.IndexOf(options, value); if (i < 0) i = 0;
            return options[EditorGUILayout.Popup(label, i, shown)];
        }

        void DrawBaskets()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Baskets (knitting order – the number is used in the picture)", EditorStyles.boldLabel);
            for (int i = 0; i < baskets.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var b = baskets[i];
                    EditorGUILayout.LabelField(LevelJsonIO.DigitChar(i).ToString(), GUILayout.Width(16));
                    var r = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16)); EditorGUI.DrawRect(r, ColorOf(b.color));
                    int ci = System.Array.IndexOf(Letters, b.color);
                    b.color = Letters[Mathf.Max(0, EditorGUILayout.Popup(Mathf.Max(0, ci), LetterNames, GUILayout.Width(110)))];
                    b.part = EditorGUILayout.TextField(b.part);
                    if (GUILayout.Button("▲", GUILayout.Width(24)) && i > 0) { (baskets[i - 1], baskets[i]) = (baskets[i], baskets[i - 1]); SwapPicture(i - 1, i); GUI.changed = true; }
                    if (GUILayout.Button("▼", GUILayout.Width(24)) && i < baskets.Count - 1) { (baskets[i + 1], baskets[i]) = (baskets[i], baskets[i + 1]); SwapPicture(i, i + 1); GUI.changed = true; }
                    if (GUILayout.Button("✕", GUILayout.Width(24)) && EditorUtility.DisplayDialog("Remove basket", $"Remove basket {i}? Its stitches in the picture must be repainted.", "Remove", "Cancel")) { baskets.RemoveAt(i); GUI.changed = true; break; }
                }
            }
            if (GUILayout.Button("+ Add basket", GUILayout.Width(120))) { baskets.Add(new Basket { color = baskets.Count > 0 ? baskets[baskets.Count - 1].color : 'R' }); GUI.changed = true; }
        }
        /// <summary>Moving a basket keeps its stitches: swap the two digits in the picture.</summary>
        void SwapPicture(int a, int b)
        {
            char ca = LevelJsonIO.DigitChar(a), cb = LevelJsonIO.DigitChar(b);
            for (int y = 0; y < picture.Length; y++) picture[y] = new string(picture[y].Select(c => c == ca ? cb : c == cb ? ca : c).ToArray());
        }

        void DrawBoard()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Board", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{board[0].Count} columns × {board.Count} rows", GUILayout.Width(150));
                if (GUILayout.Button("+ col", GUILayout.Width(50))) { foreach (var r in board) r.Add("_"); GUI.changed = true; }
                if (GUILayout.Button("– col", GUILayout.Width(50)) && board[0].Count > 1) { foreach (var r in board) r.RemoveAt(r.Count - 1); GUI.changed = true; }
                if (GUILayout.Button("+ row", GUILayout.Width(50))) { board.Add(Enumerable.Repeat("_", board[0].Count).ToList()); GUI.changed = true; }
                if (GUILayout.Button("– row", GUILayout.Width(50)) && board.Count > 1) { board.RemoveAt(board.Count - 1); GUI.changed = true; }
            }
            var old = GUI.backgroundColor;
            foreach (var row in board)
            {
                using (new EditorGUILayout.HorizontalScope())
                    for (int x = 0; x < row.Count; x++)
                    {
                        GUI.backgroundColor = TokenColor(row[x]);
                        row[x] = EditorGUILayout.TextField(row[x], GUILayout.Width(62));
                    }
            }
            GUI.backgroundColor = old;
            EditorGUILayout.HelpBox("Tokens:  R = one red ball   R/B = stack (R hidden below, B on top, max 3)   B*4 = frozen, thaws after 4 matches\n" +
                                    "X2 = tangle (hp 2)   _ = empty spot   . = gap (no spot)\nColors:  " + ColorCodes.Legend, MessageType.None);
        }
        static Color TokenColor(string tok)
        {
            tok = (tok ?? "").Trim();
            if (tok == "" || tok == "_") return Color.white;
            if (tok == ".") return new Color(.35f, .35f, .35f);
            if (tok[0] == 'X' || tok[0] == 'x') return new Color(.8f, .7f, .55f);
            string balls = tok.Split('*')[0]; char top = balls[balls.Length - 1];
            var c = ColorOf(char.ToUpper(top));
            return tok.Contains("*") ? Color.Lerp(c, new Color(.7f, .9f, 1f), .5f) : c;
        }
        static Color ColorOf(char letter) => ColorCodes.ByLetter.TryGetValue(letter, out var name) ? UIKit.C(name) : Color.magenta;

        void DrawPicture()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Picture ({LevelJsonIO.PictureSize}×{LevelJsonIO.PictureSize}) – each stitch is knitted by the basket with that number", EditorStyles.boldLabel);
            if (baskets.Count == 0) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Brush", GUILayout.Width(40));
                brush = Mathf.Clamp(EditorGUILayout.Popup(brush, baskets.Select((b, i) => $"{LevelJsonIO.DigitChar(i)}  {ColorCodes.ByLetter.GetValueOrDefault(b.color, "?")}  {b.part}").ToArray(), GUILayout.Width(220)), 0, baskets.Count - 1);
                EditorGUILayout.LabelField("click / drag on the picture to paint", EditorStyles.miniLabel);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = GUILayoutUtility.GetRect(312, 312, GUILayout.Width(312), GUILayout.Height(312));
                float s = rect.width / LevelJsonIO.PictureSize;
                for (int y = 0; y < LevelJsonIO.PictureSize; y++)
                    for (int x = 0; x < LevelJsonIO.PictureSize; x++)
                    {
                        int v = LevelJsonIO.DigitValue(picture[y][x]);
                        var c = v >= 0 && v < baskets.Count ? ColorOf(baskets[v].color) : Color.magenta;
                        EditorGUI.DrawRect(new Rect(rect.x + x * s, rect.y + y * s, s - 1, s - 1), c);
                    }
                var e = Event.current;
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && rect.Contains(e.mousePosition))
                {
                    int x = (int)((e.mousePosition.x - rect.x) / s), y = (int)((e.mousePosition.y - rect.y) / s);
                    var chars = picture[y].ToCharArray(); chars[x] = LevelJsonIO.DigitChar(brush); picture[y] = new string(chars);
                    GUI.changed = true; e.Use(); Repaint();
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    var mono = new GUIStyle(EditorStyles.textField) { font = EditorStyles.standardFont, fontSize = 11 };
                    for (int y = 0; y < LevelJsonIO.PictureSize; y++) picture[y] = EditorGUILayout.TextField(picture[y], mono, GUILayout.Width(210)).PadRight(LevelJsonIO.PictureSize, '0').Substring(0, LevelJsonIO.PictureSize);
                }
            }
        }

        void DrawReport()
        {
            EditorGUILayout.Space();
            if (report == null) { EditorGUILayout.HelpBox("Press Validate to check rules, solvability and the bot win rate.", MessageType.None); return; }
            var type = !report.Ok ? MessageType.Error : report.warnings.Count > 0 ? MessageType.Warning : MessageType.Info;
            var text = report.Summary + string.Concat(report.errors.Select(e => "\nERROR: " + e)) + string.Concat(report.warnings.Select(w => "\nwarning: " + w));
            EditorGUILayout.HelpBox(text, type);
        }
    }
}
