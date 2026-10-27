// GameController – fitting the screen on every phone / tablet shape (25/10).
// The whole game screen is designed in one 1080-wide column (reference pixels, see Lay / SkinLay). On any screen:
//   - the column is always 1080 wide and centred; the column is never shorter than MinHeight (2100).
//     Tall phones (19.5:9, 20:9, 21:9): the column fills the width, the extra height goes to the board.
//     Short / wide screens (16:9, 4:3 tablets): the column is scaled down to fit the height, the background fills the sides.
//   - the top of the column moves below the notch / camera hole (Screen.safeArea), the background still goes under it.
//   - popups, the dim, confetti and the tutorial overlay cover the whole screen (screenRT), not only the column.
//   - when the window changes size (editor Game view, foldable phones) the scale, the safe area and the board are redone.
// To test: pick other sizes in the Game view (e.g. 1080x1920, 1536x2048, 1080x2400) – nothing else to change.
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public partial class GameController
    {
        static class Fit
        {
            public const float ColumnW = 1080;     // design width (reference pixels)
            public const float MinHeight = 2100;   // below this the column is scaled down to keep the board big enough
            /// <summary>The layout already keeps this much free space above the top buttons (Lay.TopY) for a phone's status bar.
            /// On a phone with a notch / Dynamic Island only the part of the safe-area inset beyond this pushes the column down
            /// (27/10: the full inset + this space left a big empty band at the top and squeezed the board).</summary>
            public const float TopReserve = 80;
        }
        RectTransform screenRT;   // the whole screen (background, popups, dim, confetti, coach)
        /// <summary>Editor test hook (27/10): pretend the phone has this safe area (pixels, like Screen.safeArea), e.g. an iPhone
        /// notch, to check the layout without a device. null = the real Screen.safeArea. Set it while playing; the layout updates.</summary>
        public static Rect? SimSafeArea;
        static Rect SafeArea => Application.isEditor && SimSafeArea.HasValue ? SimSafeArea.Value : Screen.safeArea;
        Vector2 fitScreen; Rect fitSafe;

        /// <summary>Make the canvas, the full-screen layer and the 1080-wide column. Returns the column (= root).</summary>
        RectTransform BuildScreen(GameObject canvasGo)
        {
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            screenRT = (RectTransform)canvasGo.transform;
            var column = UIKit.Rect("Column", screenRT);
            column.anchorMin = new Vector2(.5f, 0); column.anchorMax = new Vector2(.5f, 1); column.pivot = new Vector2(.5f, .5f);
            column.sizeDelta = new Vector2(Fit.ColumnW, 0); column.anchoredPosition = Vector2.zero;
            return column;
        }

        /// <summary>Scale + safe area. Called once after BuildUI and whenever the screen size or safe area changes.</summary>
        void ApplyScreenFit()
        {
            if (screenRT == null) return;
            float w = Screen.width, h = Screen.height; fitScreen = new Vector2(w, h); fitSafe = SafeArea;
            float scale = Mathf.Min(w / Fit.ColumnW, h / Fit.MinHeight);
            canvas.GetComponent<CanvasScaler>().scaleFactor = scale;
            // notch / camera hole: push the top of the column down (the background stays full screen)
            float topInset = Mathf.Max(0, Mathf.Max(0, h - fitSafe.yMax) / scale - Fit.TopReserve);
            // home bar: lift the bottom of the column (boosters, board); the orange bar grows so it still reaches the screen bottom
            float bottomInset = Mathf.Max(SK ? SkinLay.HomeSpace : 0, fitSafe.yMin / scale);
            root.offsetMax = new Vector2(root.offsetMax.x, -topInset); root.offsetMin = new Vector2(root.offsetMin.x, bottomInset);
            if (skinBar != null) skinBar.sizeDelta = new Vector2(skinBar.sizeDelta.x, SkinLay.BarH + bottomInset);
            root.sizeDelta = new Vector2(Fit.ColumnW, root.sizeDelta.y);
            KLog.Info("Config", $"Screen {w}x{h}: UI scale {scale:0.###}, column {Fit.ColumnW}x{h / scale - topInset:0}, notch {topInset:0}");
        }

        /// <summary>Called from Update: redo the fit (and the board) when the window changes.</summary>
        void CheckScreenFit()
        {
            if (screenRT == null) return;
            if (fitScreen.x == Screen.width && fitScreen.y == Screen.height && fitSafe == SafeArea) return;
            ApplyScreenFit();
            Canvas.ForceUpdateCanvases();
            if (S != null && cells.Count > 0) { BuildBoard(); RenderAll(); }   // the board area changed: lay the spots out again
        }
    }
}
