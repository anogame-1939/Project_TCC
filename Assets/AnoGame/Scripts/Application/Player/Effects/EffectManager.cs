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

        // アクティブな効果を追跡
        private readonly HashSet<CollisionType> _activeEffects = new();

        // 効果の適用
        public void ApplyEffect(CollisionType effectType)
        {
            if (_activeEffects.Contains(effectType))
                return; // 既に効果が適用中の場合はスキップ

            _activeEffects.Add(effectType);

            switch (effectType)
            {
                case CollisionType.VisionReduction:
                    visionEffect.TriggerEffect(settings.visionReductionDuration);
                    break;
                    
                case CollisionType.Stun:
                    stunEffect.TriggerEffect(settings.stunDuration);
                    break;
                    
                case CollisionType.Slow:
                    slowEffect.TriggerEffect(settings.slowDuration);
                    break;
                    
                case CollisionType.EnemyAware:
                    enemyAwareness.SpawnNearPlayer();
                    _activeEffects.Remove(effectType); // 即時効果なので直ぐに削除
                    break;
                    
                case CollisionType.Teleport:
                    teleporter.TeleportToRandom();
                    _activeEffects.Remove(effectType); // 即時効果なので直ぐに削除
                    break;
                    
                case CollisionType.Knockback:
                    knockback.ApplyKnockback(transform.forward);
                    _activeEffects.Remove(effectType); // 即時効果なので直ぐに削除
                    break;
                    
                case CollisionType.InstantDeath:
                    instantDeath.Kill();
                    _activeEffects.Remove(effectType); // 即時効果なので直ぐに削除
                    break;
            }
        }

        // 効果の終了通知を受け取るメソッド
        public void NotifyEffectEnd(CollisionType effectType)
        {
            _activeEffects.Remove(effectType);
        }

        // 特定の効果が適用中かチェック
        public bool IsEffectActive(CollisionType effectType)
        {
            return _activeEffects.Contains(effectType);
        }
    }
}
