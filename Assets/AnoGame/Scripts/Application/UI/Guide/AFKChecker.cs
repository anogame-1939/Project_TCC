using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Unity.TinyCharacterController.Control; // MoveControl

namespace AnoGame.UI.Guide
{
    public sealed class AFKChecker : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private MoveControl moveControl;
        [SerializeField] private GuideUISwitcher guide;

        [Header("AFK 判定")]
        [SerializeField, Tooltip("この速度未満が続いたらAFK扱い")]
        private float speedThreshold = 0.00f;
        [SerializeField, Tooltip("AFKと見なすまでの継続秒数")]
        private float afkSeconds = 15f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Events")]
        public UnityEvent OnBecomeAFK;
        public UnityEvent OnReturnFromAFK;
        public UnityEvent<bool> OnAFKStateChanged;

        public bool IsAFK { get; private set; }
        public float InactiveElapsed { get; private set; }

        private CancellationTokenSource _cts;

        private void Reset()
        {
            moveControl = FindObjectOfType<MoveControl>();
            guide = FindObjectOfType<GuideUISwitcher>();
        }

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();
            MonitorLoopAsync(_cts.Token).Forget();
        }

        private void OnDisable()
        {
            _cts?.Cancel(); _cts?.Dispose(); _cts = null;
        }

        public void MarkActive()
        {
            InactiveElapsed = 0f;
            if (IsAFK)
            {
                SetAFK(false);
                guide?.Hide();
            }
        }

        private async UniTask MonitorLoopAsync(CancellationToken ct)
        {
            if (moveControl == null)
            {
                moveControl = FindObjectOfType<MoveControl>();
                if (moveControl == null)
                {
                    Debug.LogError("[AFKChecker] MoveControl が見つかりません。");
                    enabled = false; return;
                }
            }

            IsAFK = false;
            InactiveElapsed = 0f;

            while (!ct.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float speed = moveControl.CurrentSpeed;
                bool isMoving = speed > speedThreshold;

                if (isMoving)
                {
                    if (IsAFK)
                    {
                        SetAFK(false);
                        guide?.Hide();
                    }
                    InactiveElapsed = 0f;
                }
                else
                {
                    InactiveElapsed += dt;
                    if (!IsAFK && InactiveElapsed >= afkSeconds)
                    {
                        SetAFK(true);
                        guide?.Show();
                    }
                }
            }
        }

        private void SetAFK(bool afk)
        {
            if (IsAFK == afk) return;
            IsAFK = afk;
            OnAFKStateChanged?.Invoke(afk);
            if (afk) OnBecomeAFK?.Invoke();
            else OnReturnFromAFK?.Invoke();
        }
    }
}
