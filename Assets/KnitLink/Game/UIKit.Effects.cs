// UIKit – soft drop shadow and smooth text outline (25/10), drawn as extra copies of the graphic's own mesh, so they follow
// every move, scale, fade and sprite change of the graphic with no extra objects.
//   SoftShadow:   like the web demo's "drop-shadow(x y blur color)": a dark copy pushed down, blurred by 8 faint copies on a ring.
//   RoundOutline: outline made of 16 copies on a circle (+8 on a half-size circle) – round and even, unlike Unity's Outline
//                 (4 diagonal copies), which looks jagged at 4–5 px.
// Offsets / sizes are in the graphic's own units (reference pixels of the 1080 × 2340 canvas for most UI).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KnitLink
{
    public abstract class MeshCopies : BaseMeshEffect
    {
        public Color color = new Color(0, 0, 0, .35f);
        static readonly List<UIVertex> src = new List<UIVertex>(), dst = new List<UIVertex>();
        /// <summary>Offsets of the copies and the alpha of each copy (0..1, times color.a and the graphic's own alpha).</summary>
        protected abstract IEnumerable<(Vector2 off, float a)> Copies();
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            src.Clear(); vh.GetUIVertexStream(src); dst.Clear();
            foreach (var (off, a) in Copies())
                foreach (var v0 in src)
                {
                    var v = v0; v.position += (Vector3)off;
                    var c = color; c.a *= a * v0.color.a / 255f; v.color = c;
                    dst.Add(v);
                }
            dst.AddRange(src);   // the graphic itself on top
            vh.Clear(); vh.AddUIVertexTriangleStream(dst);
        }
        public void Refresh() { if (graphic != null) graphic.SetVerticesDirty(); }
    }

    /// <summary>Soft drop shadow under an image or text (web demo: drop-shadow(0 3px 2px rgba(...))).</summary>
    public class SoftShadow : MeshCopies
    {
        public Vector2 offset = new Vector2(0, -8); public float blur = 5;
        protected override IEnumerable<(Vector2, float)> Copies()
        {
            yield return (offset, .45f);
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; yield return (offset + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * blur, .12f); }
        }
    }

    /// <summary>Round, even outline around text.</summary>
    public class RoundOutline : MeshCopies
    {
        public float width = 4;
        protected override IEnumerable<(Vector2, float)> Copies()
        {
            for (int i = 0; i < 16; i++) { float a = i * Mathf.PI / 8; yield return (new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * width, 1); }
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4 + .39f; yield return (new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * width * .55f, 1); }
        }
    }

    public static partial class UIKit
    {
        /// <summary>Move a stretched text up (and right) inside its box, to sit a number in the middle of a badge's face
        /// (the badge art and the fonts' digits are not centred the same way). Sets the offsets, so calling it again does not add up.</summary>
        public static void Lift(Text t, float up, float right = 0)
        {
            var rt = t.rectTransform; rt.offsetMin = new Vector2(right, up); rt.offsetMax = new Vector2(right, up);
        }
        /// <summary>Add a soft drop shadow (offset down by dy, blur radius blur, in the graphic's units).</summary>
        public static SoftShadow DropShadow(Graphic g, float dy, float blur, Color col)
        {
            var s = g.GetComponent<SoftShadow>(); if (s == null) s = g.gameObject.AddComponent<SoftShadow>();
            s.offset = new Vector2(0, -dy); s.blur = blur; s.color = col; s.Refresh(); return s;
        }
    }
}
