using UnityEngine;
    
namespace AnoGame.Application.Player.Effects
{
    public sealed class SlowEffect : PlayerEffectBase
    {
        [SerializeField] private float slowFactor = 0.5f;
        private float originalSpeed;

        protected override void OnEffectStart()
        {
            originalSpeed = moveController.MoveSpeed;
            moveController.MoveSpeed *= slowFactor;
        }

        protected override void OnEffectEnd()
        {
            moveController.MoveSpeed = originalSpeed;
        }
    }
}