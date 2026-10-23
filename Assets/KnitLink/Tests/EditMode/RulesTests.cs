// Rule tests: tiny boards written in the same text format as the level files.
// Run: Window > General > Test Runner > EditMode > Run All.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace KnitLink.Tests
{
    public class RulesTests
    {
        /// <summary>Rule 20/10: the same color twice in one stack can lock the last 2 balls of that color, so the validator rejects it.</summary>
        [Test]
        public void SameColorTwiceInAStackIsRejected()
        {
            var pic = string.Join(",", System.Linq.Enumerable.Repeat("\"" + new string('0', LevelJsonIO.PictureSize) + "\"", LevelJsonIO.PictureSize));
            var json = $"{{\"name\":\"t\",\"difficulty\":\"easy\",\"roll\":\"none\",\"baskets\":[{{\"color\":\"R\",\"part\":\"p\"}},{{\"color\":\"B\",\"part\":\"p\"}}],\"board\":[\"R/R R R\",\"B B B B\"],\"picture\":[{pic}]}}";
            var errors = new List<string>(); var L = LevelJsonIO.Parse(json, "t", errors); Assert.IsEmpty(errors, string.Join("\n", errors));
            var r = LevelCheck.Check(L, 6, 2000, 0);
            Assert.IsTrue(r.errors.Exists(e => e.Contains("same color twice")), "expected the same-color-stack error, got: " + string.Join(" | ", r.errors));
        }

        /// <summary>Builds a board from level-file rows. Baskets: color letters in order. Picture is filled with basket 0.</summary>
        static BoardState Board(string baskets, params string[] rows) => BoardX(baskets, "", rows);
        /// <summary>Same, with extra level-file fields, e.g. "\"box\":{...},".</summary>
        static BoardState BoardX(string baskets, string extra, params string[] rows)
        {
            var b = string.Join(",", System.Linq.Enumerable.Select(baskets.ToCharArray(), c => $"{{\"color\":\"{c}\",\"part\":\"p\"}}"));
            var pic = string.Join(",", System.Linq.Enumerable.Repeat("\"" + new string('0', LevelJsonIO.PictureSize) + "\"", LevelJsonIO.PictureSize));
            var json = $"{{\"name\":\"t\",\"difficulty\":\"easy\",\"roll\":\"none\",{extra}\"baskets\":[{b}],\"board\":[{string.Join(",", System.Linq.Enumerable.Select(rows, r => "\"" + r + "\""))}],\"picture\":[{pic}]}}";
            var errors = new List<string>();
            var L = LevelJsonIO.Parse(json, "test", errors);
            Assert.IsEmpty(errors, string.Join("\n", errors));
            return BoardState.FromLevel(L);
        }

        /// <summary>Needle box (23/10): the spots under a closed lid can't be matched; matching its color counts it down and opens it.</summary>
        [Test] public void NeedleBoxBlocksUntilItsColorIsMatched()
        {
            var s = BoardX("RB", "\"box\":{\"x\":2,\"y\":0,\"color\":\"R\",\"count\":2},", "R R B B", "_ _ B B");
            Assert.IsTrue(s.Boxed(2) && s.Boxed(7));
            Assert.AreEqual(1, s.Pairs().Count, "only the red pair outside the box can be matched");
            var ev = new List<GameEvent>(); s.Apply(0, 1, ev);
            Assert.AreEqual(0, s.box.n);
            Assert.IsTrue(ev.Exists(e => e.k == EvKind.BoxOpen));
            Assert.IsFalse(s.Boxed(2));
            Assert.Greater(s.Pairs().Count, 0, "the blue balls are free once the box is open");
        }

        /// <summary>Conveyor belt (23/10): after a valid match the belt row slides 1 spot and the last spot wraps to the front.</summary>
        [Test] public void ConveyorBeltSlidesAfterEachMatch()
        {
            var s = BoardX("RB", "\"belt\":{\"row\":1,\"dir\":\"right\"},", "R R B G", "B G Y Y");
            var ev = new List<GameEvent>(); s.Apply(0, 1, ev);
            Assert.AreEqual(new[] { "Y", "B", "G", "Y" }, new[] { 4, 5, 6, 7 }.Select(t => ColorCodes.Letter(s.Top(t).c).ToString()).ToArray());
            Assert.IsTrue(ev.Exists(e => e.k == EvKind.Belt));
        }

        [Test] public void MatchingFillsTheOpenBasket()
        {
            var s = Board("R", "R R R R");
            s.Apply(0, 1, null);
            Assert.AreEqual(2, s.q[0].fill);
        }

        [Test] public void FourBallsFinishTheBasketAndTheLevel()
        {
            var s = Board("R", "R R R R");
            s.Apply(0, 1, null); s.Apply(2, 3, null);
            Assert.IsTrue(s.Won);
        }

        [Test] public void ColorNotWantedGoesToTheShelf()
        {
            var s = Board("RB", "B B R R  R R B B");
            s.Apply(0, 1, null);
            Assert.AreEqual(2, s.shelf.Count);
        }

        [Test] public void ShelfOverflowLosesTheLevel()
        {
            var s = Board("RB", "B B R R  R R B B"); s.cap = 1;
            s.Apply(0, 1, null);
            Assert.IsTrue(s.over);
        }

        [Test] public void LineMayBendAtMostTwice()
        {
            // the two R are only joined by a 3-bend path: blocked
            var s = Board("RB", "B R B", "B B B", "R B B");
            Assert.IsFalse(s.Connected(1, 6));
        }

        [Test] public void HiddenBallShowsAfterTheTopLeaves()
        {
            var s = Board("RB", "B/R R R R", "B B B _");
            Assert.IsTrue(s.stacks[0][0].hidden);
            s.Apply(0, 1, null);
            Assert.IsFalse(s.Top(0).hidden);
            Assert.AreEqual("blue", s.Top(0).c);
        }

        [Test] public void TangleLoosensWhenNeighboursLeave()
        {
            var s = Board("R", "R X2 R", "R _ R");
            Assert.AreEqual(2, s.tg[1]);
            s.Apply(0, 2, null);   // both neighbours of the tangle leave
            Assert.AreEqual(0, s.tg[1]);
        }

        [Test] public void FrozenBallCannotBeMatchedAndThawsWithMatches()
        {
            var s = Board("RR", "R*2 R R R  R R R R");
            Assert.IsTrue(s.Frozen(0));
            foreach (var p in s.Pairs()) Assert.AreNotEqual(0, p.a, "a frozen ball must not be in a pair");
            s.Apply(1, 2, null);
            Assert.AreEqual(1, s.ice[0]);
            s.Apply(3, 4, null);
            Assert.IsFalse(s.Frozen(0));
        }

        [Test] public void FrozenBallDoesNotRoll()
        {
            var s = Board("RB", "B*3 R", "_ R", "R R", "B B");
            s.roll = "down";
            s.Apply(4, 5, null);  // the two R in row 3 match, the board rolls
            Assert.AreEqual(1, s.stacks[0].Count, "the frozen ball stays in its spot");
        }

        [Test] public void YarnBagTakesTheOldestShelfBalls()
        {
            var s = Board("RB", "B B R R  R R B B");
            s.Apply(0, 1, null);       // B B go to the shelf
            s.BagTransfer(4, null);
            Assert.AreEqual(0, s.shelf.Count);
            Assert.AreEqual(2, s.bag.Count);
        }

        [Test] public void SolverFindsAWin()
        {
            var s = Board("RB", "R B R B", "B R B R");
            Assert.IsNotNull(BoardState.Solve(s, 20000, out _));
        }
    
        [Test]
        public void VietnameseTextsTranslateAndEnglishStays()
        {
            try
            {
                Loc.Set(Loc.Vi);
                Assert.AreEqual("Cài đặt", Loc.T("Settings"));
                Assert.AreEqual("TẠM DỪNG", Loc.T("PAUSED"));                       // pills are upper-case
                Assert.AreEqual("Màn 12", Loc.T("Level 12"));
                Assert.AreEqual("KỆ  3/6", Loc.T("SHELF  3/6"));
                Assert.AreEqual("CÒN 7", Loc.T("7 LEFT"));
                Assert.AreEqual("MỚI  ·  MÀN 5", Loc.T("NEW  ·  LEVEL 5"));
                Assert.AreEqual("GIỎ  ·  TIẾP THEO", Loc.T("BASKET  ·  NEXT"));
                Assert.AreEqual("LĂN XUỐNG ↓", Loc.T("ROLLS DOWN ↓"));
                Assert.AreEqual("Trái tim hồng ngọc\nđã đan xong!", Loc.T("Ruby Heart\nis knitted!"));
                Assert.AreEqual("HẾT HOÀN TÁC", Loc.T("OUT OF UNDO"));
                Assert.AreEqual("Len đóng băng! Tan sau 2 lần nối nữa.", Loc.T("Frozen! It thaws after 2 more matches."));
                Assert.AreEqual("Màn tiếp theo   +40", Loc.T("Next level   +40"));
                Assert.AreEqual("+", Loc.T("+"));
                Assert.AreEqual("zzz unknown", Loc.T("zzz unknown"));                // missing text stays English
                Loc.Set(Loc.En);
                Assert.AreEqual("Settings", Loc.T("Settings"));
            }
            finally { Loc.Set(Loc.En); }
        }
    }
}
