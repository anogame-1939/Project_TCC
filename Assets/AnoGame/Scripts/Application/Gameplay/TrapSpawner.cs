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

        [Header("同時スポーン（バースト）")]
        [Tooltip("1回のスポーンで同時に出す最小個数")]
        [SerializeField] private int minSpawnPerBurst = 2;

        [Tooltip("1回のスポーンで同時に出す最大個数（min <= max）")]
        [SerializeField] private int maxSpawnPerBurst = 4;

        [Tooltip("同一フレームに湧かせる位置同士の最小距離。0 で無効。")]
        [SerializeField] private float minDistanceBetweenSpawns = 1.5f;

        [Tooltip("onlyLatestActive=false のとき、非表示(未使用)個体を優先して使う")]
        [SerializeField] private bool preferInactive = true;


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

            if (minSpawnPerBurst < 1) minSpawnPerBurst = 1;
            if (maxSpawnPerBurst < 1) maxSpawnPerBurst = 1;
            if (minSpawnPerBurst > maxSpawnPerBurst) minSpawnPerBurst = maxSpawnPerBurst;
            if (minDistanceBetweenSpawns < 0f) minDistanceBetweenSpawns = 0f;
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

            // 配置の基準点
            Vector3 center = (target != null) ? target.position : transform.position;

            // 「最新のみ表示」なら先に全消灯
            if (onlyLatestActive) SetAllActive(false);

            // このフレームで出す個数をランダム決定
            int want = Random.Range(minSpawnPerBurst, maxSpawnPerBurst + 1);

            // 候補インデックスを作成
            // ・onlyLatestActive=true → 全部候補
            // ・false かつ preferInactive=true → 非表示のものだけ
            List<int> candidates = new List<int>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                if (onlyLatestActive) { candidates.Add(i); }
                else
                {
                    if (!preferInactive || !pool[i].activeSelf)
                        candidates.Add(i);
                }
            }
            if (candidates.Count == 0) return;

            // 実際に出せる個数 = 候補数まで
            int countToSpawn = Mathf.Min(want, candidates.Count);

            // 位置をまとめてサンプリング（近接しすぎ防止オプション付き）
            var positions = GetSpawnPositions(center, countToSpawn);
            if (positions.Count == 0) return;

            // 候補をシャッフルしてランダムな個体を選ぶ
            Shuffle(candidates);

            int count = Mathf.Min(countToSpawn, positions.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = candidates[i];
                var go = pool[idx];
                if (go == null)
                {
                    var prefab = trapPrefabs[Random.Range(0, trapPrefabs.Length)];
                    go = Instantiate(prefab, parentUnderThis ? transform : null);
                    pool[idx] = go;
                }

                go.transform.position = positions[i];
                go.transform.rotation = Quaternion.identity;

                var trap = go.GetComponent<AnoGame.Application.Animation.Gmmicks.TrapController>();
                if (trap != null)
                {
                    trap.AppearAt(positions[i]);  // フェードインで出現（子の有効化＆フェード）
                }
                else
                {
                    // TrapController なしの保険
                    go.SetActive(true);
                }
            }
        }

        private List<Vector3> GetSpawnPositions(Vector3 center, int count)
        {
            List<Vector3> list = new List<Vector3>(count);
            int guard = Mathf.Max(16, sampleAttempts * count * 3); // 過剰ループ防止

            int tries = 0;
            float minSqr = minDistanceBetweenSpawns * minDistanceBetweenSpawns;

            while (list.Count < count && tries++ < guard)
            {
                if (!TryGetRandomNavMeshPoint(center, spawnRadius, out var p)) continue;

                if (minDistanceBetweenSpawns > 0f)
                {
                    bool farEnough = true;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if ((list[i] - p).sqrMagnitude < minSqr)
                        {
                            farEnough = false;
                            break;
                        }
                    }
                    if (!farEnough) continue;
                }

                list.Add(p);
            }

            return list; // NavMeshや距離条件で不足する場合、少ない個数で返ることがあります
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
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
                if (go == null) continue;

                var trap = go.GetComponent<Animation.Gmmicks.TrapController>();

                if (active)
                {
                    // 今回のスポーンで個別に AppearAt するので、ここで true にしない
                    // （全ONが必要なケースだけ下の1行を使う）
                    // if (trap == null && !go.activeSelf) go.SetActive(true);
                    continue;
                }
                else
                {
                    // OFF にする時はフェードアウトを依頼
                    if (trap != null)
                    {
                        // 罠アクション中は残したいならスキップ
                        if (trap.IsBusy) continue;
                        trap.Disappear();              // フェードアウト→非表示
                    }
                    else
                    {
                        if (go.activeSelf) go.SetActive(false);
                    }
                }
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