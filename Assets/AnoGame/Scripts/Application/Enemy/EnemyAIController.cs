using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using AnoGame.Application.Player.Control;
using Unity.TinyCharacterController.Control;
using Unity.TinyCharacterController.Core; // NOTE:微妙...別のnamespaceがいい
using Unity.TinyCharacterController.Brain;
using AnoGame.Application.Enemy;
using Cysharp.Threading.Tasks;
using System;

namespace AnoGame.Application.Enmemy.Control
{
    public class EnemyAIController : MonoBehaviour, IForcedMoveController
    {
        [SerializeField] private MoveNavmeshControl moveControl;
        // プレイヤーのタグ
        [SerializeField] private string playerTag = "Player";

        // 移動アニメーションを切り替える速度のしきい値
        [SerializeField] private float speedThreshold = 0.1f;

        // 子オブジェクトにあるアニメーターを取得する場合のインデックス
        [SerializeField] private int animatorChildIndex = 0;

        // アニメーター内で設定している Bool パラメータ名
        [SerializeField] private string animatorBoolParam = "IsMove";

        [SerializeField] private bool isChasing = false;
        public bool IsChasing => isChasing;

        [SerializeField] private bool isStoryMode = false;
        public bool IsStoryMode => isStoryMode;



        private NavMeshAgent agent;
        private Animator animator;
        private GameObject player;

        void Start()
        {
            if (moveControl == null)
            {
                moveControl = GetComponent<MoveNavmeshControl>();
            }

            // NavMeshAgent の取得
            agent = GetComponentInChildren<NavMeshAgent>();

            // 指定した子オブジェクトから Animator を取得
            animator = transform.GetChild(animatorChildIndex).GetComponent<Animator>();

            GetComponent<EnemyLifespan>().OnLifespanExpired += OnLifespanExpired;
            GetComponent<EnemyHitDetector>().OnPlayerHit += OnPlayerHit;
        }

        private void OnLifespanExpired()
        {
            isChasing = false;
            agent.isStopped = true;
        }

        private void OnPlayerHit()
        {
            isChasing = false;
            agent.isStopped = true;
        }

        public void SetStoryMode(bool isStoryMode)
        {
            this.isStoryMode = isStoryMode;
        }

        /// <summary>
        /// Bolt の「On Update」相当
        /// 毎フレームごとに速度をチェックしてアニメーターに反映する
        /// </summary>
        void Update()
        {
            // 1. オブジェクトの速度（NavMeshAgent の速度）を取得し、アニメーターの Bool を設定
            float speed = agent.velocity.magnitude;
            bool isMoving = speed > speedThreshold;

            // アニメーション用パラメータをセット
            animator.SetBool(animatorBoolParam, isMoving);
        }

        /// <summary>
        /// Bolt の「On Fixed Update」相当
        /// 物理演算やエージェントの移動更新タイミングに合わせて呼び出される
        /// </summary>
        void FixedUpdate()
        {
            // Debug.Log($"IsStoryMode:{IsStoryMode}, GameStateManager.Instance.CurrentState:{GameStateManager.Instance.CurrentState}, isChasing:{isChasing}");
            if (this.IsStoryMode)
            {
                // ストーリーモードの場合は、プレイヤーを追いかけない
                return;
            }
            if (GameStateManager.Instance.CurrentState != GameState.Gameplay)
            {
                // ゲームがプレイ中でない場合は、入力を無視
                return;
            }
            if (!isChasing) return;

            // 2. タグからプレイヤーを探して、その位置を目標地点に設定
            if (player == null)
            {
                player = GameObject.FindWithTag(playerTag);
            }

            if (player != null)
            {
                moveControl.SetTargetPosition(player.transform.position);

                // Debug.Log($"Player position: {targetPosition}");
            }
        }

        public void FaceTarget(GameObject target)
        {
            if (target == null)
                return;
            StartCoroutine(FaceTargetRoutine(target));
        }

        private IEnumerator FaceTargetRoutine(GameObject target)
        {
            // プレイヤーとターゲットの水平な位置を取得
            Vector3 playerPosition = transform.position;
            Vector3 targetPosition = target.transform.position;

            // Y軸は無視して水平な方向ベクトルを計算
            Vector3 desiredDirection = targetPosition - playerPosition;
            desiredDirection.y = 0f;

            if (desiredDirection.sqrMagnitude < 0.0001f)
                yield break;

            desiredDirection.Normalize();

            // カメラのY軸回転を取得（見下ろし視点でも、カメラのY軸は有効と仮定）
            Transform cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                Debug.LogWarning("Camera.mainが見つかりません。");
                yield break;
            }
            // カメラのY軸回転（水平回転）のみを抽出
            Quaternion cameraYawRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

