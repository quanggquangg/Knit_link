// GameController – Shelf full / no moves: revive popup, yarn bag, open the way (prices and limits in GameConfig).
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
        // ---------- revive (max 2 per picture; the 1st costs coins or one rewarded ad, the 2nd only coins). Popups: GameController.Popups.cs ----------
        void DoRevive(bool stuck)
        {
            CloseModal(); revives++; KLog.Info("Revive", $"Revive {revives} ({(stuck ? "open the way" : "yarn bag")}) on level {L.num}");
            if (stuck)
            {
                var picks = S.OpenPathPicks(); if (picks.Count == 0) { Toast("No yarn left to pull."); return; }
                var origins = new Dictionary<int, Vector3>();
                foreach (var p in picks) origins[p.id] = cells[p.t].rt.position;
                foreach (var p in picks) StartCoroutine(PulseRing(cells[p.t].rt, .6f));
                var ev = new List<GameEvent>(); S.Take(picks, ev); RenderBoosters();
                AfterAct(ev, origins, .35f);
                return;
            }
            // full shelf: go back to just before the move that overflowed, move 4 balls into the bag, then play that move again
            var h = history.Pop(); var (a, b) = lastMove;
            token++; StopAllCoroutines(); foreach (Transform c in fx) Destroy(c.gameObject);
            foreach (var v in basketViews.Values) Destroy(v.rt.gameObject); basketViews.Clear(); basketOrder.Clear(); busyBaskets.Clear(); leavingBaskets.Clear();
            S = h.s.Clone(); prog = (float[])h.prog.Clone(); moves = h.moves; peak = h.peak; pending = 0; sel = -1; posAnims.Clear();
            foreach (var cv in cells) { cv.shownTop = -1; cv.shownCount = 0; SnapHome(cv.front.rectTransform); SnapHome(cv.back.rectTransform); cv.front.rectTransform.localScale = Vector3.one; }
            for (int t = 0; t < cells.Count; t++) { cells[t].shownHp = S.tg[t]; cells[t].shownIce = S.Frozen(t) ? S.ice[t] : 0; }
            // the 4 oldest shelf balls (slots 0..3) fly into the yarn bag (GameController.Bag.cs), then the move is played again
            RenderAll(); var froms = new List<(Vector3 pos, Image slot)>(); for (int i = 0; i < Mathf.Min(Cfg.reviveBagSize, S.shelf.Count); i++) froms.Add((SlotPos(i), slots[i]));
            var bev = new List<GameEvent>(); S.BagTransfer(Cfg.reviveBagSize, bev);
            StartCoroutine(BagFlights(bev, froms, () =>
            {
                var path = S.FindPath(a, b);
                if (path != null && S.Top(a) != null && S.Top(b) != null && S.Top(a).c == S.Top(b).c) DoMove(a, b, path); else AfterSettle();
            }));
        }
        IEnumerator BagFlights(List<GameEvent> ev, List<(Vector3 pos, Image slot)> froms, System.Action then)
        {
            int my = token; pending++;
            var toBag = ev.Where(e => e.k == EvKind.ToBag).ToList();
            bagHold = Mathf.Max(0, S.bag.Count - toBag.Count); RenderBag();   // the chip keeps showing what it had before
            var items = toBag.Select((e, i) => (e.c, i < froms.Count ? froms[i].pos : shelfRow.position, i < froms.Count ? froms[i].slot : null)).ToList();
            yield return BagCollect(items, bagHold == 0);
            pending--; bagHold = -1;
            if (my != token) yield break;
            RenderShelf();
            var rest = ev.Where(e => e.k != EvKind.ToBag).ToList();
            if (rest.Count > 0) yield return Animate(rest, new Dictionary<int, Vector3>(), 0);
            then();
        }
    }
}
