using UnityEngine;
using DG.Tweening;
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Player.Control
{
    public class PlayerForcedTransformMover : MonoBehaviour
    {
        [SerializeField] private float moveDuration = 1.0f;      // 移動にかかる時間
        [SerializeField] private Ease moveEase = Ease.InOutQuad; // DOTweenのイージング

        private CharacterBrain characterBrain;
        private Animator animator;

        private void Awake()
        {
            // CharacterBrainを取得（あれば）
            characterBrain = GetComponent<CharacterBrain>();
            if (characterBrain == null)
            {
                Debug.LogWarning("CharacterBrainが見つかりません。");
            }

            // 子オブジェクトを探索して最初に見つかったAnimatorを取得
            animator = FindChildAnimator(transform);
            if (animator == null)
            {
                Debug.LogWarning("子オブジェクトにAnimatorが見つかりません。");
            }
        }

        /// <summary>
        /// 子階層をすべて探索して、最初に見つかったAnimatorを返すメソッド
        /// </summary>
        private Animator FindChildAnimator(Transform parent)
        {
            // 直接の子階層をループ
            foreach (Transform child in parent)
            {
                // まずは子オブジェクトにAnimatorがないかチェック
                Animator anim = child.GetComponent<Animator>();
                if (anim != null)
                {
                    return anim;
                }
                // 孫以降の階層も再帰的に探索
                Animator foundInSubChild = FindChildAnimator(child);
                if (foundInSubChild != null)
                {
                    return foundInSubChild;
                }
            }
            // どこにもAnimatorがなかった場合はnull
            return null;
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

            if (targetTransform == null)
            {
                Debug.LogError("targetTransformが設定されていません。");
                return;
            }

            // ▼ ここからアニメーターのパラメーター制御 ▼
            if (animator != null)
            {
                // ターゲットまでの水平ベクトルを求める
                Vector3 directionAnim = targetTransform.position - transform.position;
                directionAnim.y = 0f; // 垂直方向は無視

                // Angleパラメーターを8方向にスナップした角度として設定
                if (directionAnim.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(directionAnim.x, directionAnim.z) * Mathf.Rad2Deg;
                    float snappedAngle = Mathf.Round(angle / 45f) * 45f;
                    animator.SetFloat("Angle", snappedAngle);

                    Debug.Log($"Angle...angle: {angle}");
                    Debug.Log($"Angle...snappedAngle: {snappedAngle}");
                }
                else
                {
                    // ターゲット位置がほぼ同じなら角度0とする
                    animator.SetFloat("Angle", 0f);
                }


                Debug.Log($"Angle: {animator.GetFloat("Angle")}");

                // IsMoveフラグをtrueにして移動アニメーションを再生させる
                animator.SetBool("IsMove", true);

                Debug.Log($"animator.GetFloat(IsMove): {animator.GetFloat("IsMove")}");

                transform.rotation = Quaternion.LookRotation(directionAnim.normalized, Vector3.up);
            }
            // ▲ ここまでアニメーターのパラメーター制御 ▲

            Vector3 direction = targetTransform.position;
            direction.y = transform.position.y;
            // ▼ DOTweenでターゲット位置へ移動 ▼
            transform.DOMove(direction, moveDuration)
                .SetEase(moveEase)
                .OnComplete(() =>
                {
                    // 移動完了後にBrainを再有効化して通常操作に戻す
                    if (characterBrain != null)
                    {
                        characterBrain.enabled = true;
                    }

                    // アニメーター側のIsMoveフラグをfalseに戻してIdle等へ遷移
                    if (animator != null)
                    {
                        animator.SetBool("IsMove", false);
                    }
                });
        }
    }
}
