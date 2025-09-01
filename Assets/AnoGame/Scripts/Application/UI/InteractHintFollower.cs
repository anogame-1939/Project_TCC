using UnityEngine;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// 指定タグ(既定: "Player")のTransformを見つけ、その位置にUIを追従表示する。
    /// - CanvasのRender Modeを問わず動作
    /// - ワールド/スクリーン両方のオフセット調整
    /// - 画面外・背面時の非表示、端へのクランプ(任意)
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class InteractHintFollower : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private Transform target;            // 見つからない場合はStartでtag検索

        [Header("UI & Canvas")]
        [SerializeField] private RectTransform ui;            // 追従させたいUI
        [SerializeField] private Canvas canvas;               // 配置されているCanvas
        [SerializeField] private Camera targetCamera;         // Overlay以外では必須(未設定時はCamera.main)

        [Header("Offsets")]
        [Tooltip("ターゲット(ワールド)からのローカルオフセット")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 1.6f, 0);
        [Tooltip("スクリーン座標での微調整(アンカー基準)")]
        [SerializeField] private Vector2 screenOffset = new Vector2(0, 0);

        [Header("Visibility & Clamp")]
        [SerializeField] private bool hideWhenBehindCamera = true;
        [SerializeField] private bool hideWhenOffScreen = false;
        [SerializeField] private bool clampInsideCanvas = true;

        [Header("Smoothing")]
        [SerializeField, Range(0f, 1f)] private float smoothTime = 0.15f; // 0で即時追従

        // 内部
        private Vector2 _velocity; // SmoothDamp用(スクリーン座標)
        private RectTransform _canvasRect;

        private void Reset()
        {
            canvas = GetComponentInParent<Canvas>();
            ui = GetComponent<RectTransform>();
        }

        private void Awake()
        {
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            _canvasRect = canvas ? canvas.transform as RectTransform : null;
            if (!targetCamera) targetCamera = Camera.main;
        }

        private void Start()
        {
            if (!target && !string.IsNullOrEmpty(targetTag))
            {
                var go = GameObject.FindGameObjectWithTag(targetTag);
                if (go) target = go.transform;
            }

            if (ui && !ui.gameObject.activeSelf)
                ui.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!ui || !canvas || !target) return;
            if (!targetCamera) targetCamera = Camera.main;

            Vector3 worldPos = target.position + worldOffset;
            Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

            // 背面処理
            if (hideWhenBehindCamera && screenPos.z < 0f)
            {
                SetUIActive(false);
                return;
            }

            // Canvas座標へ変換
            Vector2 canvasPoint;
            bool gotPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCamera, out canvasPoint);

            if (!gotPoint) return;

            // スクリーン微調整(アンカー基準)
            canvasPoint += screenOffset;

            // クランプ or 画面外で非表示
            if (_canvasRect)
            {
                Vector2 half = _canvasRect.rect.size * 0.5f;
                Vector2 clamped = canvasPoint;

                if (clampInsideCanvas)
                {
                    clamped.x = Mathf.Clamp(clamped.x, -half.x, half.x);
                    clamped.y = Mathf.Clamp(clamped.y, -half.y, half.y);
                }

                bool off =
                    (canvasPoint.x < -half.x || canvasPoint.x > half.x ||
                     canvasPoint.y < -half.y || canvasPoint.y > half.y);

                if (hideWhenOffScreen && off)
                {
                    SetUIActive(false);
                    return;
                }

                // スムージング
                if (smoothTime > 0f)
                {
                    Vector2 current = ui.anchoredPosition;
                    ui.anchoredPosition = new Vector2(
                        Mathf.SmoothDamp(current.x, clamped.x, ref _velocity.x, smoothTime),
                        Mathf.SmoothDamp(current.y, clamped.y, ref _velocity.y, smoothTime));
                }
                else
                {
                    ui.anchoredPosition = clamped;
                }
            }

            SetUIActive(true);
        }

        private void SetUIActive(bool active)
        {
            if (ui.gameObject.activeSelf != active)
                ui.gameObject.SetActive(active);
        }

        /// <summary>外部からターゲットを差し替えたい場合に使用</summary>
        public void SetTarget(Transform t) => target = t;

        /// <summary>ターゲットをタグから再検索</summary>
        public void RefreshTargetByTag()
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;
        }
    }
}
