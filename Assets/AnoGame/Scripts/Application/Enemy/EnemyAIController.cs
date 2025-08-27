using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using AnoGame.Application.Player.Control;
using Unity.TinyCharacterController.Control;
using Unity.TinyCharacterController.Core; // NOTE:微妙...別のnamespaceがいい
using AnoGame.Application.Enemy;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.TinyCharacterController.Brain; // ★ 追加: CancellationToken 用

namespace AnoGame.Application.Enmemy.Control
{
    public class EnemyAIController : MonoBehaviour, IForcedMoveController
    {
        // 先頭のフィールド群に追加
        [SerializeField] private CharacterBrain characterBrain;

        [SerializeField] private MoveNavmeshControl moveControl;

        [Header("ターゲット")]
        [SerializeField] private string playerTag = "Player";

        [Header("アニメーション")]
        [SerializeField] private float speedThreshold = 0.1f;
        [SerializeField] private int animatorChildIndex = 0;
        [SerializeField] private string animatorBoolParam = "IsMove";

        [Header("ステート")]
        [SerializeField] private bool isChasing = false;
        public bool IsChasing => isChasing;

        [SerializeField] private bool isStoryMode = false;
        public bool IsStoryMode => isStoryMode;

        [Header("突進(Dash) 設定")]
        [Tooltip("突進機能を有効にするか")]
        [SerializeField] private bool enableDash = false;

        [Tooltip("突進前の待機秒数")]
        [SerializeField, Min(0f)] private float dashPreWaitSeconds = 2.0f;

        [Tooltip("突進後の待機秒数")]
        [SerializeField, Min(0f)] private float dashPostWaitSeconds = 3.0f;

        [Tooltip("突進距離(直線)")]
        [SerializeField, Min(0.1f)] private float dashDistance = 10f;

        [Tooltip("突進速度(m/s)")]
        [SerializeField, Min(0.1f)] private float dashSpeed = 8f;

        [Tooltip("突進中はターゲット方向へ即座に向きを合わせる")]
        [SerializeField] private bool dashFaceTarget = true;

        private bool _isDashing;
        public bool IsDashing => _isDashing;

        private NavMeshAgent agent;
        private Animator animator;
        private GameObject player;

        // Dashを途中で止めるためのCTS
        private CancellationTokenSource _dashCts;

        void Start()
        {
            if (moveControl == null)
                moveControl = GetComponent<MoveNavmeshControl>();

            agent = GetComponentInChildren<NavMeshAgent>();
            animator = transform.GetChild(animatorChildIndex).GetComponent<Animator>();
            if (characterBrain == null)
                characterBrain = GetComponent<CharacterBrain>();
        }

        void OnDisable()
        {
            CancelDashIfRunning();
        }

        void OnDestroy()
        {
            CancelDashIfRunning();
        }

        private void CancelDashIfRunning()
        {
            if (_dashCts != null)
            {
                _dashCts.Cancel();
                _dashCts.Dispose();
                _dashCts = null;
            }
            _isDashing = false;
        }

        private void OnLifespanExpired()
        {
            isChasing = false;
            if (agent != null) agent.isStopped = true;
        }

        private void OnPlayerHit()
        {
            isChasing = false;
            if (agent != null) agent.isStopped = true;
        }

        public void SetStoryMode(bool isStoryMode)
        {
            Debug.Log($"isStoryMode:{isStoryMode}");
            this.isStoryMode = isStoryMode;
        }

        void Update()
        {
            // ★ 追加: ダッシュ中は IsMove を Update で上書きしない
            if (_isDashing) return;

            if (agent == null || animator == null) return;
            float speed = agent.velocity.magnitude;
            bool isMoving = speed > speedThreshold;
            animator.SetBool(animatorBoolParam, isMoving);
        }

        void FixedUpdate()
        {
            if (this.IsStoryMode) return;
            if (GameStateManager.Instance.CurrentState == GameState.GameOver) return;
            if (GameStateManager.Instance.CurrentState == GameState.InGameEvent) return;

            // ★ ダッシュ中 or 非チェイス中 は通常チェイス停止
            if (!isChasing || _isDashing) return;

            if (player == null)
                player = GameObject.FindWithTag(playerTag);

            if (player != null)
                moveControl.SetTargetPosition(player.transform.position);
        }

        public void FaceTarget(GameObject target)
        {
            if (target == null) return;
            StartCoroutine(FaceTargetRoutine(target));
        }

        private IEnumerator FaceTargetRoutine(GameObject target)
        {
            Vector3 playerPosition = transform.position;
            Vector3 targetPosition = target.transform.position;

            Vector3 desiredDirection = targetPosition - playerPosition;
            desiredDirection.y = 0f;

            if (desiredDirection.sqrMagnitude < 0.0001f)
                yield break;

            desiredDirection.Normalize();

            Transform cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                Debug.LogWarning("Camera.mainが見つかりません。");
                yield break;
            }
            Quaternion cameraYawRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

            Vector3 leftStickInput3D = Quaternion.Inverse(cameraYawRotation) * desiredDirection;
            Vector2 leftStickInput = new Vector2(leftStickInput3D.x, leftStickInput3D.z);
            Vector2 snappedInput = SnapToKeyboardDirections(leftStickInput, 0.5f);

