using UnityEngine;
using UnityEngine.AI;

namespace AnoGame.Application.Enemy.Animation
{
    /// <summary>
    /// アニメ用のIsMove/LocomotionSpeedを更新するだけの軽量スクリプト。
    /// 速度ソース優先度:
    ///   NavMeshAgent -> Rigidbody -> Transform差分
    /// </summary>
    [AddComponentMenu("AnoGame/Animation/" + nameof(BrainSpeedToIsMove))]
    public sealed class BrainSpeedToIsMove : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private Animator animator;                       // 未指定なら自動取得
        [SerializeField] private NavMeshAgent agent;                      // メインの速度参照元
        [SerializeField] private Rigidbody rigidBody;                     // 任意

        [Header("Animator パラメータ")]
        [SerializeField] private string isMoveBoolParam = "IsMove";
        [SerializeField] private string locomotionSpeedParam = "LocomotionSpeed"; // 空なら更新しない

        [Header("判定/平滑化")]
        [SerializeField, Min(0f)] private float speedThreshold = 0.05f;   // これ超えたら移動扱い
        [SerializeField, Min(0f)] private float emaHalfLife = 0.10f;      // 速度EMA（0で生値）
        [SerializeField] private bool planarOnly = true;                   // Y成分を除外して速度算出

        [Header("更新タイミング")]
        [SerializeField] private bool useFixedUpdate = false;              // 物理同期したいならON

        private Vector3 _lastPos;
        private bool _hasLastPos;
        private float _emaSpeed;
        private bool _paused;

        void Reset()
        {
            animator ??= GetComponentInChildren<Animator>();
            agent ??= GetComponentInChildren<NavMeshAgent>();
            rigidBody ??= GetComponentInChildren<Rigidbody>();
        }

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (agent == null) agent = GetComponentInChildren<NavMeshAgent>();
            if (rigidBody == null) rigidBody = GetComponentInChildren<Rigidbody>();
        }

        void OnEnable()
        {
            _hasLastPos = false;
            _emaSpeed = 0f;
            _hasLastPos = false; // Reset again just to be sure if paused/resumed
            _lastPos = transform.position; // Initial position
        }

        void Update()
        {
            if (!useFixedUpdate) Tick(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (useFixedUpdate) Tick(Time.fixedDeltaTime);
        }

        private void Tick(float dt)
        {
            if (animator == null) return;

            float raw = GetSpeed(dt);
            float smooth = Smooth(raw, dt);

            bool isMoving = smooth > speedThreshold;
            animator.SetBool(isMoveBoolParam, isMoving);

            if (!string.IsNullOrEmpty(locomotionSpeedParam))
                animator.SetFloat(locomotionSpeedParam, smooth);
        }

        private float GetSpeed(float dt)
        {
            if (_paused) return 0f;

            // 1) NavMeshAgent (Primary Source)
            if (agent != null && agent.isActiveAndEnabled)
            {
                var v = agent.velocity;
                if (planarOnly) v.y = 0f;
                return v.magnitude;
            }

            // 2) Rigidbody
            if (rigidBody != null)
            {
                var v = rigidBody.velocity;
                if (planarOnly) v.y = 0f;
                return v.magnitude;
            }

            // 3) Transform 差分 (Fallback)
            if (!_hasLastPos)
            {
                _lastPos = transform.position;
                _hasLastPos = true;
                return 0f;
            }
            Vector3 now = transform.position;
            Vector3 delta = now - _lastPos;
            if (planarOnly) delta.y = 0f;
            _lastPos = now;
            return delta.magnitude / Mathf.Max(dt, 1e-5f);
        }

        private float Smooth(float raw, float dt)
        {
            if (emaHalfLife <= 0f) return raw;
            float alpha = 1f - Mathf.Exp(-Mathf.Log(2f) * dt / Mathf.Max(emaHalfLife, 1e-5f));
            _emaSpeed = Mathf.Lerp(_emaSpeed, raw, alpha);
            return _emaSpeed;
        }

        // --- 外部制御（任意） ---
        public void Pause() => _paused = true;   // EventLockで手動制御したい時に
        public void Resume() => _paused = false;
        public void ForceSetIsMove(bool v) { if (animator) animator.SetBool(isMoveBoolParam, v); }
    }
}
