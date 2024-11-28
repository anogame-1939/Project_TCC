using UnityEngine;
using AnoGame.Application.Damage;
using AnoGame.Application.Story;

namespace AnoGame.Application.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private float _invincibilityDuration = 1.0f; // 無敵時間

        private float _currentHealth;
        private bool _isInvincible;
        private float _invincibilityTimer;

        public float CurrentHealth => _currentHealth;
        public bool IsInvincible => _isInvincible;

        private void Start()
        {
            _currentHealth = _maxHealth;
        }

        private void Update()
        {
            // 無敵時間の処理
            if (_isInvincible)
            {
                _invincibilityTimer -= Time.deltaTime;
                if (_invincibilityTimer <= 0)
                {
                    _isInvincible = false;
                }
            }
        }

        public void TakeDamage(int damage)
        {
            // 無敵中はダメージを受けない
            if (_isInvincible) return;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            
            // 無敵時間の開始
            _isInvincible = true;
            _invincibilityTimer = _invincibilityDuration;

            // HPが0になった時の処理
            if (_currentHealth <= 0)
            {
                OnDeath();
            }
        }

        private void OnDeath()
        {
            // 死亡時の処理を実装
            // 例：ゲームオーバー画面表示、リスポーン処理など
            // リトライポイントへ
            // StoryManager.Instance.RetyrCurrentScene();
            PlayerSpawnManager.Instance.SpawnPlayerAtRetryPoint();
            // NOTE:エネミーもリトライポイントに移動させるのはいいが、ここでいいのか。。
            EnemySpawnManager.Instance.SpawnEnemyAtRetryPoint();
        }

        // HP回復メソッド
        public void Heal(float amount)
        {
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        }
    }
}