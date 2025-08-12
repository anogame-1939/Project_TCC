using UnityEngine;
using AnoGame.Application.Damage;
using System.Collections;

namespace AnoGame.Application.Enemy
{
    public class EnemyHitDetector : MonoBehaviour
    {
        [SerializeField] bool _isActive = true;
        [SerializeField] private int damage = 1;

        // イベント追加

        private void Awake()
        {
            Deactivate();
        }
        
        public void Activate()
        {
            _isActive = true;
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"),
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
            if (!other.CompareTag("Player")) return;

            if (GameStateManager.Instance.CurrentState == GameState.GameOver) return;
            if (GameStateManager.Instance.CurrentState == GameState.InGameEvent) return;
            

            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                // OnPlayerHit?.Invoke();  // これ自体はちゃんと使わてない
            }
        }


    }
}