using UnityEngine;
using AnoGame.Application.Damage;

namespace AnoGame.Application.Enemy
{
    public class EnemyHitDetector : MonoBehaviour
    {
        [SerializeField] bool _isActive = true;
        [SerializeField] private int damage = 1;
        
        // イベント追加
        public event System.Action OnPlayerHit;

        private void Start()
        {
            Deactivate();
        }

        public void Activate()
        {
            _isActive = true;
            Physics.IgnoreLayerCollision( LayerMask.NameToLayer("Player"),
                                          LayerMask.NameToLayer("Enemy"),
                                          false);
        }

        public void Deactivate()
        {
            _isActive = false;
            Physics.IgnoreLayerCollision( LayerMask.NameToLayer("Player"),
                                          LayerMask.NameToLayer("Enemy"),
                                          true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;
            Debug.Log($"enable:{enabled} - hit");
            if (!other.CompareTag("Player")) return;

            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Debug.Log("Player");
                damageable.TakeDamage(damage);
                // OnPlayerHit?.Invoke();  // これ自体はちゃんと使わてない
            }
        }


    }
}