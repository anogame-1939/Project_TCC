using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.AnoDialogue.Editor
{
    /// <summary>
    /// Step 1: Simple straight line from output port to input port.
    /// from/to are in Edge's coordinate space, not EdgeControl's local space.
    /// Must subtract EdgeControl.layout.position to convert.
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

            // Intermediate points
            float midX = (localFrom.x + localTo.x) * 0.5f;
            Vector2 mid1 = new Vector2(midX, localFrom.y);
            Vector2 mid2 = new Vector2(midX, localTo.y);

            painter.BeginPath();
            painter.MoveTo(localFrom);
            painter.LineTo(mid1);
            painter.LineTo(mid2);
            painter.LineTo(localTo);
            painter.Stroke();
        }
    }
}
