// GameController – Undo, Hint, Extra Shelf: gate (locked / free tutorial use / paid), using them, buying with coins or a rewarded ad.
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
        // ---------- boosters ----------
        /// <summary>0 = blocked, 1 = use (paid), 2 = free guided first use.</summary>
        int Gate(string k)
        {
            if (S == null || S.Won || S.over || modalRT.gameObject.activeSelf || Time.unscaledTime < introUntil) return 0;
            if (coach != null && coach.booster != k) return 0;
            if (Locked(k)) { Toast($"{BName[k]} unlocks at level {Unlock[k]}."); return 0; }
            if (coach != null && coach.booster == k) return 2;
            if (Inv(k) <= 0) { OfferBooster(k); return 0; }
            return 1;
        }
        void Spend(string k, int g)
        {
            if (g == 2) ResolveCoach(); else SetInv(k, Inv(k) - 1);
            RenderBoosters();
        }

        void UseUndo()
        {
            if (S == null || S.Won || S.over || modalRT.gameObject.activeSelf) return;
            if (autoRun || pending > 0) { Toast("Wait for the yarn to land."); return; }
            if (history.Count == 0) { if (!Locked("undo")) Toast("Nothing to undo yet."); else Gate("undo"); return; }
            int g = Gate("undo"); if (g == 0) return;
            var h = history.Pop(); int cap = S.cap;
            token++; StopAllCoroutines(); foreach (Transform c in fx) Destroy(c.gameObject);
            foreach (var v in basketViews.Values) Destroy(v.rt.gameObject); basketViews.Clear(); basketOrder.Clear(); busyBaskets.Clear(); leavingBaskets.Clear();
            S = h.s; S.cap = Mathf.Max(S.cap, cap); prog = h.prog; moves = h.moves; peak = h.peak; pending = 0; sel = -1;
            posAnims.Clear();
            foreach (var cv in cells) { cv.shownTop = -1; cv.shownCount = 0; cv.shownHp = 0; SnapHome(cv.front.rectTransform); SnapHome(cv.back.rectTransform); cv.front.rectTransform.localScale = Vector3.one; }
            toastRT.gameObject.SetActive(false);
            RenderAll(); Spend("undo", g); KLog.Info("Booster", "Undo used");
            for (int t = 0; t < cells.Count; t++) { cells[t].shownHp = S.tg[t]; cells[t].shownIce = S.Frozen(t) ? S.ice[t] : 0; }
        }

        void UseHint()
        {
            if (S == null || S.Won || S.over || modalRT.gameObject.activeSelf) return;
            if (autoRun || pending > 0) { Toast("Wait for the yarn to land."); return; }
            if (Locked("hint")) { Gate("hint"); return; }
            var m = S.Hint(Cfg.solverBudget);
            if (m == null) { Toast("No pair can be joined any more."); return; }
            int g = Gate("hint"); if (g == 0) return;
            Spend("hint", g);
            var (a, b) = m.Value; sel = -1; RenderBoard();
            DrawLink(S.FindPath(a, b), S.Top(a).c);
            StartCoroutine(PulseRing(cells[a].rt, 2.6f)); StartCoroutine(PulseRing(cells[b].rt, 2.6f));
        }

        void UseShelf()
        {
            if (S == null || S.Won || S.over || modalRT.gameObject.activeSelf || autoRun) return;
            if (!Locked("shelf") && S.cap > Cfg.shelfSlots) { Toast("Extra Shelf is already in use in this picture."); return; }
            int g = Gate("shelf"); if (g == 0) return;
            Spend("shelf", g); S.cap = Cfg.shelfSlots + Cfg.extraShelfSlots; KLog.Info("Booster", "Extra Shelf used"); StartCoroutine(UnlockSlots()); RenderShelf(); RenderBoosters(); MaybeAuto(); CheckStuck();
        }

        /// <summary>Extra Shelf: the padlocks of the 2 locked slots pop off in place; nothing else moves.</summary>
        IEnumerator UnlockSlots()
        {
            var locks = new List<(Image im, RectTransform rt)>();
            for (int i = Cfg.shelfSlots; i < slotLocks.Count; i++)
            {   // a copy of the padlock flies off; the real one is already hidden by RenderShelf
                var c = UIKit.Img("LockOff", fx, UIKit.IconLock, UIKit.Hex("#c9a77f")); c.rectTransform.sizeDelta = new Vector2(46, 46); c.rectTransform.position = slotLocks[i].rectTransform.position;
                locks.Add((c, c.rectTransform));
            }
            float k = canvas.scaleFactor; var start = locks.Select(l => l.rt.position).ToList();
            for (float t = 0; t < .5f; t += Time.unscaledDeltaTime)
            {
                float p = t / .5f;
                for (int i = 0; i < locks.Count; i++) { locks[i].rt.position = start[i] + Vector3.up * KF.At(p, 0, 0, .3f, 11, 1, 39) * k; locks[i].rt.localScale = Vector3.one * KF.At(p, 0, 1, .3f, 1.2f, 1, .6f); locks[i].im.color = new Color(.79f, .65f, .5f, 1 - Mathf.Max(0, p - .3f) / .7f); }
                yield return null;
            }
            foreach (var l in locks) if (l.im != null) Destroy(l.im.gameObject);
        }
        // buying boosters (coins / rewarded ad) and the ad stand-in live in GameController.Popups.cs
    }
}
