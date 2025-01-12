using UnityEngine;
using AnoGame.Application.Damage;

namespace AnoGame.Application.Enemy
{
    public class EnemyHitDetector : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        
        // イベント追加
        public event System.Action OnPlayerHit;

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("hit");
            if (!other.CompareTag("Player")) return;
            
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Debug.Log("Player");
                damageable.TakeDamage(damage);
                OnPlayerHit?.Invoke();  // イベント発火
            }
        }
    }
}