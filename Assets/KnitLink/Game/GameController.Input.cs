// GameController – Taps on the board: select, match, no-path feedback (no text since 21/10: buzz + shake + red flash, see
// GameController.Mech.cs), then hand the move to the rules (YarnCore) and start the animation.
// (GameController is split into GameController.*.cs files by feature; all parts share the same fields.)
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        // ---------- input ----------
        void OnTap(int t)
        {
            if (S == null || S.over || S.Won || modalRT.gameObject.activeSelf || autoRun) return;
            if (Time.unscaledTime < introUntil) return;   // yarn is still dropping in
            if (coach != null && (coach.booster != null || !coach.trays.Contains(t))) return;
            if (S.Boxed(t)) { BoxNudge(); return; }   // under the closed needle box
            if (S.tg[t] > 0) { Buzz(); Sfx.Play(Sfx.Err); Shake(cells[t].knot.rectTransform); Toast("Tangle! Clear 2 balls next to it to pull it loose."); return; }
            if (S.Frozen(t)) { Buzz(); Sfx.Play(Sfx.Err); Shake(cells[t].ice.rectTransform); Toast($"Frozen! It thaws after {S.ice[t]} more {(S.ice[t] > 1 ? "matches" : "match")}."); return; }
            var b = S.Top(t); if (b == null) return;
            if (sel < 0) { Sfx.Play(Sfx.Tap); sel = t; RenderBoard(); CoachHand(); return; }
            if (sel == t) { Sfx.Play(Sfx.Tap); sel = -1; RenderBoard(); CoachHand(); return; }
            var a = S.Top(sel);
            if (a != null && a.c == b.c)
            {
                var path = S.FindPath(sel, t);
                if (path == null)
                {
                    int s0 = sel; sel = -1; RenderBoard(); NoPathFeedback(s0, t);
                    if (coach != null && coach.nopath) ResolveCoach();
                    return;
                }
                if (coach != null) ResolveCoach();
                DoMove(sel, t, path); return;
            }
            Sfx.Play(Sfx.Tap); sel = t; RenderBoard(); CoachHand();
        }

        void DoMove(int a, int b, List<(int x, int y)> path)
        {
            var origins = Origins(new[] { a, b }); var ev = new List<GameEvent>();
            history.Push((S.Clone(), (float[])prog.Clone(), moves, peak)); lastMove = (a, b);
            string c = S.Top(a).c;
            KLog.Info("Move", $"Match {c} spots {a}+{b} (move {moves + 1}, shelf {S.shelf.Count}/{S.cap})");
            S.Apply(a, b, ev); moves++; Sfx.Play(Sfx.Link); Haptics.Light();
            DrawLink(path, c);
            AfterAct(ev, origins, .2f);
        }

        Dictionary<int, Vector3> Origins(IEnumerable<int> trays)
        {
            var m = new Dictionary<int, Vector3>();
            foreach (int t in trays) { var b = S.Top(t); if (b != null) m[b.id] = cells[t].front.rectTransform.position; }
            return m;
        }

        void AfterAct(List<GameEvent> ev, Dictionary<int, Vector3> origins, float start)
        {
            sel = -1; peak = Mathf.Max(peak, S.shelf.Count);
            bool over = S.over;
            StartCoroutine(ShowBoardAfter(ev, start));
            if (tipShelfPending && ev.Any(e => e.k == EvKind.Shelf)) { tipShelfPending = false; StartCoroutine(Delay(.7f, () => HintBubble(shelfRow, "No basket wants this color yet, so the pair waits on the shelf. It hops in when its basket comes. Don't let the shelf fill up!", 4.6f))); }
            StartCoroutine(AnimateThen(ev, origins, start, () =>
            {
                if (over) { StartCoroutine(Delay(.45f, ShowFail)); return; }
                AfterSettle();
            }));
        }
        IEnumerator ShowBoardAfter(List<GameEvent> ev, float start)
        {
            if (start > 0) { foreach (var c in cells) c.selRing.gameObject.SetActive(false); yield return new WaitForSecondsRealtime(start); }
            RenderBoard(); AnimateRoll(ev); AnimateBelt(ev);
        }
    }
}
