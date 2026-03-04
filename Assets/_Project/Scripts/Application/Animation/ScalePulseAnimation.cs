using UnityEngine;
using DG.Tweening;

namespace AnoGame.Application.Animation
{
    public class ScalePulseAnimation : MonoBehaviour
    {
        [Header("Scale Settings")]
        [SerializeField] private Vector3 maxScale = new Vector3(1.2f, 1.2f, 1.2f);
        [SerializeField] private Vector3 minScale = new Vector3(0.8f, 0.8f, 0.8f);
        [SerializeField] private float scaleDuration = 1.0f;
        [SerializeField] private Ease scaleEase = Ease.InOutQuad;
        
        private Sequence scaleSequence;
        
        private void Start()
        {
            // 初期化時にシーケンスを作成
            CreateScaleSequence();
        }
        
        private void CreateScaleSequence()
        {
            // 既存のシーケンスがあれば停止して破棄
            if (scaleSequence != null)
            {
                scaleSequence.Kill();
            }
            
            // 新しいシーケンスを作成
            scaleSequence = DOTween.Sequence();
            
            // 拡大縮小のシーケンスを設定
            scaleSequence
                .Append(transform.DOScale(maxScale, scaleDuration).SetEase(scaleEase))
                .Append(transform.DOScale(minScale, scaleDuration).SetEase(scaleEase))
                .SetLoops(-1); // -1で無限ループ
        }
        
        private void OnDisable()
        {
            // コンポーネントが無効になった時にシーケンスを停止
            if (scaleSequence != null)
            {
                scaleSequence.Kill();
            }
        }
    }
}