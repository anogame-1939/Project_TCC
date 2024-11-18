using UnityEngine;
using Unity.TinyCharacterController.Control;

namespace AnoGame.Application.Player.Effects
{
    public sealed class StunEffect : PlayerEffectBase
    {
        private int originalMovePriority;

        public override void TriggerEffect(float duration)
        {
            base.TriggerEffect(duration);
            
            // 現在の優先度を保存
            originalMovePriority = moveController.MovePriority;
            
            // 移動を停止
            moveController.MovePriority = 0;
        }

        protected override void EndEffect()
        {
            base.EndEffect();
            
            // 元の優先度を復元
            moveController.MovePriority = originalMovePriority;
        }
    }
}