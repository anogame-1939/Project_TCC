using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackClipType(typeof(DialogueConversationClip))]
    [TrackBindingType(typeof(PlayableDirector))]
    public class DialogueConversationTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<DialogueConversationBehaviour>.Create(graph, inputCount);
        }
    }
}
