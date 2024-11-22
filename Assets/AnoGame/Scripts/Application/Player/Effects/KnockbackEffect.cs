using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    public class KnockbackEffect : PlayerEffectBase
    {
        [SerializeField] private float knockbackForce = 10f;
        
        private Vector3 knockbackDirection;
        private float originalMoveSpeed;
        private int originalMovePriority;

        public void ApplyKnockback(Vector3 direction, float duration)
        {
            Debug.Log("ApplyKnockback");
            knockbackDirection = direction.normalized;
            TriggerEffect(duration);
        }

        protected override void OnEffectStart()
        {
            Debug.Log("OnEffectStart");
            // Store original values
            originalMovePriority = moveController.MovePriority;
            originalMoveSpeed = moveController.MoveSpeed;

            // Override movement control
            moveController.MovePriority = 999;
            moveController.MoveSpeed = knockbackForce;
            moveController.Velocity = knockbackDirection * knockbackForce;
            moveController.enabled = false;

            stateManager.SetInputEnabled(false);
        }

        protected override void OnEffectEnd()
        {
            Debug.Log("OnEffectEnd");
            // Restore original values
            moveController.MovePriority = originalMovePriority;
            moveController.MoveSpeed = originalMoveSpeed;
            moveController.Velocity = Vector3.zero;
            moveController.enabled = true;
            stateManager.SetInputEnabled(true);
        }

        protected override void Update()
        {
            base.Update();

            if (!isActive) return;

            // ノックバック効果の減衰（より急激な初期ノックバックと自然な減衰）
            float normalizedTime = timer / timer;
            float forceMultiplier = Mathf.Pow(normalizedTime, 2f); // 二次関数的な減衰
            moveController.Velocity = knockbackDirection * (knockbackForce * forceMultiplier);
        }
    }
}