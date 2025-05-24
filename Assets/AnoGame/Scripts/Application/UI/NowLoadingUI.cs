using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AnoGame.Application.Core;

namespace AnoGame.Application.UI
{
    public class NowLoadingUI : SingletonMonoBehaviour<NowLoadingUI>
    {
        [SerializeField]
        private Image _loadingImage;

        // アニメーションの参照を保持
        private Sequence _loadingSequence;
        // 初期アンカー位置を保持
        private Vector2 _initialAnchoredPosition;

        [Header("アニメーション設定")]
        [SerializeField]
        // 移動量・時間・待機時間を調整
        [Tooltip("右に移動する量（px）")]
        [Range(0f, 500f)]
        private float moveDistance = 200f;   // 右に移動する量（px）
        [SerializeField]
        [Tooltip("移動にかける時間（秒）")]
        private float moveDuration = 2f;     // 移動にかける時間（秒）
        [SerializeField]
        [Tooltip("右端で待つ時間（秒）")]
        private float waitDuration = 1f;     // 右端で待つ時間（秒）

        protected void Awake()
        {
            base.Awake();
            // 最初にアンカー位置をキャッシュ
            _initialAnchoredPosition = _loadingImage.rectTransform.anchoredPosition;
        }

        private void Start()
        {
            // HideLoadingImage();
        }

        // ロード中イメージを表示＆アニメ開始
        public void ShowLoadingImage()
        {
            Debug.Log("NowLoadingUI.ShowLoadingImage()");

            // 既存シーケンスがあれば停止・破棄
            if (_loadingSequence != null && _loadingSequence.IsActive())
                _loadingSequence.Kill();

            _loadingImage.gameObject.SetActive(true);

            // 色と位置を初期状態にリセット
            _loadingImage.color = new Color(1f, 1f, 1f, 1f);
            _loadingImage.rectTransform.anchoredPosition = _initialAnchoredPosition;



            // シーケンス生成
            _loadingSequence = DOTween.Sequence()
                // 1) 右へ移動（相対）
                .Append(_loadingImage.rectTransform
                    .DOAnchorPosX(moveDistance, moveDuration)
                    .SetRelative()
                    .SetEase(Ease.Linear))
                // 2) 右端で少し待機
                .AppendInterval(waitDuration)
                // 3) 開始点へ瞬間ジャンプ戻り（相対）
                .Append(_loadingImage.rectTransform
                    .DOAnchorPosX(-moveDistance, 0f)
                    .SetRelative())
                // 4) この一連を無限ループ
                .SetLoops(-1, LoopType.Restart);
        }

        // ロード中イメージを非表示＆アニメ停止
        public void HideLoadingImage()
        {
            Debug.Log("NowLoadingUI.HideLoadingImage()");

            if (_loadingSequence != null && _loadingSequence.IsActive())
            {
                _loadingSequence.Kill();
            }

            _loadingImage.gameObject.SetActive(false);
        }
    }
}
