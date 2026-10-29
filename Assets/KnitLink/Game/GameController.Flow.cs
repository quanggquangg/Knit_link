// GameController – Level flow: auto-finish (switched off 28/10, BoardState.AutoFinishOn), out-of-moves check, progress save. (Win / retry popups: GameController.Popups.cs)
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
        // ---------- auto-finish & stuck ----------
        void MaybeAuto()
        {
            if (autoRun || pending > 0 || S == null || !S.CanAuto) return;
            autoRun = true; sel = -1; int my = token; Sfx.Play(Sfx.Boost);
            StartCoroutine(Delay(.65f, () =>
            {
                if (my != token) { autoRun = false; return; }
                var origins = new Dictionary<int, Vector3>();
                for (int t = 0; t < S.stacks.Length; t++) foreach (var b in S.stacks[t]) origins[b.id] = cells[t].front.rectTransform.position;
                var ev = new List<GameEvent>(); S.AutoFinish(ev); RenderBoard();
                StartCoroutine(AnimateThen(ev, origins, 0, () => { autoRun = false; if (my == token) AfterSettle(); }));
            }));
        }
        void CheckStuck()
        {
            if (S == null || autoRun || pending > 0 || modalRT.gameObject.activeSelf || !S.Stuck) return;
            StartCoroutine(Delay(.35f, () => { if (S != null && !autoRun && pending == 0 && !modalRT.gameObject.activeSelf && S.Stuck) ShowStuck(); }));
        }

        void AfterSettle()
        {
            if (S == null || S.over) return;
            if (S.Won) { if (pending == 0 && busyBaskets.Count == 0) StartCoroutine(Delay(.3f, ShowWin)); return; }
            MaybeAuto(); CheckStuck(); CheckArmed();
        }
        // ---------- progress (build 1: no Home, no lives) ----------
        static bool Beaten(int num) => PlayerPrefs.GetInt("kl.beaten." + num, 0) == 1;
        void MarkBeaten(int li) { PlayerPrefs.SetInt("kl.beaten." + file.levels[li].num, 1); }
        void ResetTutorials()
        {
            foreach (var k in BKeys) { PlayerPrefs.DeleteKey("kl.inv." + k); PlayerPrefs.DeleteKey("kl.claimed." + k); PlayerPrefs.DeleteKey("kl.coached." + k); }
            foreach (var k in new[] { "howto", "tip.shelf", "tip.gaps", "mech.hidden", "mech.mystery", "mech.tangles", "mech.deep", "mech.ice", "mech.box", "mech.belt", "roll.down", "roll.left", "roll.up", "roll.right" }) PlayerPrefs.DeleteKey("kl.seen." + k);
            StartLevel(0);
        }
    }
}
