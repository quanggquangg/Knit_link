// Level checks used by the Level Editor, the LevelList "Validate all" button and the tests.
// - rules: board size, colors, each color = 4 balls per basket, stack depth, picture uses every basket
// - solvable: the solver finds a way to win without boosters
// - bot win rate: a casual player model (prefers pairs for the open basket, otherwise random)
using System;
using System.Collections.Generic;
using System.Linq;

namespace KnitLink
{
    public class LevelReport
    {
        public string file; public int num;
        public List<string> errors = new List<string>();     // must fix: the level is broken
        public List<string> warnings = new List<string>();   // allowed, but against the design rules
        public bool solvable; public bool solverGaveUp; public float botWinRate = -1;
        public int balls, colors, width, height, maxDepth, frozen, tangles;
        public bool Ok => errors.Count == 0 && solvable;
        public string Summary =>
            $"{(Ok ? "OK " : "FAIL")}  {file}  {width}x{height}  {colors} colors  {balls} balls" +
            (botWinRate >= 0 ? $"  bot win {botWinRate:P0}" : "") +
            (solvable ? "" : solverGaveUp ? "  (solver gave up – try a bigger budget)" : "  NOT SOLVABLE") +
            (errors.Count > 0 ? $"  {errors.Count} error(s)" : "") + (warnings.Count > 0 ? $"  {warnings.Count} warning(s)" : "");
    }

    public static class LevelCheck
    {
        // design rules (decided 16-17/10)
        public const int MaxW = 6, MaxH = 7, MaxColors = 6;   // 6 colors since 22/10 (more detailed pictures)

