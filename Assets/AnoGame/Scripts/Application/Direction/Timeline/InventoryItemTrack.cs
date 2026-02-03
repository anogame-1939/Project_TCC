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

    public class InventoryItemMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            InventoryHandler trackBinding = playerData as InventoryHandler;

            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                var inputPlayable = (ScriptPlayable<InventoryItemBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();

                if (weight > 0)
                {
                    if (!behaviour.hasExecuted)
                    {
                        // Overrideがあればそちら、なければTrack Bindingを使う
                        var handler = behaviour.handlerOverride != null ? behaviour.handlerOverride : trackBinding;

                        if (handler != null && behaviour.itemData != null)
                        {
                            if (UnityEngine.Application.isPlaying)
                            {
                                handler.AddItem(behaviour.itemData);
                            }
                        }
                        else
                        {
                            // Only log warning if playing to avoid editor spam
                            if (UnityEngine.Application.isPlaying)
                            {
                                if (handler == null) Debug.LogWarning($"[InventoryItemMixer] Handler is null for clip {i}. Check Track Binding or Override.");
                                if (behaviour.itemData == null) Debug.LogWarning($"[InventoryItemMixer] ItemData is null for clip {i}.");
                            }
                        }
                        behaviour.hasExecuted = true;
                    }
                }
                else
                {
                    behaviour.hasExecuted = false;
                }
            }
        }
    }
}
