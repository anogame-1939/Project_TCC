using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Application.Gameplay
{

    [DisallowMultipleComponent]
    public class TrapSpawner : MonoBehaviour
    {
        [Header("Trap Prefabs (候補)")]
        [Tooltip("生成候補となるトラップのプレハブを複数登録します。")]
        [SerializeField] private GameObject[] trapPrefabs;

        [Header("Pool 設定 (Start時にランダム個数を作成)")]
        [Tooltip("Start時に作るプール数の最小値。min <= max の範囲でランダムな個数が選ばれます。")]
        [SerializeField] private int minPoolSize = 3;

        [Tooltip("Start時に作るプール数の最大値。")]
        [SerializeField] private int maxPoolSize = 6;

        [Tooltip("実際に確定したプール数（デバッグ確認用・実行時のみ表示）。")]
        [SerializeField, ReadOnly] private int decidedPoolSize;

        [Header("ターゲット追跡")]
        [Tooltip("追跡対象。未指定なら 'Player' タグを自動で探します。")]
        [SerializeField] private Transform target;

        [Header("スポーン設定")]
        [Tooltip("スポーン間隔（秒）。")]
        [SerializeField] private float spawnInterval = 2.5f;

        [Tooltip("ターゲット周辺に配置する半径。")]
        [SerializeField] private float spawnRadius = 10f;

        [Tooltip("NavMesh.SamplePosition の最大サーチ距離。")]
        [SerializeField] private float navMeshMaxDistance = 2.0f;

        [Tooltip("NavMesh.SamplePosition に失敗した場合の再試行回数。")]
        [SerializeField] private int sampleAttempts = 8;

        [Tooltip("使用する NavMesh エリア (通常は AllAreas)")]
        [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

        [Header("挙動")]
        [Tooltip("常に最新1個のみ表示（過去は非表示）します。true 固定推奨。")]
        [SerializeField] private bool onlyLatestActive = true;

        [Tooltip("生成物をこのオブジェクトの子にします。false ならワールド直下。")]
        [SerializeField] private bool parentUnderThis = true;

        // 内部
        private readonly List<GameObject> pool = new List<GameObject>();
        private int nextIndex = 0;
        private Coroutine spawnRoutine;
        private float timeAcc;

        // Player 自動再取得用（ターゲットが消えた場合に備える）
        private float reacquireTimer;
        private const float ReacquireInterval = 1.0f;

        private void OnValidate()
        {
            if (maxPoolSize < 0) maxPoolSize = 0;
            if (minPoolSize < 0) minPoolSize = 0;
            if (minPoolSize > maxPoolSize) minPoolSize = maxPoolSize;
            if (spawnInterval < 0.05f) spawnInterval = 0.05f;
            if (spawnRadius < 0f) spawnRadius = 0f;
            if (navMeshMaxDistance < 0.1f) navMeshMaxDistance = 0.1f;
            if (sampleAttempts < 1) sampleAttempts = 1;
        }

        private void Start()
        {
            // 1) 追跡対象の確定
            if (target == null) TryFindPlayer();

            // 2) ランダムなプール数を決定し、プレハブから作成（以後は再生成せず再利用）
            decidedPoolSize = Random.Range(minPoolSize, maxPoolSize + 1);
            BuildPool(decidedPoolSize);

            // 3) 全て非表示で開始
            SetAllActive(false);

            // 4) スポーンループ開始
            spawnRoutine = StartCoroutine(SpawnLoop());
        }

        private void Update()
        {
            // ターゲットが消えた場合は定期的に再取得
            reacquireTimer += Time.deltaTime;
            if ((target == null || !target.gameObject.activeInHierarchy) && reacquireTimer >= ReacquireInterval)
            {
                reacquireTimer = 0f;
                TryFindPlayer();
            }
        }

        private void OnDisable()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }
            // 止めるだけ。プールは残す（再有効化で再利用）
        }

        private void OnDestroy()
        {
            // プール破棄（必要に応じて）
            foreach (var go in pool)
            {
                if (go != null)
                {
                    if (UnityEngine.Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }
            pool.Clear();
        }

        private void TryFindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        private void BuildPool(int count)
        {
            if (trapPrefabs == null || trapPrefabs.Length == 0)
            {
                Debug.LogWarning($"{nameof(TrapSpawner)}: trapPrefabs が未設定です。");
                return;
            }

            for (int i = pool.Count; i < count; i++)
            {
                var prefab = trapPrefabs[Random.Range(0, trapPrefabs.Length)];
                if (prefab == null)
                {
                    Debug.LogWarning($"{nameof(TrapSpawner)}: trapPrefabs に null が含まれています。スキップします。");
                    continue;
                }

                var go = Instantiate(prefab, parentUnderThis ? transform : null);
                go.SetActive(false);
                pool.Add(go);
            }
        }

        private IEnumerator SpawnLoop()
        {
            timeAcc = 0f;

            // 最初に即時スポーンしたい場合はここで一度呼ぶ
            yield return null;
            SpawnOnce();

            // 以後、一定間隔でスポーン
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);
                SpawnOnce();
            }
        }

        private void SpawnOnce()
        {
            if (pool.Count == 0) return;

            // 配置位置を決める
            Vector3 center = (target != null) ? target.position : transform.position;

            if (!TryGetRandomNavMeshPoint(center, spawnRadius, out var hitPos))
            {
                // NavMesh 上に見つからない場合はスキップ（次のタイミングに再トライ）
                return;
            }

            // 直前までの表示をオフ（「最新のみ表示」の要件）
            if (onlyLatestActive) SetAllActive(false);

            // 次のオブジェクトを取得
            var go = pool[nextIndex];
            if (go == null)
            {
                // 何らかの理由で破棄されていたら補充
                var prefab = trapPrefabs[Random.Range(0, trapPrefabs.Length)];
                go = Instantiate(prefab, parentUnderThis ? transform : null);
                pool[nextIndex] = go;
            }

            // 配置＆有効化
            go.transform.position = hitPos;
            go.transform.rotation = Quaternion.identity; // 必要なら向き制御
            go.SetActive(true);

            // 次のインデックスへ（ラウンドロビン）
            nextIndex = (nextIndex + 1) % pool.Count;
        }

        private bool TryGetRandomNavMeshPoint(Vector3 center, float radius, out Vector3 result)
        {
            for (int i = 0; i < sampleAttempts; i++)
            {
                // XZ 平面でランダム
                Vector2 inside = Random.insideUnitCircle * radius;
                var candidate = new Vector3(center.x + inside.x, center.y + 1.0f, center.z + inside.y);

                if (NavMesh.SamplePosition(candidate, out var hit, navMeshMaxDistance, navMeshAreaMask))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = Vector3.zero;
            return false;
        }

        private void SetAllActive(bool active)
        {
            foreach (var go in pool)
            {
                if (go != null && go.activeSelf != active)
                    go.SetActive(active);
            }
        }

        // デバッグ用にギズモで半径を表示
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.identity;
            Vector3 center = (target != null) ? target.position : transform.position;
            Gizmos.color = new Color(0.3f, 0.8f, 0.9f, 0.35f);
            Gizmos.DrawWireSphere(center, spawnRadius);
        }

        // インスペクタで ReadOnly 表示するための属性
        public class ReadOnlyAttribute : PropertyAttribute { }
    }



#if UNITY_EDITOR
    // ReadOnly 属性の簡易 Drawer
    [CustomPropertyDrawer(typeof(TrapSpawner.ReadOnlyAttribute), true)]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}