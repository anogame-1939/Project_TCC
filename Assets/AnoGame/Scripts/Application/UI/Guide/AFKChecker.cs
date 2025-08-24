using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Unity.TinyCharacterController.Control; // MoveControl

namespace AnoGame.UI.Guide
{
    /// <summary>
    /// MoveControl.CurrentSpeed を監視してAFK判定する
    /// </summary>
    public class AFKChecker : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private MoveControl moveControl;         // プレイヤーに付いている MoveControl
        [SerializeField] private GuideUISwitcher guideUISwitcher; // ガイド制御

        [Header("AFK 判定")]
        [SerializeField, Tooltip("この速度未満が続いたらAFK扱い")]
        private float speedThreshold = 0.00f;
        [SerializeField, Tooltip("AFKと見なすまでの継続秒数")]
        private float afkSeconds = 15f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Events")]
        public UnityEvent OnBecomeAFK;
        public UnityEvent OnReturnFromAFK;
        public UnityEvent<bool> OnAFKStateChanged; // 引数: true=AFK, false=復帰

        public bool IsAFK { get; private set; }
        public float InactiveElapsed { get; private set; }

        private CancellationTokenSource _cts;

        private void Reset()
        {
            moveControl = FindObjectOfType<MoveControl>();
            if (guideUISwitcher == null)
                guideUISwitcher = FindObjectOfType<GuideUISwitcher>();
        }

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();
            MonitorLoopAsync(_cts.Token).Forget();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// 外部から「何か操作があった」と知らせてAFK解除/タイマーリセット
        /// </summary>
        public void MarkActive()
        {
            InactiveElapsed = 0f;
            if (IsAFK)
            {
                SetAFK(false);
                guideUISwitcher?.HideSticky(); // ★ 解除でフェードアウト
            }
        }

        private async UniTask MonitorLoopAsync(CancellationToken ct)
        {
            if (moveControl == null)
            {
                moveControl = FindObjectOfType<MoveControl>();
                if (moveControl == null)
                {
                    Debug.LogError("[AFKChecker] MoveControl を見つけられません。");
                    enabled = false;
                    return;
                }
            }

            InactiveElapsed = 0f;
            IsAFK = false;

            while (!ct.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                // MoveControl.CurrentSpeed は MovePriority の影響を反映した「実効速度」
                float speed = moveControl.CurrentSpeed;
                bool isMoving = speed > speedThreshold;

                if (isMoving)
                {
                    if (IsAFK)
                    {
                        SetAFK(false);
                        guideUISwitcher?.HideSticky(); // ★ 解除でフェードアウト
                    }
                    InactiveElapsed = 0f;
                }
                else
                {
                    InactiveElapsed += dt;
                    if (!IsAFK && InactiveElapsed >= afkSeconds)
                    {
                        SetAFK(true);
                        guideUISwitcher?.ShowSticky(); // ★ AFK中は表示しっぱなし
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
