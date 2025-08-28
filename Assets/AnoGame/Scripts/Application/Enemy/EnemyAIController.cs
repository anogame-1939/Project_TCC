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
using Unity.TinyCharacterController.Brain;
using AnoGame.Application.Gameplay; // ★ 追加: CancellationToken 用

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

        [Header("アニメーション速度")]
        [SerializeField] private string locomotionSpeedParam = "LocomotionSpeed";
        [SerializeField, Min(0.05f)] private float chaseAnimSpeedMult = 1.0f; // 通常
        [SerializeField, Min(0.05f)] private float dashAnimSpeedMult = 1.6f; // ダッシュ時

        [Header("ステート")]
        [SerializeField] private bool isChasing = false;
        public bool IsChasing => isChasing;

        [SerializeField] private bool isStoryMode = false;
        public bool IsStoryMode => isStoryMode;

        private enum DashPattern { Random, Straight, Homing, ZigZag }

        [Header("突進(Dash) 設定")]
        [Header("ダッシュパターン")]
        [SerializeField] private DashPattern dashPattern = DashPattern.Random; // Random=毎回ランダム
        [Tooltip("突進機能を有効にするか")]
        [SerializeField] private bool enableDash = false;

        [Tooltip("突進前の待機秒数")]
        [SerializeField, Min(0f)] private float dashPreWaitSeconds = 2.0f;

        [Tooltip("突進後の待機秒数")]
        [SerializeField, Min(0f)] private float dashPostWaitSeconds = 0.5f;

        [Tooltip("次の突進までのクールダウン最小秒（チェイスしながら経過、ランダム）")]
        [SerializeField, Min(0f)] private float dashCooldownMinSeconds = 1f;

        [Tooltip("次の突進までのクールダウン最大秒（チェイスしながら経過、ランダム）")]
        [SerializeField, Min(0f)] private float dashCooldownMaxSeconds = 3f;


        [Tooltip("突進距離(直線)")]
        [SerializeField, Min(0.1f)] private float dashDistance = 10f;

        [Tooltip("突進速度(m/s)")]
        [SerializeField, Min(0.1f)] private float dashSpeed = 8f;

        [Tooltip("突進中はターゲット方向へ即座に向きを合わせる")]
        [SerializeField] private bool dashFaceTarget = true;

        [Header("追尾ダッシュ(2) 設定")]
        [Tooltip("追尾時のダッシュ速度倍率（基準 dashSpeed に対する%）。")]
        [SerializeField, Range(0.1f, 1.5f)] private float homingSpeedMultiplier = 0.7f;
        [Tooltip("旋回の鋭さ（大きいほど素早くプレイヤー方向へ向く）")]
        [SerializeField, Min(0f)] private float homingTurnSharpness = 6f; // 1/s 程度

        [Header("ジグザグダッシュ(3) 設定")]
        [Tooltip("ジグザグ時のダッシュ速度倍率（基準 dashSpeed に対する%）。")]
        [SerializeField, Range(0.1f, 1.5f)] private float zigzagSpeedMultiplier = 0.6f;
        [Tooltip("最大偏向角（度）。左右にこの角度まで振ります。")]
        [SerializeField, Range(0f, 60f)] private float zigzagMaxAngleDeg = 25f;
        [Tooltip("左右の振り回し周波数（Hz）。1秒間に何回左右に振るか。")]
        [SerializeField, Range(0.1f, 10f)] private float zigzagFrequencyHz = 3f;

        [Header("チェイス時の向き制御")]
        [SerializeField] private bool controlFacingWhileChasing = true;           // 通常チェイスも向きを制御
        [SerializeField, Min(0f)] private float chaseTurnSharpness = 8f;          // 回頭の鋭さ(>大=キビキビ)

        private bool _isDashing;
        public bool IsDashing => _isDashing;

        private NavMeshAgent agent;
        private Animator animator;
        private GameObject player;

        // Dashを途中で止めるためのCTS
        private CancellationTokenSource _dashCts;
        private CancellationTokenSource _chaseLoopCts;

        void Start()
        {
            if (moveControl == null)
                moveControl = GetComponent<MoveNavmeshControl>();

            agent = GetComponentInChildren<NavMeshAgent>();
            animator = transform.GetChild(animatorChildIndex).GetComponent<Animator>();
            if (characterBrain == null)
                characterBrain = GetComponent<CharacterBrain>();
        }

        private void CancelChaseLoopIfRunning()
        {
            if (_chaseLoopCts != null)
            {
                _chaseLoopCts.Cancel();
                _chaseLoopCts.Dispose();
                _chaseLoopCts = null;
            }
            // 進行中のDashも確実に止める
            CancelDashIfRunning();
        }

        // 既存: OnDisable/OnDestroy にも ChaseLoop の停止を追加
        void OnDisable()
        {
            CancelChaseLoopIfRunning();
            CancelDashIfRunning();
        }

        void OnDestroy()
        {
            CancelChaseLoopIfRunning();
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

            // ★ 追加：見た目の向きを合わせる（NavMeshAgentは回さない）
            if (controlFacingWhileChasing)
                UpdateChaseFacing();
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
            Transform player, 
            float spawnDistance = 5f,
            float waitTime = 3f,
            float moveDelay = 0f,           // ← EnemySpawnManagerのmoveDelayを渡す
            float predictSeconds = 0.6f,    // ← 後述の予測時間
            float coneAngleDeg = 35f,       // ← 前方ウェッジ角
            CancellationToken token = default)
        {
            // 1) 予測に使う「将来の基準点」を先に計算（待機＋演出分を考慮）
            float totalLead = Mathf.Max(0f, waitTime + moveDelay + predictSeconds);
            Vector3 basisPos = PredictPlayerPosition(player, totalLead);

            // 2) 前方ウェッジ（±coneAngle）内でランダム方向を選ぶ
            Vector3 fwd = GetSmoothedForward(player); // 後述のスムーズ前方
            Vector3 dir = Quaternion.Euler(0f, UnityEngine.Random.Range(-coneAngleDeg, coneAngleDeg), 0f) * fwd;

            // 3) 候補点（NavMeshに吸着）
            Vector3 candidate = basisPos + dir * spawnDistance;
            if (NavMesh.SamplePosition(candidate, out var hit, spawnDistance, NavMesh.AllAreas))
                candidate = hit.position;

            // 4) ← ここではワープしない。まず猶予時間を待つ
            await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: token);

            // 5) 直前に最終補正（プレイヤーが大きく曲がっていたらミラー補正）
            Vector3 final = EnsureFrontOfPlayer(player.position, candidate, fwd);
            if (NavMesh.SamplePosition(final, out var snap, 0.4f, NavMesh.AllAreas))
                final = snap.position;

            GetComponent<BrainBase>()?.Warp(final, dir); // ★ late-warp
        }

        // --- ユーティリティ ---
        private Vector3 PredictPlayerPosition(Transform player, float t)
        {
            // 簡易：直近フレームの速度 or 補助コンポから取得（無ければforward）
            var tracker = player.GetComponent<MotionTracker>(); // 後述
            Vector3 v = tracker ? tracker.SmoothedVelocity : player.forward * 3.5f; // 想定移動速度をフォールバック
            v.y = 0f;
            return player.position + v * Mathf.Max(0f, t);
        }

        private Vector3 GetSmoothedForward(Transform player)
        {
            var tracker = player.GetComponent<MotionTracker>();
            Vector3 f = tracker && tracker.SmoothedForward.sqrMagnitude > 0.01f
                ? tracker.SmoothedForward : player.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }

        private Vector3 EnsureFrontOfPlayer(Vector3 playerPos, Vector3 point, Vector3 playerFwd)
        {
            // 前後判定して後方なら“前方ミラー”に反転
            Vector3 offset = point - playerPos;
            offset.y = 0f;
            float along = Vector3.Dot(offset, playerFwd);
            if (along < 0f)
            {
                // forward 成分だけ符号反転 = 前方へミラー
                Vector3 fComp = Vector3.Project(offset, playerFwd);
                Vector3 nComp = offset - fComp;
                offset = -fComp + nComp;
            }
            return playerPos + offset;
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

                // ★ Animator元値を取得してから、待機中は「停止」へ
                bool prevAnimMove = false;
                if (animator != null)
                {
                    prevAnimMove = animator.GetBool(animatorBoolParam);
                    animator.SetBool(animatorBoolParam, false);  // 待機中は歩かせない
                }

                // 2) ★ プレ待機を毎フレームループ化し、常にプレイヤー方向へ向く
                if (dashPreWaitSeconds > 0f)
                {
                    float endTime = Time.time + dashPreWaitSeconds;

                    // ※ DashFlowAsync 側で agent.updateRotation=false 済み（念のため止めたい場合はここでも false を保証）
                    // if (agent != null) agent.updateRotation = false;

                    while (Time.time < endTime)
                    {
                        token.ThrowIfCancellationRequested();

                        if (player == null)
                            player = GameObject.FindWithTag(playerTag);

                        if (player != null)
                        {
                            Vector3 toPlayer = player.transform.position - transform.position;
                            toPlayer.y = 0f;

                            if (toPlayer.sqrMagnitude > 0.0001f && dashFaceTarget)
                            {
                                Vector3 dir = toPlayer.normalized;
                                ApplyFacingAndAnimatorAngle(dir);   // ★ 毎フレーム向きを更新（Angleパラメータも更新）
                            }
                        }

                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                }

                // 3) 方向計算（直前の最新位置で算出）
                Vector3 toPlayer2 = player != null ? (player.transform.position - transform.position) : Vector3.zero;
                toPlayer2.y = 0f;
                if (toPlayer2.sqrMagnitude < 0.0001f) return;

                Vector3 dirDash = toPlayer2.normalized;

                if (dashFaceTarget)
                {
                    ApplyFacingAndAnimatorAngle(dirDash);
                }

                // 4) 直線ダッシュ準備（Transform直駆動）
                float remaining = dashDistance;

                bool prevUpdatePos = agent != null ? agent.updatePosition : true;
                bool prevUpdateRot = agent != null ? agent.updateRotation : true;
                if (agent != null)
                {
                    agent.updatePosition = false;
                    agent.updateRotation = false;
                }

                // ★ 実ダッシュに入る直前で「移動中」にする（prevAnimMove は上書きしない！）
                if (animator != null)
                {
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
                        Vector3 candidate = current + dirDash * moveLen;

                        // 向き＆Angle を毎フレーム更新
                        if (dashFaceTarget)
                        {
                            ApplyFacingAndAnimatorAngle(dirDash);
                        }

                        // 障害物チェック
                        if (NavMesh.Raycast(current, candidate, out var hitInfo, NavMesh.AllAreas))
                        {
                            Vector3 stopPos = hitInfo.position - dirDash * 0.05f;
                            if (NavMesh.SamplePosition(stopPos, out var snap, 0.2f, NavMesh.AllAreas))
                                stopPos = snap.position;

                            characterBrain.ForceSetPosition(stopPos);
                            break;
                        }

                        // NavMesh 吸着 & 実移動
                        if (NavMesh.SamplePosition(candidate, out var sample, 0.2f, NavMesh.AllAreas))
                            candidate = sample.position;

                        characterBrain.ForceSetPosition(candidate);

                        remaining -= Vector3.Distance(current, transform.position);
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                }
                finally
                {
                    // NavMeshAgent と同期を戻す
                    if (agent != null)
                    {
                        agent.Warp(agent.transform.position);
                        agent.updatePosition = prevUpdatePos;
                        agent.updateRotation = prevUpdateRot;
                    }

                    // Animator の移動フラグを元に戻す（※ prevAnimMove を再利用）
                    if (animator != null)
                        animator.SetBool(animatorBoolParam, prevAnimMove);
                }
            }
            catch (OperationCanceledException)
            {
                isChasing = prevChasing;
                agent.isStopped = prevStopped;
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
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
                agent.isStopped = false;
                if (controlFacingWhileChasing) UpdateChaseFacing();

                // ループを開始（多重起動防止）
                CancelChaseLoopIfRunning();

                if (enableDash)
                {
                    _chaseLoopCts = new CancellationTokenSource();
                    UniTask.Void(async () =>
                    {
                        try
                        {
                            await ChaseCycleAsync(_chaseLoopCts.Token);
                        }
                        catch (OperationCanceledException) { /* 正常キャンセル */ }
                        finally
                        {
                            // ループ終了時の片付け
                            CancelChaseLoopIfRunning();
                            animator.speed = chaseAnimSpeedMult;
                            isChasing = false;
                        }
                    });
                }
            }
            else
            {
                // チェイス停止：ループとDashを確実に停止
                CancelChaseLoopIfRunning();
                agent.isStopped = true;
            }
        }

        private async UniTask ChaseCycleAsync(CancellationToken token)
        {
            // ベースライン：まずは通常チェイスを開始
            isChasing = true;
            if (agent != null) agent.isStopped = false;

            // 0) 初回ダッシュまでのクールダウン（通常チェイス中）
            float firstCooldown = GetDashCooldownSeconds();
            if (firstCooldown > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(firstCooldown), cancellationToken: token);

            // 以降は「ダッシュ → 復帰待機 → チェイス再開 → クールダウン」を繰り返す
            while (isChasing && !token.IsCancellationRequested)
            {
                // 1) ダッシュ（内部で pre-wait と直線ダッシュを実行）
                await DashFlowAsync(token);
                if (token.IsCancellationRequested) break;

                // 2) 復帰待機：ダッシュ直後の硬直（その間は停止）
                if (agent != null) agent.isStopped = true;
                if (dashPostWaitSeconds > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(dashPostWaitSeconds), cancellationToken: token);
                if (token.IsCancellationRequested) break;

                // 3) チェイス再開（FixedUpdateで追尾が走る）
                isChasing = true;
                if (agent != null) agent.isStopped = false;

                // 4) クールダウン：次のダッシュまでの待機（通常チェイス中に経過、ランダム）
                float cooldown = GetDashCooldownSeconds();
                if (cooldown > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(cooldown), cancellationToken: token);

                // ループ先頭へ（再びダッシュ）
            }
        }

        private float GetDashCooldownSeconds()
        {
            float min = Mathf.Max(0f, dashCooldownMinSeconds);
            float max = Mathf.Max(min, dashCooldownMaxSeconds); // min > max の場合でも安全に
            if (Mathf.Approximately(min, max)) return min;
            return UnityEngine.Random.Range(min, max);
        }

        // ▼ 既存: 「チェイス開始時に一回だけ」版は互換のため残すが、中身は共通化
        private async UniTask DashFlowOnChaseStartAsync(CancellationToken token)
        {
            await DashFlowAsync(token);
        }

        private async UniTask DashFlowAsync(CancellationToken token)
        {
            if (IsStoryMode) return;
            if (GameStateManager.Instance.CurrentState == GameState.GameOver) return;
            if (GameStateManager.Instance.CurrentState == GameState.InGameEvent) return;
            if (!enableDash) return;
            if (characterBrain == null || agent == null) return;

            if (player == null)
                player = GameObject.FindWithTag(playerTag);
            if (player == null) return;

            bool prevUpdatePos = agent.updatePosition;
            bool prevUpdateRot = agent.updateRotation;

            try
            {
                _isDashing = true;

                agent.ResetPath();
                agent.isStopped = true;     // 経路追従は止める
                agent.updatePosition = false;
                agent.updateRotation = false;

                GetComponent<CameraAngleToAnimatorAndSprite>()?.OnForcedMoveBegin();

                // ★ パターン選択
                DashPattern selected = dashPattern;
                if (selected == DashPattern.Random)
                {
                    // Straight=1, Homing=2, ZigZag=3 を等確率で
                    selected = (DashPattern)UnityEngine.Random.Range(1, 4);
                }

                // ★ パターンに応じてアニメ速度も合わせる（見た目の整合性）
                float typeSpeedMult = 1f;
                switch (selected)
                {
                    case DashPattern.Homing: typeSpeedMult = homingSpeedMultiplier; break;
                    case DashPattern.ZigZag: typeSpeedMult = zigzagSpeedMultiplier; break;
                        // Straight は 1.0
                }
                if (animator != null) animator.speed = dashAnimSpeedMult * typeSpeedMult;

                // ★ 各パターン実行
                switch (selected)
                {
                    case DashPattern.Straight:
                        Debug.Log("Dash: Straight");
                        await DashOnceAsync(token);            // 既存の直線
                        break;
                    case DashPattern.Homing:
                        Debug.Log("Dash: Homing");
                        await DashOnceHomingAsync(token);      // 追尾（速度落とす）
                        break;
                    case DashPattern.ZigZag:
                        Debug.Log("Dash: ZigZag");
                        await DashOnceZigZagAsync(token);      // ジグザグ（速度落とす）
                        break;
                }

                // 終了/キャンセル時
                GetComponent<CameraAngleToAnimatorAndSprite>()?.OnForcedMoveEnd();
                if (animator != null) animator.speed = chaseAnimSpeedMult;
            }
            finally
            {
                // 位置同期して制御フラグを戻す
                agent.Warp(agent.transform.position);
                agent.updatePosition = prevUpdatePos;
                agent.updateRotation = prevUpdateRot;

                // この後の通常チェイス再開は呼び出し側（ループ側）で agent.isStopped=false に戻す
                agent.isStopped = !isChasing;

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

        private async UniTask DashOnceHomingAsync(CancellationToken token)
        {
            if (IsStoryMode) return;
            if (GameStateManager.Instance.CurrentState == GameState.GameOver) return;
            if (GameStateManager.Instance.CurrentState == GameState.InGameEvent) return;

            if (player == null)
                player = GameObject.FindWithTag(playerTag);
            if (player == null || agent == null) return;

            bool prevChasing = isChasing;
            bool prevStopped = agent.isStopped;

            _isDashing = true;

            try
            {
                // 1) 通常チェイス停止
                isChasing = false;
                agent.ResetPath();
                agent.isStopped = true;

                // Animator: 待機→直前で移動ONに切替
                bool prevAnimMove = false;
                if (animator != null)
                {
                    prevAnimMove = animator.GetBool(animatorBoolParam);
                    animator.SetBool(animatorBoolParam, false);
                }

                // 2) プレ待機（向きを合わせ続ける）
                if (dashPreWaitSeconds > 0f)
                {
                    float endTime = Time.time + dashPreWaitSeconds;
                    while (Time.time < endTime)
                    {
                        token.ThrowIfCancellationRequested();
                        if (player == null) player = GameObject.FindWithTag(playerTag);
                        if (player != null && dashFaceTarget)
                        {
                            Vector3 toPlayer = (player.transform.position - transform.position);
                            toPlayer.y = 0f;
                            if (toPlayer.sqrMagnitude > 0.0001f)
                                ApplyFacingAndAnimatorAngle(toPlayer.normalized);
                        }
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                }

                // 3) 初期方向
                Vector3 dir = (player.transform.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) return;
                dir.Normalize();

                float speed = dashSpeed * Mathf.Max(0.1f, homingSpeedMultiplier);
                float remaining = dashDistance;

                bool prevUpdatePos = agent != null ? agent.updatePosition : true;
                bool prevUpdateRot = agent != null ? agent.updateRotation : true;
                if (agent != null)
                {
                    agent.updatePosition = false;
                    agent.updateRotation = false;
                }
                if (animator != null) animator.SetBool(animatorBoolParam, true);

                try
                {
                    while (remaining > 0f)
                    {
                        token.ThrowIfCancellationRequested();

                        // ★ 追尾：現在のdirを、プレイヤー方向へ徐々に寄せる
                        Vector3 desired = player != null
                            ? (player.transform.position - transform.position)
                            : dir;
                        desired.y = 0f;
                        if (desired.sqrMagnitude > 0.0001f)
                        {
                            desired.Normalize();
                            // 時間依存の滑らかな係数（フレームレート非依存のイメージ）
                            float s = Mathf.Clamp01(homingTurnSharpness * Time.deltaTime);
                            dir = Vector3.Slerp(dir, desired, s);
                        }

                        if (dashFaceTarget) ApplyFacingAndAnimatorAngle(dir);

                        float step = speed * Time.deltaTime;
                        float moveLen = Mathf.Min(step, remaining);

                        Vector3 current = transform.position;
                        Vector3 candidate = current + dir * moveLen;

                        // 障害物チェック
                        if (NavMesh.Raycast(current, candidate, out var hitInfo, NavMesh.AllAreas))
                        {
                            Vector3 stopPos = hitInfo.position - dir * 0.05f;
                            if (NavMesh.SamplePosition(stopPos, out var snap, 0.2f, NavMesh.AllAreas))
                                stopPos = snap.position;
                            characterBrain.ForceSetPosition(stopPos);
                            break;
                        }

                        if (NavMesh.SamplePosition(candidate, out var sample, 0.2f, NavMesh.AllAreas))
                            candidate = sample.position;

                        characterBrain.ForceSetPosition(candidate);

                        remaining -= Vector3.Distance(current, transform.position);
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                }
                finally
                {
                    if (agent != null)
                    {
                        agent.Warp(agent.transform.position);
                        agent.updatePosition = prevUpdatePos;
                        agent.updateRotation = prevUpdateRot;
                    }
                    if (animator != null) animator.SetBool(animatorBoolParam, prevAnimMove);
                }
            }
            catch (OperationCanceledException)
            {
                isChasing = prevChasing;
                agent.isStopped = prevStopped;
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                isChasing = prevChasing;
                agent.isStopped = prevStopped;
            }
            finally
            {
                _isDashing = false;
            }
        }

        private async UniTask DashOnceZigZagAsync(CancellationToken token)
        {
            if (IsStoryMode) return;
            if (GameStateManager.Instance.CurrentState == GameState.GameOver) return;
            if (GameStateManager.Instance.CurrentState == GameState.InGameEvent) return;

            if (player == null)
                player = GameObject.FindWithTag(playerTag);
            if (player == null || agent == null) return;

            bool prevChasing = isChasing;
            bool prevStopped = agent.isStopped;

            _isDashing = true;

            try
            {
                // 1) 通常チェイス停止
                isChasing = false;
                agent.ResetPath();
                agent.isStopped = true;

                // Animator: 待機→直前で移動ONに切替
                bool prevAnimMove = false;
                if (animator != null)
                {
                    prevAnimMove = animator.GetBool(animatorBoolParam);
                    animator.SetBool(animatorBoolParam, false);
                }

                // 2) プレ待機（向きを合わせ続ける）
                if (dashPreWaitSeconds > 0f)
                {
                    float endTime = Time.time + dashPreWaitSeconds;
                    while (Time.time < endTime)
                    {
                        token.ThrowIfCancellationRequested();
                        if (player == null) player = GameObject.FindWithTag(playerTag);
                        if (player != null && dashFaceTarget)
                        {
                            Vector3 toPlayer = (player.transform.position - transform.position);
                            toPlayer.y = 0f;
                            if (toPlayer.sqrMagnitude > 0.0001f)
                                ApplyFacingAndAnimatorAngle(toPlayer.normalized);
                        }
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                }

                // 3) 基準方向
                Vector3 baseDir = (player.transform.position - transform.position);
                baseDir.y = 0f;
                if (baseDir.sqrMagnitude < 0.0001f) return;
                baseDir.Normalize();

                float speed = dashSpeed * Mathf.Max(0.1f, zigzagSpeedMultiplier);
                float remaining = dashDistance;

                bool prevUpdatePos = agent != null ? agent.updatePosition : true;
                bool prevUpdateRot = agent != null ? agent.updateRotation : true;
                if (agent != null)
                {
                    agent.updatePosition = false;
                    agent.updateRotation = false;
                }
                if (animator != null) animator.SetBool(animatorBoolParam, true);

                float elapsed = 0f;
                float maxAng = Mathf.Abs(zigzagMaxAngleDeg);
                float freq = Mathf.Max(0.1f, zigzagFrequencyHz);

                try
                {
                    while (remaining > 0f)
                    {
                        token.ThrowIfCancellationRequested();

                        elapsed += Time.deltaTime;
                        // ★ ジグザグ角度（-maxAng ～ +maxAng）
                        float ang = Mathf.Sin(elapsed * Mathf.PI * 2f * freq) * maxAng;
                        Vector3 dir = Quaternion.Euler(0f, ang, 0f) * baseDir;

                        if (dashFaceTarget) ApplyFacingAndAnimatorAngle(dir);

                        float step = speed * Time.deltaTime;
                        float moveLen = Mathf.Min(step, remaining);

                        Vector3 current = transform.position;
                        Vector3 candidate = current + dir * moveLen;

                        // 障害物チェック
                        if (NavMesh.Raycast(current, candidate, out var hitInfo, NavMesh.AllAreas))
                        {
                            Vector3 stopPos = hitInfo.position - dir * 0.05f;
                            if (NavMesh.SamplePosition(stopPos, out var snap, 0.2f, NavMesh.AllAreas))
                                stopPos = snap.position;
                            characterBrain.ForceSetPosition(stopPos);
                            break;
                        }

                        if (NavMesh.SamplePosition(candidate, out var sample, 0.2f, NavMesh.AllAreas))
                            candidate = sample.position;

                        characterBrain.ForceSetPosition(candidate);

                        remaining -= Vector3.Distance(current, transform.position);
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                }
                finally
                {
                    if (agent != null)
                    {
                        agent.Warp(agent.transform.position);
                        agent.updatePosition = prevUpdatePos;
                        agent.updateRotation = prevUpdateRot;
                    }
                    if (animator != null) animator.SetBool(animatorBoolParam, prevAnimMove);
                }
            }
            catch (OperationCanceledException)
            {
                isChasing = prevChasing;
                agent.isStopped = prevStopped;
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                isChasing = prevChasing;
                agent.isStopped = prevStopped;
            }
            finally
            {
                _isDashing = false;
            }
        }

        private void UpdateChaseFacing()
        {
            if (animator == null) return;

            // 1) 進行意図の方向を決める：優先は agent.desiredVelocity
            Vector3 dir = Vector3.zero;
            if (agent != null)
            {
                var v = agent.desiredVelocity; // 目標速度（NavMeshが出す進行方向）
                v.y = 0f;
                if (v.sqrMagnitude > 0.0001f) dir = v.normalized;
            }

            // 2) ほぼ停止/コーナー手前などで desiredVelocity が出ない場合はプレイヤー方向
            if (dir == Vector3.zero && player != null)
            {
                dir = (player.transform.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
            }

            if (dir == Vector3.zero) return;

            // 3) スムーズに回頭（フレームレート非依存気味）
            if (chaseTurnSharpness > 0f)
            {
                // 現在の forward → 目標dir へ少しずつ
                Vector3 current = transform.forward;
                current.y = 0f;
                if (current.sqrMagnitude > 0.0001f)
                    dir = Vector3.Slerp(current.normalized, dir, Mathf.Clamp01(chaseTurnSharpness * Time.deltaTime));
            }

            // 4) root回頭 & Animator Angle 更新（ダッシュ時と同じメソッドで統一）
            ApplyFacingAndAnimatorAngle(dir);
        }

    }
}
