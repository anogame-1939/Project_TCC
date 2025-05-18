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
            {
                _loadingSequence.Kill();
            }

            _loadingImage.gameObject.SetActive(true);

            // 色と位置を初期状態にリセット
            _loadingImage.color = new Color(1f, 1f, 1f, 1f);
            _loadingImage.rectTransform.anchoredPosition = _initialAnchoredPosition;

            // シーケンス生成
            _loadingSequence = DOTween.Sequence()
                // フェードアウト → フェードイン
                .Append(_loadingImage.DOFade(0f, 0.5f))
                .Append(_loadingImage.DOFade(1f, 0.5f))
                // 相対移動で上方向に30px → 自動でYoyoリターン
                .Join(_loadingImage.rectTransform
                    .DOAnchorPosY(30f, 0.5f)
                    .SetRelative(true)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine))
                // 無限ループで全体を繰り返し
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
