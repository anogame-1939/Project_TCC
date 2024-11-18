// Assets/AnoGame/Scripts/Application/Player/Manager/EffectManager.cs
using UnityEngine;
using AnoGame.Application.Enemy;
using AnoGame.Application.Player.Effects;

namespace AnoGame.Application.Player.Manager
{
    public enum CollisionType
    {
        VisionReduction,
        Stun,
        Slow,
        EnemyAware,
        Teleport,
        Knockback,
        InstantDeath
    }

    public class EffectManager : MonoBehaviour
    {
        [SerializeField] private VisionReductionEffect visionEffect;
        [SerializeField] private StunEffect stunEffect;
        [SerializeField] private SlowEffect slowEffect;
        [SerializeField] private EnemyAwareness enemyAwareness;
        [SerializeField] private PlayerTeleporter teleporter;
        [SerializeField] private KnockbackEffect knockback;
        [SerializeField] private InstantDeath instantDeath;

        public void OnPlayerHit(CollisionType collisionType)
        {
            switch (collisionType)
            {
                case CollisionType.VisionReduction:
                    visionEffect.TriggerEffect(3f);
                    break;
                case CollisionType.Stun:
                    stunEffect.TriggerEffect(2f);
                    break;
                case CollisionType.Slow:
                    slowEffect.TriggerEffect(5f);
                    break;
                case CollisionType.EnemyAware:
                    enemyAwareness.SpawnNearPlayer();
                    break;
                case CollisionType.Teleport:
                    teleporter.TeleportToRandom();
                    break;
                case CollisionType.Knockback:
                    knockback.ApplyKnockback(transform.forward);
                    break;
                case CollisionType.InstantDeath:
                    instantDeath.Kill();
                    break;
            }
        }
    }
}
