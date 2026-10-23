// Every level file in Resources/Levels must load without errors and be solvable.
// One test per file, so the Test Runner shows exactly which level is broken.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace KnitLink.Tests
{
    public class LevelFilesTests
    {
        static IEnumerable<string> Files() => Resources.LoadAll<TextAsset>("Levels").Select(t => t.name).OrderBy(n => n);

        [Test] public void ThereAreLevels() => Assert.Greater(Resources.LoadAll<TextAsset>("Levels").Length, 0);

        [TestCaseSource(nameof(Files))]
        public void LevelLoadsAndIsSolvable(string file)
        {
            var ta = Resources.Load<TextAsset>("Levels/" + file);
            var errors = new List<string>();
            var L = LevelJsonIO.Parse(ta.text, file, errors);
            Assert.IsEmpty(errors, string.Join("\n", errors));
            // big levels (50+ balls) need a large budget for the simple DFS solver (23/10)
            var r = LevelCheck.Check(L, GameConfig.Current.shelfSlots, 600000, 0);
            Assert.IsEmpty(r.errors, string.Join("\n", r.errors));
            Assert.IsTrue(r.solvable, r.solverGaveUp ? "solver gave up (budget too small?)" : "the level cannot be won");
        }

        [TestCaseSource(nameof(Files))]
        public void LevelSurvivesSaveAndLoad(string file)
        {
            // writing the level back to JSON and reading it again must give the same board
            var ta = Resources.Load<TextAsset>("Levels/" + file);
            var a = LevelJsonIO.Parse(ta.text, file, new List<string>());
            var b = LevelJsonIO.Parse(LevelJsonIO.ToJson(a), file, new List<string>());
            CollectionAssert.AreEqual(a.shape, b.shape);
            CollectionAssert.AreEqual(a.baskets, b.baskets);
            CollectionAssert.AreEqual(a.picture, b.picture);
            for (int t = 0; t < a.board.stacks.Length; t++) Assert.AreEqual(LevelJsonIO.SpotToken(a, t), LevelJsonIO.SpotToken(b, t));
            Assert.AreEqual(a.box == null, b.box == null, "needle box");
            if (a.box != null) { CollectionAssert.AreEqual(a.box.spots, b.box.spots); Assert.AreEqual(a.box.color, b.box.color); Assert.AreEqual(a.box.count, b.box.count); }
            Assert.AreEqual(a.belt == null, b.belt == null, "conveyor belt");
            if (a.belt != null) { Assert.AreEqual(a.belt.row, b.belt.row); Assert.AreEqual(a.belt.dir, b.belt.dir); }
        }
    }
}
