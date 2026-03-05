using UnityEngine;
using Unity.TinyCharacterController.Control;

namespace AnoGame.Application.Player.Effects
{
    public sealed class StunEffect : PlayerEffectBase
    {
        private int originalMovePriority;

        protected override void OnEffectStart()
        {
            originalMovePriority = moveController.MovePriority;
            moveController.MovePriority = 0;
        }

        protected override void OnEffectEnd()
        {
            moveController.MovePriority = originalMovePriority;
        }
    }
}