using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnoGame.AnoDialogue;

namespace AnoGame.AnoDialogue.Timeline
{
    public class AnoNarrativeClip : PlayableAsset, ITimelineClipAsset
    {
        [HideInInspector] public int targetEpisode = -1;
        [HideInInspector] public int targetChapter = -1;
        [HideInInspector] public int targetSection = -1;

        public string conversationID;
        public string dialogueStyleName;
        public AnoGame.AnoDialogue.Data.DialogueStyle styleDatabase; // Reference to DB for dropdown
        public bool pauseTimeline = true;

        // ITimelineClipAsset implementation
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<AnoNarrativeBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.conversationID = conversationID;
            behaviour.dialogueStyleName = dialogueStyleName;
            behaviour.pauseTimeline = pauseTimeline;

            // Resolve director to allow pausing
            var director = go.GetComponent<PlayableDirector>();
            behaviour.director = director;

            return playable;
        }
    }
}

