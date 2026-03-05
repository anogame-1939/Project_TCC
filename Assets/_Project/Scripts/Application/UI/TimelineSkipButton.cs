using UniRx;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using AnoGame.AnoFlow;
using VContainer;
using AnoGame.Application.Input;

namespace AnoGame.Application.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TimelineSkipButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private float _cooldown = 1.0f;
        [SerializeField] private float _longPressDuration = 1.0f;
        [SerializeField] private Image _skipProgressImage;

        private CanvasGroup _canvasGroup;
        private CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        private IInputActionProvider _inputProvider;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            if (_button == null)
            {
                _button = GetComponentInChildren<Button>();
            }
        }

        private void OnEnable()
        {
            // メッセージ監視: スキップ可能状態なら表示、それ以外は非表示
            MessageBroker.Default
                .Receive<TimelineQueueManager.TimelineSkipAvailabilityChanged>()
                .Subscribe(x =>
                {
                    if (x.IsAvailable) Show();
                    else Hide();
                })
                .AddTo(_disposables);

            // ボタンクリック: クールタイム付きで実行
            if (_button != null)
            {
                _button.OnClickAsObservable()
                    .ThrottleFirst(System.TimeSpan.FromSeconds(_cooldown))
                    .Subscribe(_ =>
                    {
                        TimelineQueueManager.Instance.SkipCurrentSequence();
                    })
                    .AddTo(_disposables);
            }

            // Confirm長押しによるスキップ
            if (_inputProvider != null)
            {
                var uiMap = _inputProvider.GetUIActionMap();
                var confirmAction = uiMap?.FindAction("Confirm");

                if (confirmAction != null)
                {
                    float currentPressTime = 0f;
                    bool hasSkipped = false;

                    Observable.EveryUpdate()
                        .Subscribe(_ =>
                        {
                            bool isPressed = confirmAction.IsPressed();

                            if (isPressed && !hasSkipped)
                            {
                                currentPressTime += Time.unscaledDeltaTime;
                                if (_skipProgressImage != null)
                                {
                                    _skipProgressImage.fillAmount = Mathf.Clamp01(currentPressTime / _longPressDuration);
                                }

                                if (currentPressTime >= _longPressDuration)
                                {
                                    TimelineQueueManager.Instance.SkipCurrentSequence();
                                    hasSkipped = true;
                                    // スキップ確定後は見た目をリセットしておく（あるいは次回表示まで維持でも良いが、ここではリセット）
                                    if (_skipProgressImage != null) _skipProgressImage.fillAmount = 0f;
                                }
                            }
                            else
                            {
                                // 離した、あるいはスキップ完了後
                                if (!isPressed)
                                {
                                    currentPressTime = 0f;
                                    hasSkipped = false;
                                    if (_skipProgressImage != null) _skipProgressImage.fillAmount = 0f;
                                }
                            }
                        })
                        .AddTo(_disposables);
                }
            }
        }

        private void OnDisable()
        {
            _disposables.Clear();
        }

        private void Show()
        {
            _canvasGroup.DOKill();
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true); // タイムスケール0でも動くように念のため
        }

        private void Hide()
        {
            _canvasGroup.DOKill();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.DOFade(0f, _fadeDuration).SetUpdate(true);
        }
    }
}
