using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.AnoDialogue.Editor
{
    /// <summary>
    /// Custom Edge that draws orthogonal (right-angle / Manhattan) lines.
    /// Uses control points to force the bezier curve into a step shape.
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

            base.UpdateEdgeControl();

            if (edgeControl == null) return true;

            Vector2 from = edgeControl.from;
            Vector2 to = edgeControl.to;

            float minDrop = 20f;
            float minRise = 20f;

            // Instead of a single midY, we use two control points to ensure
            // the line always exits downwards and enters upwards.
            // P0 (Start) -> P1 (Start + down) ... P2 (End + up) -> P3 (End)

            var c1 = new Vector2(from.x, from.y + minDrop);
            var c2 = new Vector2(to.x, to.y - minRise);

            var points = new Vector2[]
            {
                from,
                c1,
                c2,
                to
            };

            // Use reflection to set read-only property 'controlPoints'
            // controlPoints is a property with private/protected setter likely
            var prop = typeof(EdgeControl).GetProperty("controlPoints", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(edgeControl, points);
            }
            else
            {
                // Fallback: try backing field if property is not writable
                var field = typeof(EdgeControl).GetField("m_ControlPoints", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(edgeControl, points);
                }
            }

            return true;
        }
    }
}
