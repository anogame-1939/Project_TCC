using UnityEngine;
using DG.Tweening;
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Player.Control
{
    public class PlayerForcedTransformMover : MonoBehaviour
    {
        [SerializeField] private float moveDuration = 1.0f;     // 移動にかかる時間
        [SerializeField] private Ease moveEase = Ease.InOutQuad;  // DOTweenのイージング

        private CharacterBrain characterBrain;

        private void Awake()
        {
            // CharacterBrainコンポーネントを取得（あれば）
            characterBrain = GetComponent<CharacterBrain>();
            if (characterBrain == null)
            {
                Debug.LogWarning("CharacterBrainが見つかりません。");
            }
        }

        /// <summary>
        /// 強制移動を実行するメソッド
        /// </summary>
        /// <param name="targetTransform">移動先のTransform</param>
        public void ForceMove(Transform targetTransform)
        {
            // Brainを無効化して、通常の入力や動作を止める
            if (characterBrain != null)
            {
                characterBrain.enabled = false;
            }

            if (targetTransform != null)
            {
                // 例：Y軸は現状の値を維持し、水平方向のみ移動する
                Vector3 destination = targetTransform.position;
                destination.y = transform.position.y;

                // DOTweenで移動アニメーションを実行
                transform.DOMove(destination, moveDuration)
                    .SetEase(moveEase)
                    .OnComplete(() =>
                    {
                        // 移動完了後にBrainを再有効化して通常操作に戻す
                        if (characterBrain != null)
                        {
                            characterBrain.enabled = true;
                        }
                    });
            }
            else
            {
                Debug.LogError("targetTransformが設定されていません。");
            }
        }
    }
}