            yield return new WaitForSeconds(0.1f);
        }

        private Vector2 SnapToKeyboardDirections(Vector2 input, float threshold)
        {
            float x = 0f;
            float y = 0f;

            if (Mathf.Abs(input.x) >= threshold) x = Mathf.Sign(input.x);
            if (Mathf.Abs(input.y) >= threshold) y = Mathf.Sign(input.y);

            return new Vector2(x, y);
        }

        public void StopChasing()
        {
            this.enabled = false;
            if (agent != null) agent.isStopped = true;
        }

        public void StartChasing()
        {
            this.enabled = true;
            if (agent != null) agent.isStopped = false;
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
            }
            else
            {
                Debug.LogWarning("有効なスポーン位置が見つかりませんでした。");
            }

            // コルーチン版は挙動だけ合わせておく（DashはAsync版に寄せる）
            yield return new WaitForSeconds(waitTIme);
        }

        /// <summary>
        /// プレイヤー位置の近くにスポーンし、指定時間待機する非同期メソッド
        /// スポーン後のウィンドウ内で突進(Dash)を1回実行し、ウィンドウ終了時点でDashをキャンセル可能。
        /// </summary>
        public async UniTask SpawnNearPlayerAsync(
            Vector3 playerPosition,
            float spawnDistance = 5f,
            float waitTime = 3f)
        {
            // 1) ワープ
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * spawnDistance;
            randomDirection.y = 0f;
            Vector3 spawnPosition = playerPosition + randomDirection;

            if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, spawnDistance, NavMesh.AllAreas))
            {
                GetComponent<BrainBase>().Warp(hit.position, randomDirection);
            }
            else
            {
                Debug.LogWarning("有効なスポーン位置が見つかりませんでした。");
            }

            // ★ ここではダッシュさせず、単に待つだけ（演出や猶予タイム）
            await UniTask.Delay(TimeSpan.FromSeconds(waitTime));
        }

        /// <summary>
        /// 突進(Dash)を1回だけ行う。途中でキャンセルされたら安全に終了。
        /// </summary>
        public async UniTask DashOnceAsync(CancellationToken token)
        {
            if (IsStoryMode) return;
            if (GameStateManager.Instance.CurrentState == GameState.GameOver) return;
            if (GameStateManager.Instance.CurrentState == GameState.InGameEvent) return;

            if (player == null)
                player = GameObject.FindWithTag(playerTag);
            if (player == null || agent == null) return;

            // 既存チェイス状態を退避
            bool prevChasing = isChasing;
            bool prevStopped = agent.isStopped;

            _isDashing = true;

            try
            {
                // 1) 通常チェイス停止
                isChasing = false;
                agent.ResetPath();
                agent.isStopped = true;

                // 2) プレ突進待機
                if (dashPreWaitSeconds > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(dashPreWaitSeconds), cancellationToken: token);

                // 3) 方向計算
                Vector3 toPlayer = player.transform.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude < 0.0001f) return;

                Vector3 dir = toPlayer.normalized;

                if (dashFaceTarget)
                {
                    ApplyFacingAndAnimatorAngle(dir /*, backstep:false*/);

                }

                // 4) 直線ダッシュ（NavMeshAgent.Move ではなく CharacterBrain.ForceSetPosition で押し出す）
                float remaining = dashDistance;

                // ★ NavMeshAgent が Transform を駆動しないように一時停止（Transform直書き対策）
                bool prevUpdatePos = agent != null ? agent.updatePosition : true;
                bool prevUpdateRot = agent != null ? agent.updateRotation : true;
                if (agent != null)
                {
                    agent.updatePosition = false;
                    agent.updateRotation = false;
                }

                // ★ アニメ側もダッシュ中は確実に「移動中」にする
                bool prevAnimMove = false;
                if (animator != null)
                {
                    prevAnimMove = animator.GetBool(animatorBoolParam);
                    animator.SetBool(animatorBoolParam, true);
                }

                try
                {
                    while (remaining > 0f)
                    {
                        token.ThrowIfCancellationRequested();

                        float step = dashSpeed * Time.deltaTime;
                        float moveLen = Mathf.Min(step, remaining);

                        Vector3 current = transform.position;
                        Vector3 candidate = current + dir * moveLen;

                        // ★ 向き＆Angle を毎フレーム更新（回転は CharacterBrain に強制適用）
                        if (dashFaceTarget)
                        {
                            ApplyFacingAndAnimatorAngle(dir /*, backstep:false*/);
                        }

                        // ★ 障害物チェック
                        if (NavMesh.Raycast(current, candidate, out var hitInfo, NavMesh.AllAreas))
                        {
                            Vector3 stopPos = hitInfo.position - dir * 0.05f;
                            if (NavMesh.SamplePosition(stopPos, out var snap, 0.2f, NavMesh.AllAreas))
                                stopPos = snap.position;

                            characterBrain.ForceSetPosition(stopPos);
                            break;
                        }

                        // ★ NavMesh 吸着 & 実移動
                        if (NavMesh.SamplePosition(candidate, out var sample, 0.2f, NavMesh.AllAreas))
                            candidate = sample.position;

                        characterBrain.ForceSetPosition(candidate);

                        // 実進行分だけ残距離を減算
                        remaining -= Vector3.Distance(current, transform.position);

                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                }
                finally
                {
                    // ★ NavMeshAgent と同期を戻す
                    if (agent != null)
                    {
                        // 現在位置をエージェントに認識させる
                        agent.Warp(agent.transform.position);
                        agent.updatePosition = prevUpdatePos;
                        agent.updateRotation = prevUpdateRot;
                    }

                    // ★ アニメの移動フラグを元に戻す
                    if (animator != null)
                        animator.SetBool(animatorBoolParam, prevAnimMove);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は安全に元状態へ
                isChasing = prevChasing;
                agent.isStopped = prevStopped;
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                // 例外時も復帰させておく
                isChasing = prevChasing;
                agent.isStopped = prevStopped;
            }
            finally
            {
                _isDashing = false;
            }
        }

        public void SetChasing(bool chasing)
        {
            Debug.Log($"SetChasing:{chasing}");

            isChasing = chasing;
            if (agent == null) return;

            if (isChasing)
            {
                // 通常チェイスを有効化
                agent.isStopped = false;

                // ★ チェイス開始時に1回だけ突進（有効化時のみ）
                if (enableDash)
                {
                    // 既存ダッシュがあれば止める（多重起動防止）
                    CancelDashIfRunning();

                    _dashCts = new CancellationTokenSource();
                    // 非同期起動（awaitしない）
                    UniTask.Void(async () =>
                    {
                        try
                        {
                            await DashFlowOnChaseStartAsync(_dashCts.Token);
                        }
                        catch (OperationCanceledException) { /* キャンセルは想定内 */ }
                        finally
                        {
                            // ダッシュタスク終了後に片付け
                            CancelDashIfRunning();
                        }
                    });
                }
            }
            else
            {
                // ★ チェイス停止時はダッシュもキャンセル
                CancelDashIfRunning();
                agent.isStopped = true;
            }
        }

        // ★ 新規追加：チェイス開始時にだけ走らせる1回分のフロー
        private async UniTask DashFlowOnChaseStartAsync(CancellationToken token)
        {
            if (IsStoryMode) return;
            if (GameStateManager.Instance.CurrentState == GameState.GameOver) return;
            if (GameStateManager.Instance.CurrentState == GameState.InGameEvent) return;
            if (!enableDash) return;
            if (characterBrain == null || agent == null) return;

            if (player == null)
                player = GameObject.FindWithTag(playerTag);
            if (player == null) return;

            // ※ 通常チェイスは on のままにしつつ、_isDashing フラグでFixedUpdateを止める方式にします。
            //    エージェントのTransform支配を止めるため、一時的に updatePosition/Rotation を切ります。
            bool prevUpdatePos = agent.updatePosition;
            bool prevUpdateRot = agent.updateRotation;

            try
            {
                _isDashing = true;

                agent.ResetPath();
                agent.isStopped = true;     // 経路追従は止める
                agent.updatePosition = false;
                agent.updateRotation = false;

                // 実ダッシュ（内部で pre/post wait、ForceSetPosition による直線移動）
                GetComponent<CameraAngleToAnimatorAndSprite>()?.OnForcedMoveBegin();
                await DashOnceAsync(token);
                GetComponent<CameraAngleToAnimatorAndSprite>()?.OnForcedMoveEnd();
            }
            finally
            {
                // 復帰：位置同期してから制御フラグ戻す
                agent.Warp(agent.transform.position);
                agent.updatePosition = prevUpdatePos;
                agent.updateRotation = prevUpdateRot;

                agent.isStopped = !isChasing;  // チェイス継続なら false、停止なら true
                _isDashing = false;
            }
        }


        private float RoundAngleTo45(float angle)
        {
            return Mathf.Round(angle / 45f) * 45f;
        }

        private void ApplyFacingAndAnimatorAngle(Vector3 dir, bool backstep = false)
        {
            if (dir.sqrMagnitude < 1e-6f) return;

            // 進行方向 → Yaw角
            float forcedYawAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            var desiredRotation = Quaternion.Euler(0f, forcedYawAngle, 0f);

            // ★ 回転は CharacterBrain に強制適用
            if (characterBrain != null) characterBrain.ForceSetRotation(desiredRotation);
            else transform.rotation = desiredRotation;

            // ★ Animator の Angle をカメラ相対で設定（45°刻み）
            if (animator != null && Camera.main != null)
            {
                float cameraY = Camera.main.transform.eulerAngles.y;
                float relativeAngle = Mathf.DeltaAngle(cameraY, forcedYawAngle);
                if (backstep)
                {
                    // バックステップ対応（必要なら）
                    relativeAngle = Mathf.DeltaAngle(0f, relativeAngle - 180f);
                }
                relativeAngle = RoundAngleTo45(relativeAngle);
                animator.SetFloat("Angle", relativeAngle);
            }
        }

    }
}
