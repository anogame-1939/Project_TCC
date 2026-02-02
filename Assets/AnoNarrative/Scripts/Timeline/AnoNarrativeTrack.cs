using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnoGame.AnoNarrative;
using AnoGame.AnoNarrative.UI;

namespace AnoGame.AnoNarrative.Timeline
{
    [TrackClipType(typeof(AnoNarrativeClip))]
    [TrackBindingType(typeof(DialogueTimelineReceiver))]
    // Reference used: [TrackClipType(typeof(DialogueConversationClip))]
    public class AnoNarrativeTrack : TrackAsset
    {
        [Header("Clip Defaults")]
        public double defaultClipDuration = 0.5;

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<AnoNarrativeMixer>.Create(graph, inputCount);
        }

        protected override void OnCreateClip(TimelineClip clip)
        {
            base.OnCreateClip(clip);
            clip.duration = defaultClipDuration;
            clip.displayName = "Conversation Clip";
        }
    }
}
