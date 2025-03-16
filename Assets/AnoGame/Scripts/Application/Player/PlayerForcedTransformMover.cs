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

        // ==== デバッグ用フィールド ====
        [Header("▼ デバッグ用：回転・アングル適用テスト")]
        [SerializeField] private bool applyTransformRotation;       // transform.rotationを適用するか？
        [SerializeField] private Vector3 debugTransformRotation;    // transform.rotation に適用するEuler角

        [SerializeField] private bool applyAnimatorRotation;        // animator.transform.rotationを適用するか？
        [SerializeField] private Vector3 debugAnimatorRotation;     // animator.transform.rotation に適用するEuler角

        [SerializeField] private bool applyAnimatorAngle;           // animator.SetFloat("Angle", ...) を適用するか？
        [SerializeField] private float debugAnimatorAngle;          // 上記で適用するfloat値

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

        private void Update()
        {
            // === デバッグ用に、シリアライズフィールドの値を適用する処理 ===
            if (applyTransformRotation)
            {
                // debugTransformRotation (x, y, z) をEuler角として transform.rotation を更新
                transform.rotation = Quaternion.Euler(debugTransformRotation);
            }

            if (applyAnimatorRotation && animator != null)
            {
                // debugAnimatorRotation (x, y, z) をEuler角として animator.transform.rotation を更新
                animator.transform.rotation = Quaternion.Euler(debugAnimatorRotation);
            }

            if (applyAnimatorAngle && animator != null)
            {
                // debugAnimatorAngle を Angleパラメーターとして適用
                animator.SetFloat("Angle", debugAnimatorAngle);
            }
        }

        /// <summary>
        /// 子階層をすべて探索して、最初に見つかったAnimatorを返すメソッド
        /// </summary>
        private Animator FindChildAnimator(Transform parent)
        {
            foreach (Transform child in parent)
            {
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

            PlayerActionController playerActionController = GetComponent<PlayerActionController>();
            // PlayerActionControllerを無効化
            if (playerActionController != null)
            {
                playerActionController.OnForcedMoveBegin();
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

                Debug.Log($"animator.GetFloat(IsMove): {animator.GetBool("IsMove")}");

                // 実際に回転を適用
                transform.rotation = Quaternion.LookRotation(directionAnim.normalized, Vector3.up);
            }
            // ▲ ここまでアニメーターのパラメーター制御 ▲

            Vector3 direction = targetTransform.position;
            direction.y = transform.position.y;
            // ▼ DOTweenでターゲット位置へ移動 ▼
            transform.DOMove(direction, moveDuration)
                .OnComplete(() =>
                {
                    // 移動完了後に再有効化
                    if (playerActionController != null)
                    {
                        characterBrain.enabled = true;
                        playerActionController.OnForcedMoveEnd();
                    }
                });
        }
    }
}
