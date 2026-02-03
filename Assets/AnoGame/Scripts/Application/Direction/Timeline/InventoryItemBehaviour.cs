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

        private InventoryHandler _trackBoundHandler;
        private bool _fired;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playerData is InventoryHandler h)
            {
                _trackBoundHandler = h;
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_fired || !App.isPlaying) return;

            var handler = handlerOverride != null ? handlerOverride : _trackBoundHandler;
            if (handler != null && itemData != null)
            {
                handler.AddItem(itemData);
                _fired = true;
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _fired = false;
        }
    }
}
