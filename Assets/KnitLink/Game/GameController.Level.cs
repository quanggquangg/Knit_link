// GameController – Starting a level and laying out the board spots (size, positions, grid coordinates for the yarn line).
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
        // ---------- levels ----------
        void StartLevel(int li)
        {
            token++; StopAllCoroutines(); foreach (Transform c in fx) Destroy(c.gameObject);
            levelIndex = li; L = file.levels[li]; PlayerPrefs.SetInt("kl.level", li);
            S = BoardState.FromLevel(L); S.cap = Cfg.shelfSlots; winShown = false; winCollecting = false; bagHold = -1; history.Clear(); EndCoach(); tipShelfPending = false; revives = 0; lastMove = (-1, -1);
            sel = -1; moves = 0; peak = 0; pending = 0; autoRun = false;
            prog = new float[L.baskets.Length]; busyBaskets.Clear(); leavingBaskets.Clear();
            foreach (var v in basketViews.Values) Destroy(v.rt.gameObject); basketViews.Clear(); basketOrder.Clear();
            // a booster that unlocked at an earlier level the player skipped is simply given (+2)
            foreach (var k in BKeys) if (L.num > Unlock[k] && !Claimed(k)) { PlayerPrefs.SetInt("kl.claimed." + k, 1); SetInv(k, Inv(k) + Cfg.boostersOnUnlock); }
            // "Level N" only – no subtitle under it (decided 20/10); hard levels get a pink title
            title.text = $"Level {L.num}";
            title.color = L.kind == "hard" || L.kind == "superhard" ? UIKit.Hex("#ff9aa8") : Pale;
            SkinLevelTitle();   // new art: white title, "Hard" chip, picture name in yellow capitals (GameController.Skin.cs)
            rollChip.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(S.roll));
            rollChip.text = string.IsNullOrEmpty(S.roll) ? "" : $"ROLLS {S.roll.ToUpper()} {Arrow(S.roll)}";
            toastRT.gameObject.SetActive(false); CloseModal();
            BuildBoard(); RenderAll();
            if (!devSkipTutorials) PlayLevelIntro(); else introUntil = 0;
            KLog.Info("Level", $"Start level {L.num} ({L.file}, {L.name}, {L.kind})");
            if (!devSkipTutorials) StartCoroutine(RunIntro(token));
        }
        static string Arrow(string d) => d == "down" ? "↓" : d == "up" ? "↑" : d == "left" ? "←" : "→";

        void BuildBoard()
        {
            foreach (var c in cells) Destroy(c.rt.gameObject); cells.Clear(); posAnims.Clear();
            Canvas.ForceUpdateCanvases();
            var g = S.geo; var area = boardPanel.rect;
            // free space between the yarn and the board edge: the same on all 4 sides in the new art (SkinLay.BoardPad)
            float padX = SK ? 2 * SkinLay.BoardPad : 60, padY = SK ? 2 * SkinLay.BoardPad : 90;
            float aw = area.width - padX, ah = area.height - padY;
            cs = Mathf.Min(aw / (g.W + GAP * (g.W - 1)), ah / (g.H * CELL_H + GAP * (g.H - 1)), 220);
            float stepX = cs * (1 + GAP), stepY = cs * CELL_H + cs * GAP;
            board.sizeDelta = new Vector2(g.W * stepX - cs * GAP, g.H * stepY - cs * GAP); board.anchoredPosition = new Vector2(0, SK ? 0 : -15);   // new art: exactly centred
            for (int t = 0; t < g.Count; t++)
            {
                int tt = t; var v = new CellView();
                v.rt = UIKit.Rect("Spot" + t, board); v.rt.anchorMin = v.rt.anchorMax = new Vector2(0, 1); v.rt.pivot = new Vector2(.5f, .5f);
                v.rt.sizeDelta = new Vector2(cs, cs * CELL_H); v.rt.anchoredPosition = CellPos(t);
                v.hit = v.rt.gameObject.AddComponent<Image>(); v.hit.color = new Color(1, 1, 1, 0); v.hit.raycastTarget = true;
                v.rt.gameObject.AddComponent<Button>().onClick.AddListener(() => OnTap(tt));
                v.shadow = Sub(v.rt, UIKit.Circle, new Color(.63f, .5f, .35f, .12f), .8f, 0, 0);
                v.selRing = Sub(v.rt, UIKit.Ring, UIKit.Hex("#7fa3e8"), 1.02f, 0, 0);
                v.front = Sub(v.rt, UIKit.Ball, Color.white, .84f, 0, 0);
                // stack badge: how many balls wait below the front one (their colors stay hidden)
                v.back = Sub(v.rt, UIKit.Circle, UIKit.Ink, .34f, .3f, .3f);
                var ring = Sub(v.back.rectTransform, UIKit.Ring, UIKit.Hex("#fffaf0"), .34f, 0, 0); ring.rectTransform.anchorMin = Vector2.zero; ring.rectTransform.anchorMax = Vector2.one; ring.rectTransform.sizeDelta = Vector2.zero;
                v.backCount = UIKit.Txt("N", v.back.transform, "1", Mathf.RoundToInt(cs * .22f), UIKit.Hex("#fffaf0")); UIKit.Stretch(v.backCount.rectTransform);
                v.knot = Sub(v.rt, UIKit.KnotSprite(false), Color.white, .9f, 0, 0);
                // frozen yarn (23/10): an OPAQUE ice block covers the whole spot – ball color and stack number stay hidden until it thaws
                v.ice = Sub(v.rt, UIKit.Rounded, IceBlock, .98f, 0, 0);
                var shine = Sub(v.ice.rectTransform, UIKit.Rounded, new Color(1, 1, 1, .55f), .5f, -.12f, .14f); shine.rectTransform.sizeDelta = new Vector2(cs * .42f, cs * .1f); shine.rectTransform.localEulerAngles = new Vector3(0, 0, 35); shine.raycastTarget = false;
                v.iceCount = UIKit.Txt("IceN", v.ice.transform, "3", Mathf.RoundToInt(cs * .42f), UIKit.Hex("#16457a")); UIKit.Stretch(v.iceCount.rectTransform);
                var iceOl = v.iceCount.gameObject.AddComponent<Outline>(); iceOl.effectColor = Color.white; iceOl.effectDistance = new Vector2(3, -3);
                v.ice.gameObject.SetActive(false);
                SkinCell(v);
                cells.Add(v);
            }
            BuildMech();   // needle box lid + conveyor belt strip (GameController.Mech.cs)
        }
        Image Sub(RectTransform parent, Sprite sp, Color col, float size, float ox, float oy)
        {
            var im = UIKit.Img(sp.name, parent, sp, col); var rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.sizeDelta = new Vector2(cs * size, cs * size); rt.anchoredPosition = new Vector2(cs * ox, cs * oy);
            return im;
        }
        Vector2 CellPos(int t) { var xy = S.geo.xy[t]; return new Vector2(xy[0] * cs * (1 + GAP) + cs / 2, -(xy[1] * (cs * CELL_H + cs * GAP) + cs * CELL_H / 2)); }
        Vector2 GridPos(float x, float y)
        {   // path corner in board space; outside the board the line runs just past the edge
            var g = S.geo; float m = Mathf.Min(cs * .22f, 30), bw = board.sizeDelta.x, bh = board.sizeDelta.y;
            float px = x < 0 ? -m : x >= g.W ? bw + m : x * cs * (1 + GAP) + cs / 2;
            float py = y < 0 ? -m : y >= g.H ? bh + m : y * (cs * CELL_H + cs * GAP) + cs * CELL_H / 2;
            return new Vector2(px, -py);
        }
    }
}
