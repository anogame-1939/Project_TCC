using UnityEngine;
using AnoGame.Application.Damage;

namespace AnoGame.Application.Enemy
{
    public class EnemyHitDetector : MonoBehaviour
    {
        [SerializeField] bool on = true;
        [SerializeField] private int damage = 1;
        
        // イベント追加
        public event System.Action OnPlayerHit;

        private void OnTriggerEnter(Collider other)
        {
            if (!on) return;
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

        public void SetEnabled(bool enabled)
        {
            on = enabled;
        }
    }
}