// Knit Link – the in-game screen (Build 1: in-game only, no Home/Shop/lives).
// GameController is ONE component split into files by feature (all share the fields below):
//   GameController.cs            fields, Start/Update, test hooks
//   GameController.UI.cs         builds the screen (top bar, picture, baskets, shelf, board panel, boosters)
//   GameController.Level.cs      start a level, lay out the board
//   GameController.Render.cs     draw the current state (spots, shelf, bag, boosters, baskets, picture)
//   GameController.Input.cs      taps, matching, no-path feedback
//   GameController.Animation.cs  flights, knitting, rolling, effects, toast
//   GameController.Flow.cs       auto-finish, stuck check, win -> next level, restart, progress
//   GameController.Boosters.cs   Undo / Hint / Extra Shelf, buying with coins or an ad
//   GameController.Revive.cs     shelf full / no moves -> revive (yarn bag, open the way)
//   GameController.Tutorial.cs   tutorials, booster unlock, coach overlay, hint bubbles
//   GameController.Popup.cs      the popup card kit (pill, title, big icon, coin/ad choice, progress, buttons)
//   GameController.Popups.cs     every popup: settings, booster offer, retry, booster intro, revive, lose, win
//   GameController.ItemIntro.cs  animated "new item" popups (hidden yarn, tangles, ice, rolling…)
//   GameController.Bag.cs        yarn bag chip next to SHELF and the "balls fly into the bag" animation
//   GameController.Intro.cs      level start animation (UI comes in, yarn drops onto the board)
// Rules live in Core/YarnCore.cs (no UI). Tuning numbers live in Resources/GameConfig.asset.
// Console tags: [Level] [Move] [Booster] [Revive] [Win] [Tutorial] [Ads] [Config].
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController : MonoBehaviour
    {
        const float RefW = 1080, RefH = 2340, GAP = .1f, CELL_H = 1.02f;
        const int PIC = LevelJsonIO.PictureSize;   // 24×24 stitches

        // ---------- state ----------
        LevelFile file; LevelDef L; int levelIndex;
        BoardState S;
        float[] prog; int sel = -1, token, pending, moves, peak;
        bool autoRun, winShown, tipShelfPending;
        /// <summary>Dev automation (AutoPlayer) skips tutorials and popups.</summary>
        [HideInInspector] public bool devSkipTutorials;
        // a full basket stays in the row until its last ball lands, then leaves to knit; the next basket takes over at once
        readonly List<string> busyBaskets = new List<string>();
        readonly HashSet<string> leavingBaskets = new HashSet<string>();
        // undo history: board, picture progress and move count before each move
        readonly Stack<(BoardState s, float[] prog, int moves, int peak)> history = new Stack<(BoardState s, float[] prog, int moves, int peak)>();

        // ---------- boosters ----------
        static readonly string[] BKeys = { "undo", "hint", "shelf" };
        // unlock level and price come from GameConfig (Resources/GameConfig.asset)
        static GameConfig Cfg => GameConfig.Current;
        static Dictionary<string, int> Unlock => new Dictionary<string, int> { { "undo", Cfg.undoUnlockLevel }, { "hint", Cfg.hintUnlockLevel }, { "shelf", Cfg.shelfUnlockLevel } };
        static readonly Dictionary<string, string> BName = new Dictionary<string, string> { { "undo", "Undo" }, { "hint", "Hint" }, { "shelf", "Extra Shelf" } };
        // short in-game texts (design doc "item launch schedule + short texts", 19/10)
        static readonly Dictionary<string, string> BDesc = new Dictionary<string, string> {
            { "undo", "Take back your last move." },
            { "hint", "Show a pair you can link." },
            { "shelf", "Add 2 more shelf slots." } };
        readonly Dictionary<string, Button> bButtons = new Dictionary<string, Button>();
        readonly Dictionary<string, Text> bLabels = new Dictionary<string, Text>(), bCounts = new Dictionary<string, Text>();
        readonly Dictionary<string, Image> bBadges = new Dictionary<string, Image>();
        static int Inv(string k) => PlayerPrefs.GetInt("kl.inv." + k, 0);
        static void SetInv(string k, int v) { PlayerPrefs.SetInt("kl.inv." + k, Mathf.Max(0, v)); }
        static bool Claimed(string k) => PlayerPrefs.GetInt("kl.claimed." + k, 0) == 1;
        static bool Seen(string k) => PlayerPrefs.GetInt("kl.seen." + k, 0) == 1;
        static void MarkSeen(string k) => PlayerPrefs.SetInt("kl.seen." + k, 1);
        bool Locked(string k) => L != null && L.num < Unlock[k];

        // ---------- coach (forced tutorial step) ----------
        class Coach { public List<int> trays; public string booster; public bool nopath; public string text; public System.Action done; }
        Coach coach; RectTransform coachRT, handRT, bubbleRT; Text bubbleText; Vector3 handTarget;

        // ---------- economy (values from GameConfig) ----------
        static int StartCoins => Cfg.startCoins;
        static Dictionary<string, int> BPrice => new Dictionary<string, int> { { "undo", Cfg.undoPrice }, { "hint", Cfg.hintPrice }, { "shelf", Cfg.shelfPrice } };
        static int Coins { get => PlayerPrefs.GetInt("kl.coins", StartCoins); set => PlayerPrefs.SetInt("kl.coins", Mathf.Max(0, value)); }
        static string Today => System.DateTime.Now.ToString("yyyyMMdd");
        // Build 1: rewarded ads are unlimited (no shop), so there is no daily cap.
        int revives; (int a, int b) lastMove = (-1, -1);
        // coin HUD: one fixed-size element next to Settings, drawn above the game AND every popup (hudLayer)
        RectTransform hudLayer, coinHud; Text coinText;
        // yarn bag chip at the right end of the SHELF row (hidden until the first revive); one ball per color + count
        RectTransform bagRT, bagGroupsRT; int bagHold = -1;   // bagHold >= 0: only the first bagHold balls are drawn (balls still flying in)
        readonly List<GameObject> lifted = new List<GameObject>();

        // ---------- views ----------
        Canvas canvas; RectTransform root, fx, boardPanel, board, basketRow, shelfRow, shelfHead, toastRT, modalRT, adLayer;
        Text title, basketsLeft, shelfCount, knittedText, toastText, rollChip;
        RectTransform topRow, picFrame, basketsHead; Button btnSettings, btnRetry;
        float introUntil;   // board taps wait until the level start animation has dropped every ball
        RawImage picImg; Texture2D picTex;
        readonly List<CellView> cells = new List<CellView>();
        readonly Dictionary<string, BasketView> basketViews = new Dictionary<string, BasketView>();
        readonly List<string> basketOrder = new List<string>();
        readonly List<Image> slots = new List<Image>(); readonly List<Image> slotLocks = new List<Image>();
        float cs = 100;
        Coroutine toastCo;

        class CellView
        {
            public RectTransform rt; public Image hit, shadow, back, front, knot, selRing, ice; public Text backCount, iceCount;
            public int shownTop = -1, shownHp, shownCount, shownIce;
        }
        class BasketView
        {
            public RectTransform rt; public Image body, rim; public Text count; public GameObject label; public CanvasGroup cg;
            public Image[] fill = new Image[4];   // yarn shown inside the basket, one ball per yarn that landed
            public int shown; public float targetX, targetScale = 1, targetAlpha = 1; public string color; public bool preview;
        }

        // Save progress, coins and boosters when the app goes to the background (27/10). iOS (and Android) often kill a
        // background app without OnApplicationQuit, and Unity only writes PlayerPrefs to disk on quit or Save().
        void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
        void OnApplicationFocus(bool focus) { if (!focus) PlayerPrefs.Save(); }

        // ======================================================================
        IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            file = LevelData.Load();
            ApplySettings();
            BuildUI();
            yield return null;   // let the canvas scaler size the layout before measuring the board
            if (file.levels.Length == 0) { KLog.Error("Level", "No level could be loaded - check the Console and the LevelList."); yield break; }
            // Build 1: in-game only. Open the app straight into the level in progress.
            int start = PlayerPrefs.GetInt("kl.level", 0);
            // "Play this level" from the Level Editor
            if (Application.isEditor && PlayerPrefs.HasKey(DevStartLevelKey)) { start = PlayerPrefs.GetInt(DevStartLevelKey); PlayerPrefs.DeleteKey(DevStartLevelKey); }
            StartLevel(Mathf.Clamp(start, 0, file.levels.Length - 1));
        }

        void Update()
        {
            CheckScreenFit();   // window / safe area changed: rescale and lay the board out again (GameController.ScreenFit.cs)
            // baskets glide to their slots and grow / shrink between "taking yarn" and "preview"
            float k = 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime);
            foreach (var kv in basketViews)
            {
                if (leavingBaskets.Contains(kv.Key)) continue;
                var v = kv.Value; var p = v.rt.anchoredPosition;
                if (Mathf.Abs(p.x - v.targetX) > .5f) v.rt.anchoredPosition = new Vector2(Mathf.Lerp(p.x, v.targetX, k), p.y);
                float sc = v.rt.localScale.x; if (Mathf.Abs(sc - v.targetScale) > .002f) v.rt.localScale = Vector3.one * Mathf.Lerp(sc, v.targetScale, k);
                if (Mathf.Abs(v.cg.alpha - v.targetAlpha) > .002f) v.cg.alpha = Mathf.Lerp(v.cg.alpha, v.targetAlpha, k);
            }
            // the tutorial hand taps its target
            if (handRT != null && handRT.gameObject.activeInHierarchy)
            {
                float ph = Mathf.Repeat(Time.unscaledTime / 1.05f, 1f), press = ph < .45f ? ph / .45f : ph < .6f ? 1 : 1 - (ph - .6f) / .4f;
                handRT.position = handTarget + new Vector3(18, -26, 0) * canvas.scaleFactor * (1 - press);
                handRT.localScale = Vector3.one * Mathf.Lerp(1f, .9f, press);
            }
        }

        // ---------- test hooks (used by editor automation) ----------
        public BoardState State => S;
        public void TapSpot(int t) => OnTap(t);
        public void GoLevel(int li) => StartLevel(li);
        /// <summary>PlayerPrefs key the Level Editor sets to start a given level index when Play is pressed.</summary>
        public const string DevStartLevelKey = "kl.devStartLevel";
    }
}