            // MoveControl 内部では
            //   _moveDirection = cameraYawRotation * (leftStickInput.normalized)
            // となっているため、desiredDirection になるようにするには
            //   leftStickInput = Quaternion.Inverse(cameraYawRotation) * desiredDirection
            Vector3 leftStickInput3D = Quaternion.Inverse(cameraYawRotation) * desiredDirection;
            Vector2 leftStickInput = new Vector2(leftStickInput3D.x, leftStickInput3D.z);

            // --- ここで “WASD 相当” に丸める ---
            Vector2 snappedInput = SnapToKeyboardDirections(leftStickInput, 0.5f);

            // 向き更新用に一時的に入力を送る
            // moveControl.Move(snappedInput);

            // 0.01秒待ってから入力をクリア（必要に応じて調整）
            yield return new WaitForSeconds(0.1f);
            // moveControl.Move(Vector2.zero);
        }

        /// <summary>
        /// アナログ入力ベクトルを 8方向(上下左右＋斜め)にスナップ(0/1化)するヘルパーメソッド
        /// </summary>
        /// <param name="input">アナログ入力</param>
        /// <param name="threshold">しきい値。例:0.5f</param>
        /// <returns>上下左右斜めいずれかの(-1,0,1)成分を持つ Vector2</returns>
        private Vector2 SnapToKeyboardDirections(Vector2 input, float threshold)
        {
            float x = 0f;
            float y = 0f;

            // X成分が threshold を超えたら ±1、それ以下なら 0
            if (Mathf.Abs(input.x) >= threshold)
            {
                x = Mathf.Sign(input.x);
            }

            // Y成分が threshold を超えたら ±1、それ以下なら 0
            if (Mathf.Abs(input.y) >= threshold)
            {
                y = Mathf.Sign(input.y);
            }

            // これで上下左右・斜めのいずれか(8方向)になる
            return new Vector2(x, y);
        }

        public void StopChasing()
        {
            // ここではスクリプト自体を無効化
            this.enabled = false;
            agent.isStopped = true;
        }

        /// <summary>
        /// 強制移動が完了したら呼び出して、PlayerActionControllerを再有効化する
        /// </summary>
        public void StartChasing()
        {
            this.enabled = true;
            agent.isStopped = false;
        }

        public IEnumerator SpawnNearPlayer(Vector3 playerPosition, float spawnDistance = 5f, float waitTIme = 3f)
        {
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * spawnDistance;
            randomDirection.y = 0;
            Vector3 spawnPosition = playerPosition + randomDirection;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPosition, out hit, spawnDistance, NavMesh.AllAreas))
            {
                GetComponent<BrainBase>().Warp(hit.position, randomDirection);
                // StartMoving(); // スポーン後に動き出す
            }
            else
            {
                Debug.LogWarning("有効なスポーン位置が見つかりませんでした。");
            }
            yield return new WaitForSeconds(waitTIme);
        }

        /// <summary>
        /// プレイヤー位置の近くにスポーンし、指定時間待機する非同期メソッド
        /// </summary>
        /// <param name="playerPosition">プレイヤーのワールド座標</param>
        /// <param name="spawnDistance">スポーン位置をランダムに散らす半径（デフォルト5m）</param>
        /// <param name="waitTime">スポーン後に待機する秒数（デフォルト3秒）</param>
        public async UniTask SpawnNearPlayerAsync(
            Vector3 playerPosition,
            float spawnDistance = 5f,
            float waitTime = 3f)
        {
            // 1. ランダム方向を算出し、高さは固定
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * spawnDistance;
            randomDirection.y = 0f;

            // 2. 実際のスポーン候補位置
            Vector3 spawnPosition = playerPosition + randomDirection;

            // 3. NavMesh上の有効な位置をサンプリング
            if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, spawnDistance, NavMesh.AllAreas))
            {
                // BrainBase コンポーネントの Warp メソッドで瞬間移動
                GetComponent<BrainBase>().Warp(hit.position, randomDirection);
                // 必要ならここで StartMoving() などを呼ぶ
            }
            else
            {
                Debug.LogWarning("有効なスポーン位置が見つかりませんでした。");
            }

            // 4. waitTime 秒だけ待機（コルーチンの WaitForSeconds 相当）
            await UniTask.Delay(TimeSpan.FromSeconds(waitTime));
        }


        public void SetChasing(bool chasing)
        {
            Debug.Log($"SetChasing:{chasing}");
            isChasing = chasing;
            if (isChasing)
            {
                // ChasePlayer(); // プレイヤーを追いかける処理を実行
            }
            else
            {
                // StopChasing(); // プレイヤーの追跡を停止する処理を実行
            }
        }
    }
}