        public static LevelReport Check(LevelDef L, int shelfSlots, int solverBudget = 600000, int botRuns = 200)
        {
            var r = new LevelReport { file = L.file, num = L.num };
            if (L.shape == null || L.board == null || L.baskets == null) { r.errors.Add("level could not be read"); return r; }
            r.height = L.shape.Length; r.width = L.shape.Max(s => s.Length);
            if (r.width > MaxW || r.height > MaxH) r.warnings.Add($"board is {r.width}x{r.height}; the rule is at most {MaxW}x{MaxH}");

            // ball counts must match the baskets: 4 balls of a color per basket of that color
            var need = new Dictionary<string, int>(); foreach (var c in L.baskets) need[c] = (need.TryGetValue(c, out var n) ? n : 0) + 4;
            var have = new Dictionary<string, int>();
            foreach (var st in L.board.stacks) foreach (var c in st.balls ?? new string[0]) have[c] = (have.TryGetValue(c, out var n) ? n : 0) + 1;
            foreach (var c in need.Keys.Union(have.Keys))
            {
                need.TryGetValue(c, out int nd); have.TryGetValue(c, out int hv);
                if (nd != hv) r.errors.Add($"{c}: {hv} balls on the board but the baskets need {nd} ({nd / 4} basket(s) x 4)");
            }
            r.balls = have.Values.Sum(); r.colors = need.Count;
            if (r.colors > MaxColors) r.warnings.Add($"{r.colors} colors; the rule is at most {MaxColors}");
            r.maxDepth = L.board.stacks.Max(s => (s.balls ?? new string[0]).Length);
            // stack depth (rule 23/10): 3 layers from Lv8 on medium/hard/superhard levels and from Lv16 on every level; else 2
            int depthRule = L.num <= 0 || L.num >= 16 || (L.num >= 8 && L.kind != "easy") ? 3 : 2;
            if (r.maxDepth > depthRule) r.warnings.Add($"stacks of {r.maxDepth}; the rule is at most {depthRule} at this level number");
            r.frozen = L.board.ice?.Count(x => x > 0) ?? 0; r.tangles = L.board.tangles?.Count(x => x > 0) ?? 0;
            for (int t = 0; t < L.board.stacks.Length; t++)
                if (L.board.ice != null && t < L.board.ice.Length && L.board.ice[t] > 0 && (L.board.stacks[t].balls ?? new string[0]).Length == 0)
                    r.errors.Add($"spot {t + 1} is frozen but has no ball");
            // rule (decided 20/10): a stack never holds two balls of the same color. Otherwise the last 2 balls of that
            // color can end up on top of each other, can never be paired, and the level cannot be finished.
            for (int t = 0; t < L.board.stacks.Length; t++)
            {
                var b = L.board.stacks[t].balls ?? new string[0];
                var twice = b.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                if (twice.Count > 0) r.errors.Add($"spot {t + 1} stacks {string.Join(", ", twice)} twice ({string.Join("/", b)}); a stack must not hold the same color twice");
            }
            // "?" balls (28/10): not frozen, not under the needle box, never two side by side (each needs a normal neighbour to open it)
            {
                var g0 = Geometry.FromShape(L.shape);
                for (int t = 0; t < L.board.stacks.Length; t++)
                {
                    if (!L.board.stacks[t].mystery) continue;
                    if ((L.board.stacks[t].balls ?? new string[0]).Length == 0) r.errors.Add($"spot {t + 1} is marked ? but has no ball");
                    if (L.board.ice != null && t < L.board.ice.Length && L.board.ice[t] > 0) r.errors.Add($"spot {t + 1}: a ? ball can't be frozen");
                    if (L.box != null && System.Array.IndexOf(L.box.spots, t) >= 0) r.errors.Add($"spot {t + 1}: a ? ball can't be under the needle box");
                    foreach (int n in g0.nb[t]) if (n > t && L.board.stacks[n].mystery) r.warnings.Add($"spots {t + 1} and {n + 1}: two ? balls side by side");
                }
            }
            // needle box: 4 single balls under the lid, no tangle/ice there, enough balls of its color outside, not on the belt row
            if (L.box != null)
            {
                foreach (int t in L.box.spots)
                {
                    var b = L.board.stacks[t].balls ?? new string[0];
                    if (b.Length != 1) r.errors.Add($"spot {t + 1} is under the needle box and must hold exactly 1 ball (has {b.Length})");
                    if ((L.board.tangles != null && L.board.tangles[t] > 0) || (L.board.ice != null && L.board.ice[t] > 0)) r.errors.Add($"spot {t + 1} is under the needle box: no tangle or ice there");
                }
                int outside = 0; for (int t = 0; t < L.board.stacks.Length; t++) if (System.Array.IndexOf(L.box.spots, t) < 0) foreach (var c in L.board.stacks[t].balls ?? new string[0]) if (c == L.box.color) outside++;
                if (outside < L.box.count) r.errors.Add($"needle box needs {L.box.count} {L.box.color} balls matched but only {outside} are outside the box");
            }
            if (L.box != null && L.belt != null)
            {
                var g = Geometry.FromShape(L.shape);
                foreach (int t in L.box.spots) if (g.xy[t][1] == L.belt.row) r.errors.Add($"the needle box must not sit on the conveyor belt row (spot {t + 1})");
            }
            var used = new HashSet<int>(L.picture ?? new int[0]);
            for (int i = 0; i < L.baskets.Length; i++) if (!used.Contains(i)) r.warnings.Add($"basket {i + 1} ({L.baskets[i]}) knits no stitch in the picture");
            if (r.errors.Count > 0) return r;

            BoardState s;
            try { s = BoardState.FromLevel(L); s.cap = shelfSlots; }
            catch (Exception e) { r.errors.Add("board could not be built: " + e.Message); return r; }
            var sol = BoardState.Solve(s, solverBudget, out bool gaveUp);
            r.solvable = sol != null; r.solverGaveUp = sol == null && gaveUp;
            if (botRuns > 0) r.botWinRate = BotWinRate(s, botRuns, 7);
            return r;
        }

        /// <summary>Share of games a casual player model wins (no boosters, no revive).</summary>
        public static float BotWinRate(BoardState s0, int runs, int seed)
        {
            var rnd = new Random(seed); int wins = 0;
            for (int i = 0; i < runs; i++) if (BotPlay(s0, rnd)) wins++;
            return (float)wins / runs;
        }
        static bool BotPlay(BoardState s0, Random rnd)
        {
            var s = s0.Clone();
            for (int guard = 0; guard < 2000 && !s.Won && !s.over; guard++)
            {
                if (s.CanAuto) return true;
                var pr = s.Pairs(); if (pr.Count == 0) return false;
                var w = s.WantSet(); var good = pr.Where(m => w.Contains(s.Top(m.a).c)).ToList();
                var pool = good.Count > 0 && rnd.NextDouble() < .85 ? good : pr;
                var m2 = pool[rnd.Next(pool.Count)]; s.Apply(m2.a, m2.b, null);
            }
            return s.Won;
        }
    }
}
