// All tuning numbers in one place. Edit Assets/KnitLink/Resources/GameConfig.asset in the Inspector.
// If the asset is missing, the defaults below are used (and a warning is logged).
using UnityEngine;

namespace KnitLink
{
    [CreateAssetMenu(menuName = "Knit Link/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Levels")]
        [Tooltip("Play order of the levels. Empty = every file in Resources/Levels, sorted by name.")]
        public LevelList levelList;

        [Header("Coins")]
        public int startCoins = 1000;
        public int winCoinsEasy = 20;
        public int winCoinsMedium = 20;
        public int winCoinsHard = 40;
        public int winCoinsSuperHard = 60;

        [Header("Boosters: unlock level (level number in the list)")]
        public int undoUnlockLevel = 5;
        public int hintUnlockLevel = 10;
        public int shelfUnlockLevel = 15;
        [Tooltip("Free boosters given by the unlock popup (Claim).")]
        public int boostersOnUnlock = 2;

        [Header("Boosters: price in coins (1 booster)")]
        public int undoPrice = 200;
        public int hintPrice = 150;
        public int shelfPrice = 250;

        [Header("Shelf")]
        public int shelfSlots = 6;
        [Tooltip("Slots added by the Extra Shelf booster (once per level).")]
        public int extraShelfSlots = 2;

        [Header("Revive")]
        public int maxRevives = 2;
        public int revive1Coins = 300;
        public int revive2Coins = 600;
        [Tooltip("Can the first revive be paid with a rewarded ad?")]
        public bool revive1ByAd = true;
        [Tooltip("Can the second revive be paid with a rewarded ad?")]
        public bool revive2ByAd = false;
        [Tooltip("Balls moved from the shelf into the yarn bag per revive.")]
        public int reviveBagSize = 4;

        [Header("Win popup")]
        [Tooltip("\"Claim ×3\": watching an ad multiplies the win coins by this.")]
        public int winClaimMultiplier = 3;

        [Header("Ads (demo: the ad is simulated)")]
        [Tooltip("Seconds the fake rewarded ad plays.")]
        public float adSeconds = 1.6f;

        [Header("Solver")]
        [Tooltip("Search budget for Hint and level checks. Higher = slower but finds harder solutions.")]
        public int solverBudget = 20000;

        public int WinCoins(string difficulty) =>
            difficulty == "superhard" ? winCoinsSuperHard : difficulty == "hard" ? winCoinsHard : difficulty == "medium" ? winCoinsMedium : winCoinsEasy;

        static GameConfig current;
        public static GameConfig Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<GameConfig>("GameConfig");
                if (current == null)
                {
                    KLog.Warn("Config", "Resources/GameConfig.asset not found - using default values. Menu: Knit Link > Create Config Assets.");
                    current = CreateInstance<GameConfig>();
                }
                return current;
            }
        }
        /// <summary>Forget the cached asset (Editor tools call this after changes).</summary>
        public static void Reload() { current = null; }
    }
}
