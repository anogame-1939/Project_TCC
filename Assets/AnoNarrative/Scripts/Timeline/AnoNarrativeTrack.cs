using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnoGame.AnoNarrative;
using AnoGame.AnoNarrative.UI;

namespace AnoGame.AnoNarrative.Timeline
{
    [TrackClipType(typeof(AnoNarrativeClip))]
    [TrackBindingType(typeof(DialogueManager))] // Optional: Bind to Manager or UI? Manager singleton is static access though.
    // If we want to bind a specific Manager logic, but Manager is singleton.
    // Actually, maybe we don't need a binding if we just access Singleton.
    // PixelCrushers example binds to NOTHING or specific component?
    // Reference used: [TrackClipType(typeof(DialogueConversationClip))]
    public class AnoNarrativeTrack : TrackAsset
    {
        [Header("Clip Defaults")]
        public double defaultClipDuration = 5.0;

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
