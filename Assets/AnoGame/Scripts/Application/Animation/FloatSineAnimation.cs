using UnityEngine;
using DG.Tweening;

namespace AnoGame.Application.Animation
{
    public class FloatSineAnimation : MonoBehaviour
    {
        [Header("Float Settings")]
        [SerializeField] private float amplitude = 0.5f; // 振幅
        [SerializeField] private float frequency = 1f;   // 周波数（1秒あたりの周期数）
        
        private Vector3 startPosition;
        private Tween floatTween;
        
        private void Start()
        {
            startPosition = transform.position;
            StartFloating();
        }
        
        private void StartFloating()
        {
            if (floatTween != null)
            {
                floatTween.Kill();
            }

            // 正弦波による連続的な動きを実現
            // -PI/2から開始することで、Sin波が0から上昇を開始する
            floatTween = DOTween.To(
                () => -Mathf.PI / 2f, // 開始位置を-PI/2に変更
                value =>
                {
                    float yOffset = amplitude * Mathf.Sin(value);
                    transform.position = new Vector3(
                        startPosition.x,
                        startPosition.y + yOffset,
                        startPosition.z
                    );
                },
                3f * Mathf.PI / 2f, // -PI/2から3PI/2まで動かす（1周期分）
                1f / frequency
            )
            .SetEase(Ease.Linear)
            .SetLoops(-1);
        }

        private void OnDisable()
        {
            if (floatTween != null)
            {
                floatTween.Kill();
                transform.position = startPosition;
            }
        }

        private void OnDestroy()
        {
            if (floatTween != null)
            {
                floatTween.Kill();
            }
        }

        private void OnValidate()
        {
            frequency = Mathf.Max(0.1f, frequency);
        }
    }
}