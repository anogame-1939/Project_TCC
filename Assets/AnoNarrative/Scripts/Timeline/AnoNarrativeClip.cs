using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnoGame.AnoNarrative;

namespace AnoGame.AnoNarrative.Timeline
{
    public class AnoNarrativeClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("Conversation Settings")]
        public ExposedReference<DialogueController> targetController;
        public string conversationID;
        public bool pauseTimeline = true;

        // ITimelineClipAsset implementation
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<AnoNarrativeBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.targetController = targetController.Resolve(graph.GetResolver());
            behaviour.conversationID = conversationID;
            behaviour.pauseTimeline = pauseTimeline;

            // Resolve director to allow pausing
            var director = go.GetComponent<PlayableDirector>();
            behaviour.director = director;

            return playable;
        }
    }
}
