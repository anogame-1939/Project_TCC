using UnityEngine;
    
namespace AnoGame.Application.Player.Effects
{
    public sealed class SlowEffect : PlayerEffectBase
    {
        [SerializeField] private float slowFactor = 0.5f;
        private float originalSpeed;

        public override void TriggerEffect(float duration)
        {
            base.TriggerEffect(duration);
            originalSpeed = moveController.MoveSpeed;
            moveController.MoveSpeed *= slowFactor;
        }

        protected override void EndEffect()
        {
            base.EndEffect();
            moveController.MoveSpeed = originalSpeed;
        }
    }
}