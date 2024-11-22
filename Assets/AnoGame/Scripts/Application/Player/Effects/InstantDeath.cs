
using UnityEngine;
using AnoGame.Application.Damage;

namespace AnoGame.Application.Player.Effects
{
    public class InstantDeath : MonoBehaviour
    {
        public void Kill()
        {
            // プレイヤーのダメージ処理用インターフェース取得
            var damageable = GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(999);
            }
        }
    }
}
