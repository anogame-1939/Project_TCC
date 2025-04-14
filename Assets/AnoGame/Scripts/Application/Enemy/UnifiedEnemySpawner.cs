using UnityEngine;
using System.Collections;
using AnoGame.Data;

namespace AnoGame.Application.Enemy
{
    /// <summary>
    /// 敵生成のモードを管理する列挙型
    /// </summary>
    public enum SpawnerMode
    {
        Story,   // ストーリー用：追跡や自動消滅などを抑制
        Random   // ランダム用：プレイヤー追跡や時間経過による消滅など、通常の挙動を実施
    }

    /// <summary>
    /// ノーマル（ストーリー用）とランダム用の処理を統合したシンプルな敵生成クラス
    /// </summary>
    public class UnifiedEnemySpawner : MonoBehaviour
    {
        [Header("共通設定")]
        [SerializeField]
        private GameObject enemyPrefab;

        [SerializeField]
        private SpawnerMode spawnMode = SpawnerMode.Story;

        [SerializeField]
        private bool isPermanent = false;

        [SerializeField]
        private EventData eventData;

        [Header("ランダムモード設定")]
        [SerializeField, Min(1f)]
        private float minSpawnTime = 15f;

        [SerializeField, Min(10f)]
        private float maxSpawnTime = 30f;

        // 共通で利用する EnemySpawnManager（シングルトン）
        private EnemySpawnManager spawnManager;

        // 現在実行中の生成処理のコルーチン
        private IEnumerator spawnCoroutine;

        private void Awake()
        {
            spawnManager = EnemySpawnManager.Instance;
            if (spawnManager == null)
            {
                Debug.LogError("EnemySpawnManagerが見つかりません。");
            }

            // enemyPrefabを設定（各モード共通のプレハブ）
            if (enemyPrefab != null)
            {
                spawnManager.SetEnemyPrefab(enemyPrefab);
            }
        }

        /// <summary>
        /// 外部から呼び出す敵生成開始メソッド
        /// </summary>
        public void TriggerEnemySpawn()
        {
            // もし既にコルーチンが動いていれば停止
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }

            // 選択モードに応じて処理を切り替え
            if (spawnMode == SpawnerMode.Story)
            {
                spawnCoroutine = StorySpawnCoroutine();
            }
            else if (spawnMode == SpawnerMode.Random)
            {
                spawnCoroutine = RandomSpawnCoroutine();
            }

            StartCoroutine(spawnCoroutine);
        }

        /// <summary>
        /// ストーリーモード用の生成コルーチン
        /// 敵はスポーンするが、追跡や自動消滅機能は開始しない
        /// </summary>
        private IEnumerator StorySpawnCoroutine()
        {
            // 必要に応じ、初期化や状態リセットをここで実施可能

            yield return null;

            // isPermanentや eventData を渡して開始位置に敵を生成
            spawnManager.SetEventData(eventData);
            spawnManager.SpawnEnemyAtStart(isPermanent);
            yield return null;

            // Storyモードでは、敵の追跡や自動消滅処理は実施しないので、追加の処理は不要
        }

        /// <summary>
        /// ランダムモード用の生成コルーチン
        /// トリガーされたら、ランダムな間隔で継続的に敵を生成し、通常の追跡・タイムアウト消滅処理を実行
        /// </summary>
        private IEnumerator RandomSpawnCoroutine()
        {
            // 終了条件がある場合はそれに合わせてループ条件を変更してください。
            while (true)
            {
                // ランダムな待機時間を設定
                float waitTime = Random.Range(minSpawnTime, maxSpawnTime);

                Debug.Log("RandomSpawnCoroutine - wait" + waitTime);
                yield return new WaitForSeconds(waitTime);
                Debug.Log("RandomSpawnCoroutine - start" + waitTime);

                // プレイヤーをタグから検索し、近くに敵をスポーン
                GameObject player = GameObject.FindWithTag(SLFBRules.TAG_PLAYER);
                if (player != null)
                {
                    spawnManager.SpawnEnemyNearPlayer(player.transform.position);
                }
                else
                {
                    Debug.LogError("プレイヤーオブジェクトが見つかりません。");
                }

                // ランダムモードでは、敵の追跡やタイムアウト消滅など、通常の機能を有効にする
                // 脳の有効化や移動開始処理を実施
                spawnManager.EnabaleEnamy();
                spawnManager.StartEnemyMovement();

                // 敵が追跡状態にある間、待機（IsChasing() が false になれば次のループへ）
                while (spawnManager.IsChasing())
                {
                    yield return new WaitForSeconds(1f);
                }

                // 状態が変わった場合に移動停止などの後処理を実施
                spawnManager.StopEnemyMovement();
            }
        }
    }
}
