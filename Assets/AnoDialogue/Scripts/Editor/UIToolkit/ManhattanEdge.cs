using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.AnoDialogue.Editor
{
    /// <summary>
    /// Custom Edge with Manhattan (right-angle) routing.
    /// Two patterns based on vertical relationship:
    ///   - ChannelRoute: target is above → horizontal → vertical → horizontal (S-shape)
    ///   - StepRoute:    target is below → horizontal → vertical (L-shape)
    /// </summary>
    public class ManhattanEdge : Edge
    {
        public ManhattanEdge() : base()
        {
            AddToClassList("manhattan-edge");
        }

        public override bool UpdateEdgeControl()
        {
            if (input == null || output == null) return false;

            bool result = base.UpdateEdgeControl();

            if (edgeControl == null) return result;

            edgeControl.generateVisualContent = OnDraw;

            return result;
        }

        private void OnDraw(MeshGenerationContext mgc)
        {
            Vector2 from = edgeControl.from;
            Vector2 to = edgeControl.to;

            if (from == Vector2.zero && to == Vector2.zero) return;

            // Convert from Edge space to EdgeControl local space
            Vector2 offset = new Vector2(edgeControl.layout.x, edgeControl.layout.y);
            Vector2 localFrom = from - offset;
            Vector2 localTo = to - offset;

            var painter = mgc.painter2D;
            painter.strokeColor = selected
                ? new Color(0.27f, 0.58f, 0.89f, 1f)
                : new Color(0.77f, 0.77f, 0.77f, 0.8f);
            painter.lineWidth = 2f;
            painter.lineCap = LineCap.Round;

            // to.y < from.y → target is above (Y increases downward)
            if (localTo.y < localFrom.y)
                DrawChannelRoute(painter, localFrom, localTo);
            else
                DrawStepRoute(painter, localFrom, localTo);
        }

        /// <summary>
        /// Target is above source.
        /// 4-point S-shape: from → (midX, from.y) → (midX, to.y) → to
        /// Horizontal → Vertical → Horizontal
        /// </summary>
        private void DrawChannelRoute(Painter2D painter, Vector2 from, Vector2 to)
        {
            float midX = (from.x + to.x) * 0.5f;
            Vector2 mid1 = new Vector2(midX, from.y);
            Vector2 mid2 = new Vector2(midX, to.y);

            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(mid1);
            painter.LineTo(mid2);
            painter.LineTo(to);
            painter.Stroke();
        }

        /// <summary>
        /// Target is below source.
        /// 3-point L-shape: from → (to.x, from.y) → to
        /// Horizontal → Vertical
        /// </summary>
        private void DrawStepRoute(Painter2D painter, Vector2 from, Vector2 to)
        {
            Vector2 corner = new Vector2(to.x, from.y);

            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(corner);
            painter.LineTo(to);
            painter.Stroke();
        }
    }
}
