using UnityEngine;
using UnityEngine.Playables;
using AnoGame.Application.Inventory;
using AnoGame.Data;
using App = UnityEngine.Application;

namespace AnoGame.Application.Direction.Timeline
{
    public class InventoryItemBehaviour : PlayableBehaviour
    {
        public InventoryHandler handlerOverride;
        public ItemData itemData;

        [HideInInspector] public bool hasExecuted = false;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // Mixer handles logic
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            // Mixer handles logic
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            hasExecuted = false; // Reset when graph stops or pauses? 
            // Note: OnBehaviourPause is called when graph stops. Mixer also handles resetting on weight 0.
        }
    }
}
