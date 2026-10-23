// One level = one JSON file (Assets/KnitLink/Resources/Levels/level_01.json ...).
// The file has NO level number: the order comes from the LevelList asset.
//
// Example:
// {
//   "name": "Snowman", "tag": "Frozen yarn", "difficulty": "easy", "intro": "ice", "roll": "none",
//   "baskets": [ { "color": "C", "part": "snow ground" }, { "color": "B", "part": "sky" } ],
//   "board":   [ "C   R/B   B*4   _   .",  ... ],
//   "box":     { "x": 1, "y": 2, "color": "R", "count": 4 },   (optional) needle box over the 2x2 spots whose top-left is board column x, row y (0-based)
//   "belt":    { "row": 3, "dir": "right" },                      (optional) conveyor belt on board row 3 (0-based), dir right/left
//   "picture": [ "111111111111111111111111", ... 24 rows of 24 characters ... ]
// }
//
// BOARD TOKENS (one per spot, separated by spaces, one string per row):
//   R O Y G T B V P C  one yarn ball of that color (see ColorCodes below)
//   R/B                a stack: R is hidden below, B is on top (left = bottom). Max 3 balls.
//   B*4                frozen ball: it thaws after 4 valid matches anywhere
//   X2                 tangle with 2 hit points (no ball)
//   _                  empty spot
//   .                  gap (no spot at all)
// PICTURE: 24 rows x 24 characters (since 20/10; was 16); each character is the basket index (0-9, then a-z) that knits that stitch.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KnitLink
{
    [Serializable] public class LevelJson
    {
        public string name, tag, difficulty, intro, roll;
        public BasketJson[] baskets;
        public string[] board;
        public string[] picture;
        public BoxJson box;     // optional (count 0 = no box)
        public BeltJson belt;   // optional (dir empty = no belt)
    }
    [Serializable] public class BasketJson { public string color; public string part; }
    [Serializable] public class BoxJson { public int x, y; public string color; public int count; }
    [Serializable] public class BeltJson { public int row; public string dir; }

    public static class ColorCodes
    {
        /// <summary>Letter used in level files -> color name used by the game.</summary>
        public static readonly Dictionary<char, string> ByLetter = new Dictionary<char, string>
        {
            { 'R', "red" }, { 'O', "orange" }, { 'Y', "yellow" }, { 'G', "green" }, { 'T', "teal" },
            { 'B', "blue" }, { 'V', "purple" }, { 'P', "pink" }, { 'C', "cream" },
        };
        public static char Letter(string color) { foreach (var kv in ByLetter) if (kv.Value == color) return kv.Key; return '?'; }
        public static string Legend => string.Join("  ", ByLetter.Select(kv => kv.Key + "=" + kv.Value));
    }

    public static class LevelJsonIO
    {
        public const int PictureSize = 24;   // picture is PictureSize × PictureSize stitches
        public static readonly string[] Difficulties = { "easy", "medium", "hard", "superhard" };
        public static readonly string[] Intros = { "", "howto", "shelf", "gaps", "hidden", "tangles", "deep", "ice", "roll", "box", "belt" };
        public static readonly string[] Rolls = { "none", "down", "up", "left", "right" };

        // ------------------------------------------------------------------ read
        /// <summary>Parses a level file into the runtime LevelDef. Problems are added to <paramref name="errors"/>; returns null if the level can't be used.</summary>
        public static LevelDef Parse(string json, string file, List<string> errors)
        {
            LevelJson j;
            try { j = JsonUtility.FromJson<LevelJson>(json); }
            catch (Exception e) { errors.Add($"{file}: not valid JSON ({e.Message})"); return null; }
            if (j == null) { errors.Add($"{file}: empty file"); return null; }

            int before = errors.Count;
            var L = new LevelDef
            {
                file = file, name = string.IsNullOrEmpty(j.name) ? file : j.name, tag = j.tag ?? "",
                kind = string.IsNullOrEmpty(j.difficulty) ? "easy" : j.difficulty.Trim().ToLower(),
                roll = (j.roll ?? "none").Trim().ToLower(),
            };
            if (L.roll == "none") L.roll = "";
            if (!Difficulties.Contains(L.kind)) errors.Add($"{file}: difficulty '{j.difficulty}' must be one of {string.Join(", ", Difficulties)}");
            if (L.roll != "" && !Rolls.Contains(L.roll)) errors.Add($"{file}: roll '{j.roll}' must be one of {string.Join(", ", Rolls)}");

            // intro -> (intro, introKey) used by the tutorial code
            string intro = (j.intro ?? "").Trim().ToLower();
            if (!Intros.Contains(intro)) errors.Add($"{file}: intro '{j.intro}' must be one of {string.Join(", ", Intros.Where(x => x != ""))} or empty");
            switch (intro)
            {
                case "howto": L.intro = "howto"; L.introKey = ""; break;
                case "shelf": case "gaps": L.intro = "tip"; L.introKey = intro; break;
                case "hidden": case "tangles": case "deep": case "ice": case "box": case "belt": L.intro = "mech"; L.introKey = intro; break;
                case "roll": L.intro = "roll"; L.introKey = L.roll; if (L.roll == "") errors.Add($"{file}: intro 'roll' needs a roll direction"); break;
                default: L.intro = ""; L.introKey = ""; break;
            }

            // baskets
            if (j.baskets == null || j.baskets.Length == 0) errors.Add($"{file}: no baskets");
            var bs = j.baskets ?? new BasketJson[0];
            L.baskets = new string[bs.Length]; L.basketNames = new string[bs.Length];
            for (int i = 0; i < bs.Length; i++)
            {
                string code = (bs[i].color ?? "").Trim().ToUpper();
                if (code.Length != 1 || !ColorCodes.ByLetter.TryGetValue(code[0], out var col)) { errors.Add($"{file}: basket {i + 1} has unknown color '{bs[i].color}' ({ColorCodes.Legend})"); col = "red"; }
                L.baskets[i] = col; L.basketNames[i] = bs[i].part ?? "";
            }

            // board
            if (j.board == null || j.board.Length == 0) { errors.Add($"{file}: no board"); return null; }
            var rows = j.board.Select(r => (r ?? "").Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
            int W = rows.Max(r => r.Length), H = rows.Length;
            var shape = new string[H]; var stacks = new List<StackDef>(); var tangles = new List<int>(); var ice = new List<int>();
            var spotAt = new Dictionary<(int x, int y), int>();   // board column/row -> spot index (for the box)
            for (int y = 0; y < H; y++)
            {
                var sb = new StringBuilder();
                for (int x = 0; x < W; x++)
                {
                    string tok = x < rows[y].Length ? rows[y][x] : ".";
                    string where = $"{file}: board row {y + 1}, column {x + 1} ('{tok}')";
                    if (tok == ".") { sb.Append('.'); continue; }
                    sb.Append('#'); spotAt[(x, y)] = stacks.Count;
                    if (tok == "_") { stacks.Add(new StackDef { balls = new string[0] }); tangles.Add(0); ice.Add(0); continue; }
                    if (tok[0] == 'X' || tok[0] == 'x')
                    {
                        if (!int.TryParse(tok.Substring(1), out int hp) || hp < 1 || hp > 2) { errors.Add(where + ": tangle must be X1 or X2"); hp = 2; }
                        stacks.Add(new StackDef { balls = new string[0] }); tangles.Add(hp); ice.Add(0); continue;
                    }
                    int frozen = 0; string ballsPart = tok;
                    int star = tok.IndexOf('*');
                    if (star >= 0)
                    {
                        ballsPart = tok.Substring(0, star);
                        if (!int.TryParse(tok.Substring(star + 1), out frozen) || frozen < 1) { errors.Add(where + ": ice count after * must be a number ≥ 1"); frozen = 3; }
                    }
                    var balls = new List<string>();
                    foreach (var part in ballsPart.Split('/'))
                    {
                        if (part.Length != 1 || !ColorCodes.ByLetter.TryGetValue(char.ToUpper(part[0]), out var col)) { errors.Add(where + $": unknown color '{part}' ({ColorCodes.Legend})"); continue; }
                        balls.Add(col);
                    }
                    if (balls.Count > 3) errors.Add(where + ": a stack can hold at most 3 balls");
                    stacks.Add(new StackDef { balls = balls.ToArray() }); tangles.Add(0); ice.Add(frozen);
                }
                shape[y] = sb.ToString();
            }
            L.shape = shape;
            L.board = new BoardDef { stacks = stacks.ToArray(), tangles = tangles.ToArray(), ice = ice.ToArray() };

            // needle box (optional): 2x2 spots, top-left at board column x, row y
            if (j.box != null && j.box.count > 0)
            {
                var sp = new List<int>();
                foreach (var (dx, dy) in new[] { (0, 0), (1, 0), (0, 1), (1, 1) })
                    if (spotAt.TryGetValue((j.box.x + dx, j.box.y + dy), out int t)) sp.Add(t);
                    else errors.Add($"{file}: box at column {j.box.x + 1}, row {j.box.y + 1} needs 2x2 spots (column {j.box.x + dx + 1}, row {j.box.y + dy + 1} is a gap or outside)");
                string code = (j.box.color ?? "").Trim().ToUpper();
                if (code.Length != 1 || !ColorCodes.ByLetter.TryGetValue(code[0], out var bc)) { errors.Add($"{file}: box color '{j.box.color}' unknown ({ColorCodes.Legend})"); bc = "red"; }
                if (sp.Count == 4) L.box = new BoxDef { spots = sp.ToArray(), color = bc, count = j.box.count };
            }
            // conveyor belt (optional): a full row
            if (j.belt != null && !string.IsNullOrEmpty(j.belt.dir))
            {
                string d = j.belt.dir.Trim().ToLower();
                if (d != "left" && d != "right") errors.Add($"{file}: belt dir '{j.belt.dir}' must be left or right");
                if (j.belt.row < 0 || j.belt.row >= H || shape[j.belt.row].Contains('.')) errors.Add($"{file}: belt row {j.belt.row + 1} must be a full row with no gaps");
                else L.belt = new BeltDef { row = j.belt.row, dir = d == "left" ? -1 : 1 };
            }

            // picture
            L.picture = new int[PictureSize * PictureSize];
            if (j.picture == null || j.picture.Length != PictureSize) errors.Add($"{file}: picture must have {PictureSize} rows");
            else for (int y = 0; y < PictureSize; y++)
                {
                    string row = (j.picture[y] ?? "").Replace(" ", "");
                    if (row.Length != PictureSize) { errors.Add($"{file}: picture row {y + 1} must have {PictureSize} characters (has {row.Length})"); continue; }
                    for (int x = 0; x < PictureSize; x++)
                    {
                        int v = DigitValue(row[x]);
                        if (v < 0 || v >= L.baskets.Length) errors.Add($"{file}: picture row {y + 1}, column {x + 1}: '{row[x]}' is not a basket number (0..{DigitChar(L.baskets.Length - 1)})");
                        L.picture[y * PictureSize + x] = Mathf.Max(0, v);
                    }
                }
            return errors.Count > before && L.board.stacks.Length == 0 ? null : L;
        }

        public static int DigitValue(char c) => c >= '0' && c <= '9' ? c - '0' : c >= 'a' && c <= 'z' ? 10 + c - 'a' : c >= 'A' && c <= 'Z' ? 10 + c - 'A' : -1;
        public static char DigitChar(int v) => v < 10 ? (char)('0' + v) : (char)('a' + v - 10);

        // ------------------------------------------------------------------ write
        /// <summary>Board as rows of tokens (gaps included), used by the writer and the Level Editor.</summary>
        public static string[][] BoardTokens(LevelDef L)
        {
            var outRows = new string[L.shape.Length][]; int t = 0;
            for (int y = 0; y < L.shape.Length; y++)
            {
                outRows[y] = new string[L.shape[y].Length];
                for (int x = 0; x < L.shape[y].Length; x++)
                {
                    if (L.shape[y][x] == '.') { outRows[y][x] = "."; continue; }
                    outRows[y][x] = SpotToken(L, t++);
                }
            }
            return outRows;
        }
        public static string SpotToken(LevelDef L, int t)
        {
            var balls = L.board.stacks[t].balls ?? new string[0];
            int hp = L.board.tangles != null && t < L.board.tangles.Length ? L.board.tangles[t] : 0;
            int ice = L.board.ice != null && t < L.board.ice.Length ? L.board.ice[t] : 0;
            if (balls.Length == 0) return hp > 0 ? "X" + hp : "_";
            string s = string.Join("/", balls.Select(b => ColorCodes.Letter(b).ToString()));
            return ice > 0 ? s + "*" + ice : s;
        }

        /// <summary>Pretty JSON with board columns lined up so a designer can read it.</summary>
        public static string ToJson(LevelDef L)
        {
            var rows = BoardTokens(L);
            int cols = rows.Max(r => r.Length);
            var width = new int[cols];
            foreach (var r in rows) for (int x = 0; x < r.Length; x++) width[x] = Mathf.Max(width[x], r[x].Length);
            string intro = L.intro == "howto" ? "howto" : L.intro == "tip" || L.intro == "mech" ? L.introKey : L.intro == "roll" ? "roll" : "";
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"name\": {Q(L.name)},\n  \"tag\": {Q(L.tag)},\n  \"difficulty\": {Q(L.kind)},\n  \"intro\": {Q(intro)},\n  \"roll\": {Q(string.IsNullOrEmpty(L.roll) ? "none" : L.roll)},\n");
            if (L.box != null)
            {   // top-left spot of the box in board column/row
                var cells = new List<(int x, int y)>(); for (int y = 0; y < L.shape.Length; y++) for (int x = 0; x < L.shape[y].Length; x++) if (L.shape[y][x] != '.') cells.Add((x, y));
                int bx = L.box.spots.Min(t => cells[t].x), by = L.box.spots.Min(t => cells[t].y);
                sb.Append($"  \"box\": {{ \"x\": {bx}, \"y\": {by}, \"color\": \"{ColorCodes.Letter(L.box.color)}\", \"count\": {L.box.count} }},\n");
            }
            if (L.belt != null) sb.Append($"  \"belt\": {{ \"row\": {L.belt.row}, \"dir\": \"{(L.belt.dir < 0 ? "left" : "right")}\" }},\n");
            sb.Append("  \"baskets\": [\n");
            for (int i = 0; i < L.baskets.Length; i++)
                sb.Append($"    {{ \"color\": \"{ColorCodes.Letter(L.baskets[i])}\", \"part\": {Q(L.basketNames != null && i < L.basketNames.Length ? L.basketNames[i] : "")} }}{(i < L.baskets.Length - 1 ? "," : "")}\n");
            sb.Append("  ],\n  \"board\": [\n");
            for (int y = 0; y < rows.Length; y++)
            {
                var line = string.Join("  ", Enumerable.Range(0, cols).Select(x => (x < rows[y].Length ? rows[y][x] : ".").PadRight(width[x]))).TrimEnd();
                sb.Append($"    {Q(line)}{(y < rows.Length - 1 ? "," : "")}\n");
            }
            sb.Append("  ],\n  \"picture\": [\n");
            for (int y = 0; y < PictureSize; y++)
            {
                var line = new string(Enumerable.Range(0, PictureSize).Select(x => DigitChar(L.picture[y * PictureSize + x])).ToArray());
                sb.Append($"    {Q(line)}{(y < PictureSize - 1 ? "," : "")}\n");
            }
            sb.Append("  ]\n}\n");
            return sb.ToString();
        }
        static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
