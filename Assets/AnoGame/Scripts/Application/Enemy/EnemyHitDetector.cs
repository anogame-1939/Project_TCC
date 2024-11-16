using UnityEngine;
using AnoGame.Application.Damage;

namespace AnoGame.Application.Enemy
{
    public class EnemyHitDetector : MonoBehaviour
    {
        [SerializeField] private int damage = 1;

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("OnTriggerEnter" + other.gameObject);

            if (!other.CompareTag("Player")) return;
            
            // プレイヤーのダメージ処理用インターフェース取得
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }
    }
}