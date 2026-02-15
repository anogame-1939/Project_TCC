using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.AnoDialogue.Editor
{
    /// <summary>
    /// Custom listener to create ManhattanEdge instead of default Edge when connecting ports.
    /// </summary>
    public class DialogueEdgeConnectorListener : IEdgeConnectorListener
    {
        public void OnDropOutsidePort(Edge edge, Vector2 position) { }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            var output = edge.output;
            var input = edge.input;

            if (output != null && input != null)
            {
                var newEdge = new ManhattanEdge();
                newEdge.output = output;
                newEdge.input = input;

                newEdge.output.Connect(newEdge);
                newEdge.input.Connect(newEdge);

                graphView.AddElement(newEdge);

                // Ensure it renders on top if layer order allows, or we handled layer order in GraphView
                newEdge.BringToFront();
            }
        }
    }
}
