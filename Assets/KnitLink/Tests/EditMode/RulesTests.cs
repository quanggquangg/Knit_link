// Rule tests: tiny boards written in the same text format as the level files.
// Run: Window > General > Test Runner > EditMode > Run All.
using System.Collections.Generic;
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
        static BoardState Board(string baskets, params string[] rows)
        {
            var b = string.Join(",", System.Linq.Enumerable.Select(baskets.ToCharArray(), c => $"{{\"color\":\"{c}\",\"part\":\"p\"}}"));
            var pic = string.Join(",", System.Linq.Enumerable.Repeat("\"" + new string('0', LevelJsonIO.PictureSize) + "\"", LevelJsonIO.PictureSize));
            var json = $"{{\"name\":\"t\",\"difficulty\":\"easy\",\"roll\":\"none\",\"baskets\":[{b}],\"board\":[{string.Join(",", System.Linq.Enumerable.Select(rows, r => "\"" + r + "\""))}],\"picture\":[{pic}]}}";
            var errors = new List<string>();
            var L = LevelJsonIO.Parse(json, "test", errors);
            Assert.IsEmpty(errors, string.Join("\n", errors));
            return BoardState.FromLevel(L);
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
    }
}
