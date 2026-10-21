// Knit Link – core rules (pure C#, no Unity dependency).
// Flat board, stacks of up to 3 balls per spot (only the front one can be played; the ones below are hidden),
// tangles, rolling yarn, ONE basket of 4 taking yarn at a time (the next 2 are shown as a preview),
// a 6-slot shelf, auto-finish, Onet line rule (max 2 bends), frozen yarn (thaws by 1 on every valid match anywhere).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KnitLink
{
    public class Ball
    {
        public int id; public string c; public bool hidden;
        public Ball Copy() => new Ball { id = id, c = c, hidden = hidden };
    }

    public class Basket
    {
        public string id; public int li; public string c; public int fill;
        public Basket Copy() => new Basket { id = id, li = li, c = c, fill = fill };
    }

    public enum EvKind { Leave, Jar, Shelf, Over, Absorb, Done, Reveal, Tangle, Roll, BagAbsorb, ToBag, Ice }

    public struct LeaveItem { public int t; public int id; public string c; }
    public struct RollMove { public int from, to; }

    public class GameEvent
    {
        public EvKind k;
        public string basketId; public int li; public string c;
        public int idx; public int t; public int hp;
        public List<LeaveItem> items; public List<RollMove> moves;
    }

    /// <summary>Board shape: spot index -> grid position, neighbours, lookup grid.</summary>
    public class Geometry
    {
        public int W, H, x0, y0;
        public int[] at;          // grid (W*H) -> spot index or -1 (gap)
        public int[][] xy;        // spot -> {x,y} relative to the bounding box
        public int[][] nb;        // spot -> 4-neighbour spots

        public static Geometry FromShape(string[] shape)
        {
            var cells = new List<int[]>();
            for (int y = 0; y < shape.Length; y++)
                for (int x = 0; x < shape[y].Length; x++)
                    if (shape[y][x] != '.') cells.Add(new[] { x, y });
            var g = new Geometry();
            g.x0 = cells.Min(c => c[0]); g.y0 = cells.Min(c => c[1]);
            g.W = cells.Max(c => c[0]) - g.x0 + 1; g.H = cells.Max(c => c[1]) - g.y0 + 1;
            g.at = Enumerable.Repeat(-1, g.W * g.H).ToArray();
            g.xy = new int[cells.Count][];
            for (int t = 0; t < cells.Count; t++)
            {
                int x = cells[t][0] - g.x0, y = cells[t][1] - g.y0;
                g.xy[t] = new[] { x, y }; g.at[y * g.W + x] = t;
            }
            g.nb = new int[cells.Count][];
            int[,] d = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
            for (int t = 0; t < cells.Count; t++)
            {
                var l = new List<int>();
                for (int k = 0; k < 4; k++)
                {
                    int X = g.xy[t][0] + d[k, 0], Y = g.xy[t][1] + d[k, 1];
                    if (X < 0 || Y < 0 || X >= g.W || Y >= g.H) continue;
                    int n = g.at[Y * g.W + X]; if (n >= 0) l.Add(n);
                }
                g.nb[t] = l.ToArray();
            }
            return g;
        }
        public int Count => xy.Length;
    }

    public class BoardState
    {
        public Geometry geo;
        public List<Ball>[] stacks;   // index 0 = back (undyed), last = front
        public int[] tg;              // tangle hit points per spot (0 = none)
        public int[] ice;             // frozen yarn: matches left before the front ball thaws (0 = not frozen)
        public string roll = "";      // "", "down", "up", "left", "right"
        public List<Basket> q = new List<Basket>();
        public int A = 1, cap = 6;          // A = baskets taking yarn at once (1); the next baskets are only a preview
        public List<string> shelf = new List<string>();
        /// <summary>Revive "yarn bag": balls moved off a full shelf. It never overflows; its balls hop into the basket of their color.</summary>
        public List<string> bag = new List<string>(); public int bagCap;
        public bool over;
        public List<int> done = new List<int>();

        // ---------- construction ----------
        public static BoardState FromLevel(LevelDef L)
        {
            var s = new BoardState { geo = Geometry.FromShape(L.shape), roll = L.roll ?? "" };
            int n = s.geo.Count, id = 1;
            s.stacks = new List<Ball>[n]; s.tg = new int[n]; s.ice = new int[n];
            for (int t = 0; t < n; t++)
            {
                s.stacks[t] = new List<Ball>();
                var balls = L.board.stacks[t].balls ?? new string[0];
                for (int i = 0; i < balls.Length; i++)
                    s.stacks[t].Add(new Ball { id = id++, c = balls[i], hidden = i < balls.Length - 1 });
                s.tg[t] = L.board.tangles != null && t < L.board.tangles.Length ? L.board.tangles[t] : 0;
                s.ice[t] = L.board.ice != null && t < L.board.ice.Length ? L.board.ice[t] : 0;
            }
            for (int i = 0; i < L.baskets.Length; i++) s.q.Add(new Basket { id = "j" + i, li = i, c = L.baskets[i] });
            return s;
        }

        public BoardState Clone()
        {
            var n = new BoardState { geo = geo, roll = roll, A = A, cap = cap, over = false };
            n.stacks = stacks.Select(st => st.Select(b => b.Copy()).ToList()).ToArray();
            n.tg = (int[])tg.Clone(); n.ice = (int[])ice.Clone();
            n.q = q.Select(j => j.Copy()).ToList();
            n.shelf = new List<string>(shelf); n.done = new List<int>(done); n.bag = new List<string>(bag); n.bagCap = bagCap;
            return n;
        }

        // ---------- queries ----------
        public Ball Top(int t) { var st = stacks[t]; return st.Count > 0 ? st[st.Count - 1] : null; }
        /// <summary>Frozen yarn can't be tapped, can't be matched, blocks the line and stays still when the board rolls.</summary>
        public bool Frozen(int t) => ice[t] > 0 && stacks[t].Count > 0;
        public bool Full(int t) => stacks[t].Count > 0 || tg[t] > 0;
        public int Active => Math.Min(A, q.Count);
        public bool Won => q.Count == 0;
        public int BoardLeft { get { int n = 0; foreach (var st in stacks) n += st.Count; return n; } }
        public bool CanAuto { get { int n = BoardLeft; return !over && !Won && n > 0 && n <= cap - shelf.Count; } }
        /// <summary>No pair can be joined any more and the level cannot finish by itself.</summary>
        public bool Stuck => !over && !Won && !CanAuto && Pairs().Count == 0;

        public HashSet<string> WantSet()
        {
            var w = new HashSet<string>();
            for (int i = 0; i < Active; i++) if (q[i].fill < 4) w.Add(q[i].c);
            return w;
        }

        // Classic Onet check: every <=2-bend line is A -> corner -> corner -> B (the outer ring is free).
        public bool Connected(int ta, int tb)
        {
            int W = geo.W, H = geo.H, ax = geo.xy[ta][0], ay = geo.xy[ta][1], bx = geo.xy[tb][0], by = geo.xy[tb][1];
            bool Free(int x, int y)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return true;
                if ((x == ax && y == ay) || (x == bx && y == by)) return true;
                int t = geo.at[y * W + x]; return t < 0 || !Full(t);
            }
            bool Row(int y, int x1, int x2) { for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++) if (!Free(x, y)) return false; return true; }
            bool Col(int x, int y1, int y2) { for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++) if (!Free(x, y)) return false; return true; }
            for (int x = -1; x <= W; x++) if (Row(ay, ax, x) && Col(x, ay, by) && Row(by, x, bx)) return true;
            for (int y = -1; y <= H; y++) if (Col(ax, ay, y) && Row(y, ax, bx) && Col(bx, y, by)) return true;
            return false;
        }

        /// <summary>Shortest line with at most 2 bends; corner points in grid coords (may be -1 or W/H = outside). Null if none.</summary>
        public List<(int x, int y)> FindPath(int ta, int tb)
        {
            int W = geo.W, H = geo.H, ax = geo.xy[ta][0], ay = geo.xy[ta][1], bx = geo.xy[tb][0], by = geo.xy[tb][1];
            bool Blocked(int x, int y)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return false;
                int t = geo.at[y * W + x]; if (t < 0 || t == tb) return false; if (t == ta) return true; return Full(t);
            }
            int[] DX = { 1, 0, -1, 0 }, DY = { 0, 1, 0, -1 };
            var nodes = new List<(int x, int y, int d, int turns, int p)>();
            var best = new Dictionary<int, int>();
            for (int d = 0; d < 4; d++) nodes.Add((ax, ay, d, 0, -1));
            for (int qi = 0; qi < nodes.Count; qi++)
            {
                var n = nodes[qi]; int nx = n.x + DX[n.d], ny = n.y + DY[n.d];
                if (nx < -1 || ny < -1 || nx > W || ny > H) continue;
                if (nx == bx && ny == by)
                {
                    var pts = new List<(int x, int y)> { (bx, by) };
                    for (int cur = qi; cur >= 0; cur = nodes[cur].p) pts.Add((nodes[cur].x, nodes[cur].y));
                    pts.Reverse();
                    var outp = new List<(int x, int y)> { pts[0] };
                    for (int i = 1; i < pts.Count - 1; i++)
                    {
                        var a = outp[outp.Count - 1]; var b = pts[i]; var c = pts[i + 1];
                        if ((a.x == b.x && b.x == c.x) || (a.y == b.y && b.y == c.y)) continue;
                        outp.Add(b);
                    }
                    outp.Add(pts[pts.Count - 1]);
                    return outp;
                }
                if (Blocked(nx, ny)) continue;
                for (int nd = 0; nd < 4; nd++)
                {
                    if (nd == (n.d + 2) % 4) continue;
                    int nt = n.turns + (nd == n.d ? 0 : 1); if (nt > 2) continue;
                    int key = ((ny + 1) * (W + 2) + (nx + 1)) * 4 + nd;
                    if (best.TryGetValue(key, out var bv) && bv <= nt) continue;
                    best[key] = nt; nodes.Add((nx, ny, nd, nt, qi));
                }
            }
            return null;
        }

        public List<(int a, int b)> Pairs()
        {
            var by = new Dictionary<string, List<int>>(); var outp = new List<(int a, int b)>();
            for (int t = 0; t < stacks.Length; t++)
            {
                var b = Top(t); if (b == null || Frozen(t)) continue;
                if (!by.TryGetValue(b.c, out var l)) by[b.c] = l = new List<int>();
                l.Add(t);
            }
            foreach (var l in by.Values)
                for (int i = 0; i < l.Count; i++)
                    for (int j = i + 1; j < l.Count; j++)
                        if (Connected(l[i], l[j])) outp.Add((l[i], l[j]));
            return outp;
        }

        // ---------- rules ----------
        void Give(string c, List<GameEvent> ev)
        {
            for (int i = 0; i < Active; i++)
            {
                var j = q[i];
                if (j.c == c && j.fill < 4) { j.fill++; ev?.Add(new GameEvent { k = EvKind.Jar, basketId = j.id, c = c }); Settle(ev); return; }
            }
            if (shelf.Count >= cap) { over = true; ev?.Add(new GameEvent { k = EvKind.Over, c = c }); return; }
            shelf.Add(c); ev?.Add(new GameEvent { k = EvKind.Shelf, c = c, idx = shelf.Count - 1 });
        }

        public void Settle(List<GameEvent> ev)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < Active; i++)
                    if (q[i].fill >= 4)
                    {
                        var j = q[i]; q.RemoveAt(i); done.Add(j.li);
                        ev?.Add(new GameEvent { k = EvKind.Done, basketId = j.id, li = j.li, c = j.c });
                        changed = true; break;
                    }
                if (changed) continue;
                for (int i = 0; i < Active; i++)
                {
                    var j = q[i];
                    while (j.fill < 4)
                    {
                        int k = shelf.IndexOf(j.c); if (k < 0) break;
                        shelf.RemoveAt(k); j.fill++;
                        ev?.Add(new GameEvent { k = EvKind.Absorb, basketId = j.id, c = j.c });
                    }
                    while (j.fill < 4)
                    {
                        int k = bag.IndexOf(j.c); if (k < 0) break;
                        bag.RemoveAt(k); j.fill++;
                        ev?.Add(new GameEvent { k = EvKind.BagAbsorb, basketId = j.id, c = j.c, idx = k });
                    }
                    if (j.fill >= 4) changed = true;
                }
            }
        }


        static readonly Dictionary<string, (int dx, int dy)> DIRS = new Dictionary<string, (int dx, int dy)>
        { { "down", (0, 1) }, { "up", (0, -1) }, { "left", (-1, 0) }, { "right", (1, 0) } };

        /// <summary>Every stack rolls as one piece until it meets the edge, a gap, a tangle or another ball.</summary>
        public void Roll(List<GameEvent> ev)
        {
            if (string.IsNullOrEmpty(roll) || !DIRS.TryGetValue(roll, out var dir)) return;
            var order = Enumerable.Range(0, stacks.Length)
                .OrderByDescending(t => geo.xy[t][0] * dir.dx + geo.xy[t][1] * dir.dy).ToList();
            var moves = new List<RollMove>();
            foreach (int t in order)
            {
                if (stacks[t].Count == 0 || Frozen(t)) continue;
                int x = geo.xy[t][0], y = geo.xy[t][1], cur = t;
                while (true)
                {
                    int X = x + dir.dx, Y = y + dir.dy;
                    if (X < 0 || Y < 0 || X >= geo.W || Y >= geo.H) break;
                    int n = geo.at[Y * geo.W + X]; if (n < 0 || Full(n)) break;
                    stacks[n] = stacks[cur]; stacks[cur] = new List<Ball>(); cur = n; x = X; y = Y;
                }
                if (cur != t) moves.Add(new RollMove { from = t, to = cur });
            }
            if (moves.Count > 0) ev?.Add(new GameEvent { k = EvKind.Roll, moves = moves });
        }

        void AfterLeave(IEnumerable<int> from, List<GameEvent> ev)
        {
            var list = from.ToList();
            foreach (int t in list)
            {
                var b = Top(t);
                if (b != null && b.hidden) { b.hidden = false; ev?.Add(new GameEvent { k = EvKind.Reveal, t = t, c = b.c }); }
            }
            foreach (int t in list)
                foreach (int n in geo.nb[t])
                    if (tg[n] > 0) { tg[n]--; ev?.Add(new GameEvent { k = EvKind.Tangle, t = n, hp = tg[n] }); }
            // every valid match thaws frozen yarn by one
            for (int t = 0; t < ice.Length; t++)
                if (ice[t] > 0) { ice[t]--; ev?.Add(new GameEvent { k = EvKind.Ice, t = t, hp = ice[t] }); }
            Roll(ev);
        }

        /// <summary>Match the front balls of two spots.</summary>
        public void Apply(int a, int b, List<GameEvent> ev)
        {
            var leaving = new List<(int t, Ball b)>();
            foreach (int t in new[] { a, b }) { var st = stacks[t]; var ball = st[st.Count - 1]; st.RemoveAt(st.Count - 1); leaving.Add((t, ball)); }
            ev?.Add(new GameEvent { k = EvKind.Leave, items = leaving.Select(x => new LeaveItem { t = x.t, id = x.b.id, c = x.b.c }).ToList() });
            AfterLeave(new[] { a, b }, ev);
            foreach (var x in leaving) { if (over) break; Give(x.b.c, ev); }
        }

        /// <summary>Revive for a full shelf: the bag grows by 4 and takes the 4 balls that waited longest on the shelf.</summary>
        public void BagTransfer(int n, List<GameEvent> ev)
        {
            bagCap += n;
            for (int i = 0; i < n && shelf.Count > 0 && bag.Count < bagCap; i++)
            {
                string c = shelf[0]; shelf.RemoveAt(0); bag.Add(c);
                ev?.Add(new GameEvent { k = EvKind.ToBag, c = c, idx = i });
            }
            Settle(ev);
        }

        /// <summary>Take specific balls (front or hidden below) ignoring the line rule.</summary>
        public void Take(List<(int t, int id)> picks, List<GameEvent> ev)
        {
            var items = new List<(int t, Ball b)>();
            foreach (var p in picks)
            {
                var st = stacks[p.t]; int k = st.FindIndex(x => x.id == p.id); if (k < 0) continue;
                var ball = st[k]; st.RemoveAt(k); items.Add((p.t, ball));
                if (st.Count == 0) ice[p.t] = 0;   // the ice goes with its ball
            }
            if (items.Count == 0) return;
            ev?.Add(new GameEvent { k = EvKind.Leave, items = items.Select(x => new LeaveItem { t = x.t, id = x.b.id, c = x.b.c }).ToList() });
            AfterLeave(items.Select(x => x.t).Distinct(), ev);
            foreach (var x in items) { if (over) break; Give(x.b.c, ev); }
        }

        /// <summary>Revive for "out of moves": up to 2 pairs of the color the basket wants, front balls first, then the shallowest hidden ones.</summary>
        public List<(int t, int id)> OpenPathPicks()
        {
            var picks = new List<(int t, int id)>(); var used = new HashSet<int>();
            for (int pair = 0; pair < 2; pair++)
            {
                // the color still needed after the picks so far
                var sim = Clone(); sim.Take(picks, null);
                string c = null;
                foreach (var j in sim.q.Take(Math.Max(1, sim.Active) + 2))
                {
                    int have = 0; foreach (var st in sim.stacks) foreach (var b in st) if (b.c == j.c) have++;
                    if (have >= 2) { c = j.c; break; }
                }
                if (c == null) break;
                var cand = new List<(int t, int id, int d)>();
                for (int t = 0; t < stacks.Length; t++)
                    for (int k = 0; k < stacks[t].Count; k++)
                        if (stacks[t][k].c == c && !used.Contains(stacks[t][k].id)) cand.Add((t, stacks[t][k].id, stacks[t].Count - 1 - k));
                foreach (var x in cand.OrderBy(x => x.d).Take(2)) { picks.Add((x.t, x.id)); used.Add(x.id); }
            }
            return picks;
        }

        public void AutoFinish(List<GameEvent> ev)
        {
            for (int t = 0; t < ice.Length; t++)
                if (ice[t] > 0) { ice[t] = 0; ev?.Add(new GameEvent { k = EvKind.Ice, t = t, hp = 0 }); }
            while (BoardLeft > 0 && !over)
            {
                var want = WantSet();
                int t = Array.FindIndex(stacks, st => st.Count > 0 && want.Contains(st[st.Count - 1].c));
                if (t < 0) t = Array.FindIndex(stacks, st => st.Count > 0);
                var stk = stacks[t]; var b = stk[stk.Count - 1]; stk.RemoveAt(stk.Count - 1);
                ev?.Add(new GameEvent { k = EvKind.Leave, items = new List<LeaveItem> { new LeaveItem { t = t, id = b.id, c = b.c } } });
                var nb = Top(t); if (nb != null && nb.hidden) { nb.hidden = false; ev?.Add(new GameEvent { k = EvKind.Reveal, t = t, c = nb.c }); }
                Give(b.c, ev);
            }
        }

        // ---------- solver (level checks, Hint) ----------
        string Key()
        {
            var sb = new StringBuilder();
            for (int t = 0; t < stacks.Length; t++)
            {
                var st = stacks[t];
                if (st.Count > 0) { sb.Append(st[st.Count - 1].id).Append('^').Append(st.Count); if (Frozen(t)) sb.Append('i').Append(ice[t]); }
                else sb.Append(tg[t] > 0 ? "T" + tg[t] : "_");
                sb.Append(',');
            }
            sb.Append('|').Append(q.Count).Append(':');
            for (int i = 0; i < Math.Min(3, q.Count); i++) sb.Append(q[i].fill).Append(',');
            sb.Append('|').Append(string.Join(",", shelf.OrderBy(x => x))).Append('|').Append(string.Join(",", bag.OrderBy(x => x))).Append('|').Append(A).Append(cap);
            return sb.ToString();
        }

        public static List<(int a, int b)> Solve(BoardState s0, int limit, out bool exhausted)
        {
            int nodes = 0; var memo = new HashSet<string>(); bool ex = false;
            List<(int a, int b)> Dfs(BoardState s, List<(int a, int b)> path)
            {
                if (s.Won || s.CanAuto) return path;
                if (++nodes > limit) { ex = true; return null; }
                if (!memo.Add(s.Key())) return null;
                var w = s.WantSet();
                var moves = s.Pairs().Select(m => (m, sc: s.Score(m, w))).OrderByDescending(x => x.sc).ToList();
                foreach (var mv in moves)
                {
                    var n = s.Clone(); n.Apply(mv.m.a, mv.m.b, null); if (n.over) continue;
                    var r = Dfs(n, new List<(int a, int b)>(path) { mv.m }); if (r != null) return r;
                    if (nodes > limit) return null;
                }
                return null;
            }
            var res = Dfs(s0.Clone(), new List<(int a, int b)>());
            exhausted = ex; return res;
        }

        int Score((int a, int b) m, HashSet<string> w)
        {
            int sc = 0; string c = Top(m.a).c;
            if (w.Contains(c)) sc += 4;
            else sc += q.Skip(Active).Take(2).Any(j => j.c == c) ? 1 : -2;
            foreach (int t in new[] { m.a, m.b })
            {
                if (stacks[t].Count > 1) sc += 1;
                foreach (int n in geo.nb[t]) if (tg[n] > 0) sc += 1;
            }
            return sc;
        }

        /// <summary>Hint: the next move of a winning line (solver). Falls back to a pair for the open basket, then any pair. Null = no pair.</summary>
        public (int a, int b)? Hint(int budget = 20000)
        {
            var sol = Solve(this, budget, out _);
            if (sol != null && sol.Count > 0) return sol[0];
            var pr = Pairs(); if (pr.Count == 0) return null;
            var w = WantSet(); foreach (var m in pr) if (w.Contains(Top(m.a).c)) return m;
            return pr[0];
        }
    }
}
