// GameController – "New item" popups with a looping demo (design canvas ItemIntro, 20/10):
//   hidden yarn (Lv6) · deeper stacks (Lv8, was 16) · tangles (Lv12) · frozen yarn (Lv21) · yarn rolls down/left/up/right (Lv24/27/36/48)
//   · needle box (Lv32) · conveyor belt (Lv43)
// Each demo is built from plain images and animated by LoopAnim + KF keyframes (times are fractions of the loop).
// To add an item: add a row to IntroText and a case in BuildIntroDemo.
// (GameController is split into GameController.*.cs files by feature; all parts share the same fields.)
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        // kind -> (title, short text, hint shown on the board afterwards)
        static readonly Dictionary<string, (string title, string text, string hint)> IntroText = new Dictionary<string, (string, string, string)>
        {
            { "hidden",    ("Hidden yarn",      "The number shows how many balls hide below.", "This stack hides more yarn below.") },
            { "tangles",   ("Tangles",          "Clear 2 balls next to it to untangle.",      "Clear 2 balls next to this tangle.") },
            { "deep",      ("Deeper stacks",    "Now up to 2 balls can hide below.",          "2 more balls wait under this one.") },
            { "ice",       ("Frozen yarn",      "Each match melts the ice by 1.",             "Join pairs anywhere to melt this ice.") },
            { "roll.down", ("Yarn rolls down",  "After each match, yarn rolls down.",         "The whole picture rolls down. Watch the board after each move.") },
            { "roll.left", ("Yarn rolls left",  "After each match, yarn rolls left.",         "The whole picture rolls left. Watch the board after each move.") },
            { "roll.up",   ("Yarn rolls up",    "After each match, yarn rolls up.",           "The whole picture rolls up. Watch the board after each move.") },
            { "roll.right",("Yarn rolls right", "After each match, yarn rolls right.",        "The whole picture rolls right. Watch the board after each move.") },
            { "box",       ("Needle box",       "Match this color to open the box.",          "Match yarn of this color anywhere. At 0 the box opens.") },
            { "belt",      ("Conveyor belt",    "This row slides 1 spot each match.",         "This row slides 1 spot after every match.") },
        };

        /// <summary>type = "mech" or "roll" (from the level file), key = hidden/tangles/deep/ice or down/left/up/right.</summary>
        void MechIntro(string type, string key)
        {
            string kind = type == "roll" ? "roll." + key : key;
            if (!IntroText.TryGetValue(kind, out var info)) { KLog.Warn("Tutorial", $"No intro popup for '{kind}'"); return; }
            OpenCard(c =>
            {
                c.Pill($"New  ·  Level {L.num}", PillStyle.Blue); c.Title(info.title);
                var box = c.Row(526, 38); var bg = UIKit.Img("DemoBox", box, UIKit.Rounded, UIKit.Hex("#efe2c6")); UIKit.Stretch(bg.rectTransform);
                box.gameObject.AddComponent<RectMask2D>();
                BuildIntroDemo(kind, box);
                c.Lead(info.text);
                c.BigButton("Got it", () =>
                {
                    CloseModal(); var target = IntroTarget(kind);
                    if (target != null) StartCoroutine(Delay(.25f, () => { if (target != null) { StartCoroutine(PulseRing(target, 3f)); HintBubble(target, info.hint, 3f); } }));
                });
            });
        }
        RectTransform IntroTarget(string kind)
        {
            int t = -1;
            if (kind == "hidden") t = System.Array.FindIndex(S.stacks, st => st.Count > 1);
            else if (kind == "deep") t = System.Array.FindIndex(S.stacks, st => st.Count > 2);
            else if (kind == "tangles") t = System.Array.FindIndex(S.tg, h => h > 0);
            else if (kind == "ice") t = System.Array.FindIndex(S.ice, n => n > 0);
            else if (kind == "box") return boxLid;
            else if (kind == "belt") return beltStrip;
            else return (RectTransform)rollChip.transform.parent;
            return t >= 0 ? cells[t].rt : null;
        }

        // ---------- demos ----------
        Image DemoBall(RectTransform parent, string c, float size, Vector2 pos)
        {
            var b = UIKit.Img("Ball", parent, UIKit.Ball, UIKit.C(c)); b.rectTransform.sizeDelta = new Vector2(size, size); b.rectTransform.anchoredPosition = pos; return b;
        }
        Image DemoBadge(RectTransform parent, string n, Vector2 pos)
        {
            var b = UIKit.Img("Badge", parent, UIKit.Circle, UIKit.Ink); b.rectTransform.sizeDelta = new Vector2(83, 83); b.rectTransform.anchoredPosition = pos;
            var i = UIKit.Img("In", b.transform, UIKit.Circle, UIKit.Hex("#fffaf0")); i.rectTransform.sizeDelta = new Vector2(69, 69);
            var t = UIKit.Txt("N", b.transform, n, 44, UIKit.Ink); UIKit.Stretch(t.rectTransform); return b;
        }
        static void Alpha(Graphic g, float a) { if (g == null) return; var c = g.color; c.a = a; g.color = c; foreach (var ch in g.GetComponentsInChildren<Graphic>()) if (ch != g) { var cc = ch.color; cc.a = a; ch.color = cc; } }

        void BuildIntroDemo(string kind, RectTransform box)
        {
            if (kind.StartsWith("roll.")) { RollDemo(kind.Substring(5), box); return; }
            if (kind == "box") { BoxDemo(box); return; }
            if (kind == "belt") { BeltDemo(box); return; }
            switch (kind)
            {
                case "hidden":
                {   // 3.2 s: the top ball flies off, the hidden one rises, the badge goes
                    var tray = UIKit.Img("Spot", box, UIKit.Rounded, new Color(.63f, .5f, .35f, .16f)); tray.rectTransform.sizeDelta = new Vector2(266, 266);
                    var rise = DemoBall(box, "blue", 222, Vector2.zero); var top = DemoBall(box, "pink", 222, Vector2.zero); var badge = DemoBadge(box, "1", new Vector2(116, 116));
                    LoopAnim.Add(box.gameObject, 3.2f, p =>
                    {
                        Alpha(top, KF.At(p, .28f, 1, .42f, 0, .92f, 0, 1, 1)); top.rectTransform.anchoredPosition = new Vector2(0, KF.At(p, .28f, 0, .42f, 166, .92f, 166, 1, 0)); top.rectTransform.localScale = Vector3.one * KF.At(p, .28f, 1, .42f, .6f, .92f, .6f, 1, 1);
                        Alpha(rise, KF.At(p, .40f, 0, .56f, 1, .92f, 1, 1, 0)); rise.rectTransform.localScale = Vector3.one * KF.At(p, .40f, .5f, .56f, 1, .92f, 1, 1, .5f);
                        Alpha(badge, KF.At(p, .34f, 1, .40f, 0, .92f, 0, 1, 1));
                    });
                    break;
                }
                case "deep":
                {   // 4.4 s: two balls leave one by one, the number goes 2 → 1 → gone
                    var tray = UIKit.Img("Spot", box, UIKit.Rounded, new Color(.63f, .5f, .35f, .16f)); tray.rectTransform.sizeDelta = new Vector2(266, 266);
                    var d3 = DemoBall(box, "blue", 222, Vector2.zero); var d2 = DemoBall(box, "yellow", 222, Vector2.zero); var d1 = DemoBall(box, "teal", 222, Vector2.zero);
                    var b2 = DemoBadge(box, "2", new Vector2(116, 116)); var b1 = DemoBadge(box, "1", new Vector2(116, 116));
                    LoopAnim.Add(box.gameObject, 4.4f, p =>
                    {
                        Alpha(d1, KF.At(p, .20f, 1, .32f, 0, .92f, 0, 1, 1)); d1.rectTransform.anchoredPosition = new Vector2(0, KF.At(p, .20f, 0, .32f, 166, .92f, 166, 1, 0));
                        Alpha(d2, KF.At(p, .26f, 0, .36f, 1, .50f, 1, .62f, 0)); d2.rectTransform.anchoredPosition = new Vector2(0, KF.At(p, .50f, 0, .62f, 166, .92f, 166, 1, 0));
                        Alpha(d3, KF.At(p, .56f, 0, .66f, 1, .92f, 1, 1, 0));
                        Alpha(b2, KF.At(p, .24f, 1, .28f, 0, .99f, 0, 1, 1)); Alpha(b1, KF.At(p, .26f, 0, .32f, 1, .54f, 1, .58f, 0));
                    });
                    break;
                }
                case "tangles":
                {   // 3.6 s: a neighbour leaves → outer layer comes off; the other leaves → the knot is gone
                    var n1 = DemoBall(box, "green", 172, new Vector2(-243, 0)); var n2 = DemoBall(box, "yellow", 172, new Vector2(243, 0));
                    var l1 = UIKit.Img("Inner", box, UIKit.KnotSprite(true), Color.white); l1.rectTransform.sizeDelta = new Vector2(170, 170);
                    var l2 = UIKit.Img("Outer", box, UIKit.KnotSprite(false), Color.white); l2.rectTransform.sizeDelta = new Vector2(238, 238);
                    LoopAnim.Add(box.gameObject, 3.6f, p =>
                    {
                        Alpha(n1, KF.At(p, .18f, 1, .26f, 0, .92f, 0, 1, 1)); n1.rectTransform.localScale = Vector3.one * KF.At(p, .18f, 1, .26f, .4f, .92f, .4f, 1, 1);
                        Alpha(l2, KF.At(p, .22f, 1, .34f, 0, .92f, 0, 1, 1)); l2.rectTransform.localScale = Vector3.one * KF.At(p, .22f, 1, .34f, 1.5f, .92f, 1.5f, 1, 1); l2.rectTransform.localEulerAngles = new Vector3(0, 0, -KF.At(p, .22f, 0, .34f, 40, .92f, 40, 1, 0));
                        Alpha(n2, KF.At(p, .46f, 1, .54f, 0, .92f, 0, 1, 1)); n2.rectTransform.localScale = Vector3.one * KF.At(p, .46f, 1, .54f, .4f, .92f, .4f, 1, 1);
                        Alpha(l1, KF.At(p, .50f, 1, .62f, 0, .92f, 0, 1, 1)); l1.rectTransform.localScale = Vector3.one * KF.At(p, .50f, 1, .62f, 1.5f, .92f, 1.5f, 1, 1); l1.rectTransform.localEulerAngles = new Vector3(0, 0, KF.At(p, .50f, 0, .62f, 40, .92f, 40, 1, 0));
                    });
                    break;
                }
                case "ice":
                {   // 3.8 s: any match (pulse) counts the ice 3 → 2 → 1, then it breaks into 4 pieces
                    var g = UIKit.Rect("Frozen", box); g.anchoredPosition = new Vector2(-150, 0); g.sizeDelta = new Vector2(288, 288);
                    DemoBall(g, "blue", 233, Vector2.zero);
                    var ice = UIKit.Img("Ice", g, UIKit.Rounded, IceBlock); UIKit.Stretch(ice.rectTransform);   // opaque: the ball shows only when it breaks
                    var n = UIKit.Txt("N", ice.transform, "3", 120, UIKit.Hex("#16457a")); UIKit.Stretch(n.rectTransform);
                    var shards = new List<Image>(); Vector2[] dirs = { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
                    foreach (var d in dirs) { var s = UIKit.Img("Shard", g, UIKit.Rounded, new Color(.8f, .93f, 1f, 0)); s.rectTransform.sizeDelta = new Vector2(122, 122); s.rectTransform.anchoredPosition = d * 61; shards.Add(s); }
                    var pulse = UIKit.Rect("AnyMatch", box); pulse.anchoredPosition = new Vector2(190, 20); pulse.sizeDelta = new Vector2(200, 120);
                    var pa = DemoBall(pulse, "red", 78, new Vector2(-42, 10)); var pb = DemoBall(pulse, "red", 78, new Vector2(42, 10));
                    var lab = UIKit.Txt("Label", box, "any match", 33, CardSoft); lab.rectTransform.anchoredPosition = new Vector2(190, -60); lab.rectTransform.sizeDelta = new Vector2(240, 50);
                    LoopAnim.Add(box.gameObject, 3.8f, p =>
                    {
                        n.text = p < .23f ? "3" : p < .45f ? "2" : "1";
                        Alpha(ice, p < .64f || p > .97f ? 1 : 0); Alpha(n, p < .64f || p > .97f ? 1 : 0);
                        for (int i = 0; i < 4; i++)
                        {
                            float q = Mathf.Clamp01((p - .64f) / .2f); shards[i].color = new Color(.8f, .93f, 1f, p >= .64f && p < .84f ? .85f * (1 - q) : 0);
                            shards[i].rectTransform.anchoredPosition = dirs[i] * (61 + 120 * q); shards[i].rectTransform.localEulerAngles = new Vector3(0, 0, (i % 2 == 0 ? 1 : -1) * 70 * q);
                        }
                        float pul = KF.At(p, .08f, 0, .12f, 1, .30f, 0, .34f, 1, .52f, 0, .56f, 1, .64f, 0);
                        Alpha(pa, pul); Alpha(pb, pul);
                    });
                    break;
                }
            }
        }

        /// <summary>Needle box, 4.4 s: two red pairs are matched (pulse), the count goes 4 → 2 → 0, the lid lifts and shows 4 balls.</summary>
        void BoxDemo(RectTransform box)
        {
            var g = UIKit.Rect("Box", box); g.anchoredPosition = new Vector2(-120, 0); g.sizeDelta = new Vector2(300, 300);
            var bottom = UIKit.Img("Bottom", g, UIKit.Rounded, UIKit.Hex("#a87650")); bottom.rectTransform.sizeDelta = new Vector2(280, 250); bottom.rectTransform.anchoredPosition = new Vector2(0, -20);
            string[] inside = { "blue", "yellow", "green", "cream" }; var balls = new List<Image>();
            for (int i = 0; i < 4; i++) balls.Add(DemoBall(g, inside[i], 92, new Vector2(i % 2 == 0 ? -56 : 56, i < 2 ? 34 : -70)));
            var lid = UIKit.Img("Lid", g, UIKit.Rounded, LidRed); lid.rectTransform.sizeDelta = new Vector2(300, 280);
            var panel = UIKit.Img("Stitches", lid.transform, UIKit.Rounded, LidPanel); panel.rectTransform.sizeDelta = new Vector2(250, 220); panel.rectTransform.anchoredPosition = new Vector2(0, 6);
            var label = UIKit.Img("Label", lid.transform, UIKit.Rounded, LidLabel); Pill(label, 96); label.rectTransform.sizeDelta = new Vector2(190, 96);
            DemoBall(label.rectTransform, "red", 66, new Vector2(-44, 0));
            var n = UIKit.Txt("N", label.transform, "4", 64, UIKit.Hex("#fffaf0")); n.rectTransform.sizeDelta = new Vector2(90, 90); n.rectTransform.anchoredPosition = new Vector2(42, 0);
            var pulse = UIKit.Rect("AnyMatch", box); pulse.anchoredPosition = new Vector2(200, 20); pulse.sizeDelta = new Vector2(200, 120);
            var pa = DemoBall(pulse, "red", 78, new Vector2(-42, 10)); var pb = DemoBall(pulse, "red", 78, new Vector2(42, 10));
            var lab = UIKit.Txt("Label", box, "any match", 33, CardSoft); lab.rectTransform.anchoredPosition = new Vector2(200, -60); lab.rectTransform.sizeDelta = new Vector2(240, 50);
            LoopAnim.Add(box.gameObject, 4.4f, p =>
            {
                n.text = p < .24f ? "4" : p < .46f ? "2" : "0";
                float pul = KF.At(p, .08f, 0, .12f, 1, .24f, 0, .30f, 1, .46f, 0);
                Alpha(pa, pul); Alpha(pb, pul);
                float up = KF.At(p, .5f, 0, .64f, 1, .92f, 1, .96f, 0);
                lid.rectTransform.anchoredPosition = new Vector2(0, 230 * up); lid.rectTransform.localEulerAngles = new Vector3(0, 0, 12 * up); Alpha(lid, 1 - up);
            });
        }

        /// <summary>Conveyor belt, 3.6 s: a pair is matched elsewhere, then the row slides 1 spot to the right; the last ball wraps to the front.</summary>
        void BeltDemo(RectTransform box)
        {
            const float Step = 130, BallS = 100;
            var strip = UIKit.Img("Belt", box, UIKit.Rounded, BeltColor); strip.rectTransform.sizeDelta = new Vector2(5 * Step + 30, 150);
            strip.gameObject.AddComponent<RectMask2D>();
            string[] cols = { "yellow", "blue", "green", "red", "pink" }; var balls = new List<Image>();
            for (int i = 0; i < 5; i++) balls.Add(DemoBall(strip.rectTransform, cols[i], BallS, new Vector2((i - 2) * Step, 0)));
            var wrapIn = DemoBall(strip.rectTransform, cols[4], BallS, new Vector2(-2 * Step - Step, 0));
            var arrow = UIKit.Img("Arrow", box, UIKit.IconArrowDown, Color.white); arrow.rectTransform.sizeDelta = new Vector2(110, 110);
            arrow.rectTransform.localEulerAngles = new Vector3(0, 0, 90); arrow.rectTransform.anchoredPosition = new Vector2(0, 150);
            LoopAnim.Add(box.gameObject, 3.6f, p =>
            {
                float m = KF.At(p, .30f, 0, .52f, 1, .92f, 1, .96f, 0);
                for (int i = 0; i < 5; i++) balls[i].rectTransform.anchoredPosition = new Vector2((i - 2 + m) * Step, 0);
                wrapIn.rectTransform.anchoredPosition = new Vector2((-3 + m) * Step, 0);
                Alpha(arrow, KF.At(p, .24f, 0, .30f, 1, .52f, 1, .60f, 0));
            });
        }

        /// <summary>3×3 board: the green pair is linked and leaves, the 4 balls behind it roll one spot; the arrow shows only while they roll.</summary>
        void RollDemo(string dir, RectTransform box)
        {
            const float Cell = 122, Step = 138, BallS = 111; const string Letters = "RYGBP";
            string[] cols = { "red", "yellow", "green", "blue", "pink" };
            (string[] grid, (int x, int y)[] pair, (int x, int y)[] slide, float rot, Vector2 arrow, Vector2 move) s =
                dir == "down"  ? (new[] { "RYB", "BRY", "GGP" }, new[] { (0, 2), (1, 2) }, new[] { (0, 0), (0, 1), (1, 0), (1, 1) }, 0f,   new Vector2(-69, 69),  new Vector2(0, -Step)) :
                dir == "up"    ? (new[] { "GGP", "BRY", "RYB" }, new[] { (0, 0), (1, 0) }, new[] { (0, 1), (0, 2), (1, 1), (1, 2) }, 180f, new Vector2(-69, -70), new Vector2(0, Step)) :
                dir == "left"  ? (new[] { "GRB", "GYR", "PBY" }, new[] { (0, 0), (0, 1) }, new[] { (1, 0), (2, 0), (1, 1), (2, 1) }, -90f, new Vector2(70, 69),   new Vector2(-Step, 0)) :
                                 (new[] { "BRG", "RYG", "YBP" }, new[] { (2, 0), (2, 1) }, new[] { (0, 0), (1, 0), (0, 1), (1, 1) }, 90f,  new Vector2(-69, 69),  new Vector2(Step, 0));
            var board = UIKit.Rect("Board3x3", box); board.sizeDelta = new Vector2(3 * Step, 3 * Step);
            var cg = board.gameObject.AddComponent<CanvasGroup>();
            Vector2 Pos(int x, int y) => new Vector2((x - 1) * Step, (1 - y) * Step);
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) { var c = UIKit.Img("Cell", board, UIKit.Rounded, new Color(.63f, .5f, .35f, .16f)); c.rectTransform.sizeDelta = new Vector2(Cell, Cell); c.rectTransform.anchoredPosition = Pos(x, y); }
            var a = s.pair[0]; var b = s.pair[1];
            var link = UIKit.Img("Link", board, null, UIKit.C("green")); link.rectTransform.anchoredPosition = (Pos(a.x, a.y) + Pos(b.x, b.y)) / 2;
            link.rectTransform.sizeDelta = a.y == b.y ? new Vector2(Step, 17) : new Vector2(17, Step);
            var pairBalls = new List<Image>(); var slideBalls = new List<(Image im, Vector2 home)>();
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                {
                    var im = DemoBall(board, cols[Letters.IndexOf(s.grid[y][x])], BallS, Pos(x, y));
                    if (s.pair.Contains((x, y))) pairBalls.Add(im); else if (s.slide.Contains((x, y))) slideBalls.Add((im, Pos(x, y)));
                }
            var arrow = UIKit.Img("Arrow", board, UIKit.IconArrowDown, Color.white); arrow.rectTransform.sizeDelta = new Vector2(177, 177);
            arrow.rectTransform.localEulerAngles = new Vector3(0, 0, s.rot);
            Vector2 down = (Vector2)(Quaternion.Euler(0, 0, s.rot) * Vector2.down);
            // 4.4 s: 0–14 still | 14–32 pair linked | 32–40 pair leaves | 42–60 the line rolls (arrow) | 60–88 hold | 88–100 fade and replay
            LoopAnim.Add(box.gameObject, 4.4f, p =>
            {
                cg.alpha = KF.At(p, .88f, 1, .93f, 0, .95f, 0, 1, 1);
                Alpha(link, KF.At(p, .14f, 0, .18f, 1, .32f, 1, .36f, 0));
                float ps = KF.At(p, .32f, 1, .40f, 0, .94f, 0, .95f, 1); foreach (var pb in pairBalls) pb.rectTransform.localScale = Vector3.one * ps;
                float m = KF.At(p, .42f, 0, .60f, 1, .94f, 1, .95f, 0); foreach (var sb in slideBalls) sb.im.rectTransform.anchoredPosition = sb.home + s.move * m;
                Alpha(arrow, KF.At(p, .38f, 0, .44f, 1, .58f, 1, .66f, 0));
                arrow.rectTransform.anchoredPosition = s.arrow + down * KF.At(p, .38f, -39, .44f, -17, .58f, 28, .66f, 39);
            });
        }
    }
}
