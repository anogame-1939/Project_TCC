using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Reflection;

namespace AnoGame.AnoDialogue.Editor
{
    /// <summary>
    /// Custom Edge that draws near-Manhattan (right-angle) routing.
    ///
    /// Unity's EdgeControl uses exactly 4 cubic bezier control points:
    ///   [from, handle1, handle2, to]
    /// where from/to are endpoints (pass-through) and handle1/handle2 are
    /// tangent handles. True Manhattan routing requires polylines, but EdgeControl
    /// only supports a single cubic bezier segment.
    ///
    /// Strategy: Set handle tangent directions to force the bezier into a
    /// near-rectangular S-shape:
    ///   - handle1 extends vertically downward from 'from' (vertical exit)
    ///   - handle2 extends vertically upward from 'to' (vertical entry)
    /// With long enough handles, the curve becomes very close to a right-angle path.
    /// </summary>
    public class ManhattanEdge : Edge
    {
        private static readonly BindingFlags kFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static PropertyInfo _controlPointsProp;
        private static FieldInfo _controlPointsField;
        private static bool _reflectionCached;

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

            // Skip if positions not yet resolved
            if (from == Vector2.zero && to == Vector2.zero) return true;

            // Compute Manhattan-style control points
            var points = ComputeManhattanHandles(from, to);
            SetControlPoints(points);

            return true;
        }

        /// <summary>
        /// Compute 4         bezier control points that approximate Manhattan routing.
        ///
        /// Default ports exit to the RIGHT (+X) and enter from the LEFT (-X).
        /// For Manhattan, we override this so:
        ///   - output exits DOWNWARD (+Y)
        ///   - input enters from ABOVE (-Y)
        ///
        /// The handle length controls how "sharp" the corners appear.
        /// Using the full vertical distance as handle length creates very tight corners.
        /// </summary>
        private Vector2[] ComputeManhattanHandles(Vector2 from, Vector2 to)
        {
            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float absDy = Mathf.Abs(dy);

            // Handle length: larger = sharper corners
            // Use a significant portion of the distance to make it as rectangular as possible
            float handleLen;

            if (dy > 30f)
            {
                // Normal flow (output above input): handles go vertically
                // Longer handles = more rectangular
                handleLen = absDy * 0.5f;
                handleLen = Mathf.Max(handleLen, 50f);

                return new Vector2[]
                {
                    from,
                    new Vector2(from.x, from.y + handleLen),   // exit downward
                    new Vector2(to.x,   to.y   - handleLen),   // enter from above
                    to
                };
            }
            else if (dy < -30f)
            {
                // Reverse flow (output below input): handles go in opposite vertical direction
                handleLen = absDy * 0.5f;
                handleLen = Mathf.Max(handleLen, 50f);

                return new Vector2[]
                {
                    from,
                    new Vector2(from.x, from.y - handleLen),   // exit upward
                    new Vector2(to.x,   to.y   + handleLen),   // enter from below
                    to
                };
            }
            else
            {
                // Similar Y level: horizontal connection with a horizontal bump
                float absDx = Mathf.Abs(dx);
                handleLen = Mathf.Max(absDx * 0.4f, 40f);
                float bumpY = 80f;

                if (dx > 0)
                {
                    // to is to the right: bump downward
                    return new Vector2[]
                    {
                        from,
                        new Vector2(from.x, from.y + bumpY),
                        new Vector2(to.x,   to.y   + bumpY),
                        to
                    };
                }
                else
                {
                    // to is to the left: bump upward
                    return new Vector2[]
                    {
                        from,
                        new Vector2(from.x, from.y - bumpY),
                        new Vector2(to.x,   to.y   - bumpY),
                        to
                    };
                }
            }
        }

        private void SetControlPoints(Vector2[] points)
        {
            CacheReflection();

            if (_controlPointsProp != null && _controlPointsProp.CanWrite)
                _controlPointsProp.SetValue(edgeControl, points);
            else if (_controlPointsField != null)
                _controlPointsField.SetValue(edgeControl, points);
        }

        private static void CacheReflection()
        {
            if (_reflectionCached) return;
            _reflectionCached = true;
            var t = typeof(EdgeControl);
            _controlPointsProp = t.GetProperty("controlPoints", kFlags);
            _controlPointsField = t.GetField("m_ControlPoints", kFlags);
        }
    }
}
