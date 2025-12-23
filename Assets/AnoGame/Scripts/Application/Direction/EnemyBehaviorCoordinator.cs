
using UnityEngine;
using AnoGame.Application.Enemy.AI;


namespace AnoGame.Application.Direction
{
    // ===================================
    // 敵 AI の挙動切替をまとめて管理する Director
    //  - 各 IntentProvider の Activate/Deactivate を呼ぶだけ
    //  - Timeline からの呼び出しも想定
    // ===================================
    [RequireComponent(typeof(MovementIntentRouter))]
    [ComponentDescription("敵 AI の挙動切替をまとめて管理する Director\n - 各 IntentProvider の Activate/Deactivate を呼ぶだけ\n - Timeline からの呼び出しも想定")]
    public sealed class EnemyBehaviorCoordinator : MonoBehaviour
    {
        [SerializeField] EventLockIntentProvider eventLock;
        [SerializeField] ChaseIntentProvider chase;
        [SerializeField] InvestigateIntentProvider investigate;
        [SerializeField] ReturnToAnchorIntentProvider ret;
        [SerializeField] PatrolSplineIntentProvider patrol;
        [SerializeField] SuspicionIntentProvider suspicion; // [NEW]

        [Header("Events")]
        [SerializeField] public UnityEngine.Events.UnityEvent OnChaseStart;
        [SerializeField] public UnityEngine.Events.UnityEvent OnChaseEnd;
        [SerializeField] public UnityEngine.Events.UnityEvent OnSuspicion;
        [SerializeField] public UnityEngine.Events.UnityEvent OnSuspicionEnd;

        [Header("Defaults")]
        [SerializeField] float defaultEncounterTTL = 2.0f;
        [SerializeField] float defaultInvestigateTTL = 3.0f;
        [SerializeField] float defaultSuspicionTTL = 2.0f; // [NEW]

        private enum State { Patrol, Chase, Investigate, Suspicion }
        private State _currentState = State.Patrol;
        private bool _isInputBlocked = false; // [NEW] 外部入力ブロック用

        public bool IsChasing => _currentState == State.Chase; // [NEW] 外部確認用
        public bool IsSuspicion => _currentState == State.Suspicion;


        void Awake()
        {
            // Investigate 失敗→帰投開始
            investigate.OnExpired += lastSeen =>
            {
                Debug.Log("見失った！");
                // まずChaseは止めておく（安全）
                if (_currentState == State.Chase) OnChaseEnd?.Invoke();
                chase.SetActive(false);
                // 帰投を開始（最近傍のKnotへ）
                ret.ActivateToNearest();   // ★ ここが Deactivate ではなく Activate
                                           // 巡回は常時ONでも良いが、明示的にONにしておくと安心
                patrol.SetActive(true);

                _currentState = State.Patrol;
            };

            // Suspicion 完了 -> Investigate
            suspicion.OnExpired += () =>
            {
                Debug.Log($"[Coordinator] Suspicion expired. Transitioning to Investigate. (Time: {Time.time})");
                OnSuspicionEnd?.Invoke();
                Debug.Log("疑念晴れず -> 捜索開始");
                _currentState = State.Investigate;
                investigate.Activate(transform.position, defaultInvestigateTTL);
            };

            // [NEW] 自動登録: Routerに登録されていない可能性が高いため念のため登録
            var router = GetComponent<MovementIntentRouter>();
            if (router != null)
            {
                router.AddProvider(suspicion);
            }
        }

        // === Timeline から呼ぶ（SignalReceiver の UnityEvent 1本でOK） ===
        public void BeginEncounter(Transform focus) =>
            eventLock.Activate(focus, defaultEncounterTTL);

        public void EndEncounterToChase()
        {
            eventLock.Deactivate();       // 内部で NavMesh 再開＋ELock解除
            chase.SetActive(true);
            _currentState = State.Chase;
            OnChaseStart?.Invoke();
        }

        // === ゲーム側（Perception等）から呼ぶ ===
        public void NotifyLost(Vector3 lastSeen)
        {
            if (_isInputBlocked) return;
            if (_currentState == State.Investigate) return;
            if (_currentState == State.Suspicion) return;

            Debug.Log("見失った！疑惑モードへ移行");
            if (_currentState == State.Chase) OnChaseEnd?.Invoke();
            _currentState = State.Suspicion;
            OnSuspicion?.Invoke();

            chase.SetActive(false);
            ret.Deactivate();
            investigate.Deactivate();

            // まずはその場(あるいはlastSeen)を注視
            suspicion.Activate(lastSeen, defaultSuspicionTTL);
        }


        public void NotifyFound()
        {
            if (_isInputBlocked) return;
            if (_currentState == State.Chase) return;

            Debug.Log("発見！追跡開始");
            if (_currentState == State.Suspicion) OnSuspicionEnd?.Invoke();
            _currentState = State.Chase;
            OnChaseStart?.Invoke();

            investigate.Deactivate();
            suspicion.Deactivate(); // [NEW]
            ret.Deactivate();
            chase.SetActive(true);                  // 再露見＝追跡へ
        }

        public void NotifyNoiseHeard(Vector3 noisePosition)
        {
            if (_isInputBlocked) return;
            if (_currentState == State.Chase) return;

            Debug.Log($"音を感知！ {noisePosition} を調査しに行きます");
            if (_currentState == State.Suspicion) OnSuspicionEnd?.Invoke();
            _currentState = State.Investigate;

            chase.SetActive(false);
            suspicion.Deactivate(); // [NEW]
            ret.Deactivate();
            investigate.Activate(noisePosition, defaultInvestigateTTL);
        }

        // 任意：強制中断
        public void AbortAll()
        {
            _isInputBlocked = false; // Abort時はブロック解除
            eventLock.Deactivate();
            investigate.Deactivate();
            if (_currentState == State.Suspicion) OnSuspicionEnd?.Invoke();
            suspicion.Deactivate(); // [NEW]
            ret.Deactivate();
            if (_currentState == State.Chase) OnChaseEnd?.Invoke();
            chase.SetActive(false);
            _currentState = State.Patrol;
        }
        // [NEW] 外部入力を一時的にブロックする
        public void SetInputBlocked(bool blocked)
        {
            _isInputBlocked = blocked;
        }
    }
}
