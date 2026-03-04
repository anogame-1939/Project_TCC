using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnoGame.Application.Inventory;
using AnoGame.Data;

namespace AnoGame.Application.Direction.Timeline
{
    public class InventoryItemClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("Settings")]
        public ItemData itemData;

        [Header("Override (Optional)")]
        public ExposedReference<InventoryHandler> handlerOverride;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<InventoryItemBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.handlerOverride = handlerOverride.Resolve(graph.GetResolver());
            behaviour.itemData = itemData;

            return playable;
        }
    }
}
