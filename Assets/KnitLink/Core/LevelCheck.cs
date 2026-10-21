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
        public const int MaxW = 6, MaxH = 7, MaxColors = 5;

        public static LevelReport Check(LevelDef L, int shelfSlots, int solverBudget = 20000, int botRuns = 200)
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
            int depthRule = L.num > 0 && L.num < 16 ? 2 : 3;
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
