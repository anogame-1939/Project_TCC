using UnityEngine;
using AnoGame.Application.Damage;
using AnoGame.Application.Event;
using AnoGame.Domain.Event;
using UniRx;

namespace AnoGame.Application.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int _maxLives = 2; // ライフ（慙愧）の最大値
        [SerializeField] private float _invincibilityDuration = 1.0f; // 無敵時間

        // 恐怖演出などのためのイベント
        public UnityEngine.Events.UnityEvent OnLifeLost;

        private int _currentLives;
        private bool _isInvincible;
        private float _invincibilityTimer;

        // IDamageableの実装として、現在のライフを返す（HP扱い）
        public float CurrentHealth => _currentLives;
        public bool IsInvincible => _isInvincible;

        private void Start()
        {
            _currentLives = _maxLives;
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

            // 即死ダメージ（999など）の場合は一撃で0にする
            if (damage >= 999)
            {
                _currentLives = 0;
            }
            else
            {
                _currentLives = Mathf.Max(0, _currentLives - 1);
            }

            // ライフ減少時のイベント（恐怖演出など）
            OnLifeLost?.Invoke();

            // UniRxイベント発行
            MessageBroker.Default.Publish(new PlayerLifeLostEvent(_currentLives, _maxLives));

            if (_currentLives <= 0)
            {
                Debug.Log("[PlayerHealth] Player has died.", this);
                _currentLives = _maxLives;
                OnDeath();
            }
            else
            {
                Debug.Log($"[PlayerHealth] Player took damage. Current Lives: {_currentLives}/{_maxLives}", this);
                // まだライフが残っている場合は無敵時間開始
                _isInvincible = true;
                _invincibilityTimer = _invincibilityDuration;
            }
        }

        private void OnDeath()
        {
            GameStateManager.Instance.SetState(GameState.GameOver);
            GameOverManager.Instance.OnGameOver();
        }

        // 回復メソッド（ライフ回復）
        public void Heal(int amount)
        {
            // amountがfloatだが、ライフはintなのでキャストして扱う（基本1回復想定）
            if (amount <= 0) amount = 1; // 最低1は回復させる

            // ライフを回復する（最大値を超えないようにする）
            _currentLives = Mathf.Min(_maxLives, _currentLives + amount);
        }
    }
}