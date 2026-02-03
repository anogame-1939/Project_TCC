using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnoGame.Application.Inventory;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackColor(0.2f, 0.6f, 1.0f)]
    [TrackBindingType(typeof(InventoryHandler))]
    [TrackClipType(typeof(InventoryItemClip))]
    public class InventoryItemTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<InventoryItemMixer>.Create(graph, inputCount);
        }

        protected override void OnCreateClip(TimelineClip clip)
        {
            base.OnCreateClip(clip);
            clip.duration = 0.5f;
            clip.displayName = "Item";
        }
    }

    public class InventoryItemMixer : PlayableBehaviour { }
}
