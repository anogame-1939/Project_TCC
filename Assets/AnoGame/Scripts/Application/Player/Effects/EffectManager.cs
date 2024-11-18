using System.Collections.Generic;
using UnityEngine;
using AnoGame.Application.Enemy;

namespace AnoGame.Application.Player.Effects
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
        [SerializeField] private EffectSettings settings;
        
        [Header("Effects")]
        [SerializeField] private VisionReductionEffect visionEffect;
        [SerializeField] private StunEffect stunEffect;
        [SerializeField] private SlowEffect slowEffect;
        [SerializeField] private EnemyAwareness enemyAwareness;
        [SerializeField] private PlayerTeleporter teleporter;
        [SerializeField] private KnockbackEffect knockback;
        [SerializeField] private InstantDeath instantDeath;

        // 即時効果のみを追跡
        private readonly HashSet<CollisionType> _activeInstantEffects = new();

        public void ApplyEffect(CollisionType effectType, Vector3? direction = null)
        {
            switch (effectType)
            {
                // デバフ効果：重複可能で時間延長
                case CollisionType.VisionReduction:
                    visionEffect.TriggerEffect(settings.visionReductionDuration);
                    break;
                    
                case CollisionType.Stun:
                    stunEffect.TriggerEffect(settings.stunDuration);
                    break;
                    
                case CollisionType.Slow:
                    slowEffect.TriggerEffect(settings.slowDuration);
                    break;
                    
                // 即時効果：重複不可
                case CollisionType.EnemyAware:
                    if (!_activeInstantEffects.Contains(effectType))
                    {
                        _activeInstantEffects.Add(effectType);
                        enemyAwareness.SpawnNearPlayer();
                        _activeInstantEffects.Remove(effectType);
                    }
                    break;
                    
                case CollisionType.Teleport:
                    if (!_activeInstantEffects.Contains(effectType))
                    {
                        _activeInstantEffects.Add(effectType);
                        teleporter.TeleportToRandom();
                        _activeInstantEffects.Remove(effectType);
                    }
                    break;
                    
                case CollisionType.Knockback:
                    if (!_activeInstantEffects.Contains(effectType))
                    {
                        _activeInstantEffects.Add(effectType);
                        knockback.ApplyKnockback(direction ?? - transform.forward, settings.knockbackDuration);
                        _activeInstantEffects.Remove(effectType);
                    }
                    break;
                    
                case CollisionType.InstantDeath:
                    if (!_activeInstantEffects.Contains(effectType))
                    {
                        _activeInstantEffects.Add(effectType);
                        instantDeath.Kill();
                        _activeInstantEffects.Remove(effectType);
                    }
                    break;
            }
        }

        // 即時効果のアクティブ状態のみをチェック
        public bool IsEffectActive(CollisionType effectType)
        {
            return _activeInstantEffects.Contains(effectType);
        }
    }
}
