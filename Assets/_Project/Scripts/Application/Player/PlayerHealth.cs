using UnityEngine;
using AnoGame.Application.Damage;
using AnoGame.AnoFlow;
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
            // 初期化時に現在のデータを反映
            if (GameManager.Instance.CurrentGameData != null)
            {
                SetHealth(GameManager.Instance.CurrentGameData.CurrentHealth);
            }
            else
            {
                SetHealth(_maxLives);
            }

            // データロードイベントを購読して、後からデータがロード/リセットされた場合にも対応
            GameManager.Instance.LoadGameData += OnGameDataLoaded;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadGameData -= OnGameDataLoaded;
            }
        }

        private void OnGameDataLoaded(AnoGame.Domain.Data.Models.GameData gameData)
        {
            if (gameData != null)
            {
                SetHealth(gameData.CurrentHealth);
            }
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

            // Update Game Data
            GameManager.Instance.UpdateCurrentHealth(_currentLives);
            GameManager.Instance.SaveData();

            // ライフ減少時のイベント（恐怖演出など）
            OnLifeLost?.Invoke();

            if (_currentLives <= 0)
            {
                Debug.Log("[PlayerHealth] Player has died.", this);
                int priorLives = _currentLives;
                _currentLives = _maxLives;

                // Publish Death Event
                MessageBroker.Default.Publish(new PlayerDeathEvent(priorLives, _maxLives));

                // Reset Health in GameData on Death (so next load starts with full health)
                GameManager.Instance.UpdateCurrentHealth(_maxLives);
                GameManager.Instance.SaveData();

                // OnDeath() logic removal - logic moved to event listeners
                // GameStateManager.Instance.SetState(GameState.GameOver); // Moved to listener or managed by GameOverManager
                // GameOverManager.Instance.OnGameOver(); // Decoupled
            }
            else
            {
                Debug.Log($"[PlayerHealth] Player took damage. Current Lives: {_currentLives}/{_maxLives}", this);

                // Publish Miss Event
                MessageBroker.Default.Publish(new PlayerMissEvent(_currentLives, _maxLives));

                // まだライフが残っている場合は無敵時間開始
                _isInvincible = true;
                _invincibilityTimer = _invincibilityDuration;
            }
        }

        // Removed OnDeath method as it is no longer needed in this form
        // private void OnDeath() { ... }

        // 回復メソッド（ライフ回復）
        public void Heal(int amount)
        {
            // amountがfloatだが、ライフはintなのでキャストして扱う（基本1回復想定）
            if (amount <= 0) amount = 1; // 最低1は回復させる

            // ライフを回復する（最大値を超えないようにする）
            _currentLives = Mathf.Min(_maxLives, _currentLives + amount);
        }

        // 外部からライフをセットする
        public void SetHealth(int amount)
        {
            _currentLives = Mathf.Clamp(amount, 0, _maxLives);
            // Debug.Log($"[PlayerHealth] Health set to {_currentLives}");
        }
    }
}