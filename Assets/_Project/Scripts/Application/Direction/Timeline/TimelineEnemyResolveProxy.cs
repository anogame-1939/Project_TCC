using UnityEngine;
using AnoGame.Application.Enemy; // EnemySpawnManager, SpriteResolveController
using VContainer;

namespace AnoGame.Application.Direction.Timeline
{
    /// <summary>
    /// タイムラインからEnemyのリゾルブ処理を制御するためのプレキシ。
    /// EnemySpawnManagerから現在の敵を取得し、SpriteResolveControllerへアクセスを提供する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimelineEnemyResolveProxy : MonoBehaviour
    {
        [Inject] private EnemySpawnManager _spawnManager;

        private SpriteResolveController _cachedController;
        private GameObject _lastEnemyInstance;

        public SpriteResolveController ResolveController
        {
            get
            {
                if (_spawnManager == null)
                {
                    _spawnManager = EnemySpawnManager.Instance;
                }

                var currentEnemy = _spawnManager.CurrentEnemyInstance;
                if (currentEnemy == null) return null;

                // インスタンスが変わっている場合はキャッシュをクリア
                if (!ReferenceEquals(currentEnemy, _lastEnemyInstance))
                {
                    _lastEnemyInstance = currentEnemy;
                    _cachedController = currentEnemy.GetComponent<SpriteResolveController>();
                }

                return _cachedController;
            }
        }

        // タイムラインから簡易的に呼べるメソッド
        public void SetResolve(float amount) => ResolveController?.SetResolve(amount);
        public void Activate() => ResolveController?.Activate();
        public void Deactivate() => ResolveController?.Deactivate();
    }
}
