using System.Collections;
using UnityEngine;
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Player.Control
{
    public class ForcedMovementController : MonoBehaviour
    {
        [Header("移動設定")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("参照コンポーネント")]
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterBrain characterBrain;
        [SerializeField] private IForcedMoveController actionController;
        [SerializeField] private CameraAngleToAnimatorAndSprite cameraAngleController;


        [Header("テスト用")]
        [SerializeField] private Transform testTarget; // ★ 追加: テスト用ターゲット

        void Start()
        {
            actionController = GetComponent<IForcedMoveController>();

            if (actionController == null)
            {
                Debug.LogError($"[{nameof(actionController)}] " +
                            $"IForcedMoveController を実装したコンポーネントが見つかりません。");
            }
        }


        /// <summary>
        /// 指定した位置へ強制移動を開始する
        /// </summary>
        public void ForceMoveTo(Vector3 targetPosition, bool doBackstep)
        {
            StartCoroutine(ForceMoveRoutine(targetPosition, doBackstep));
        }

        private IEnumerator ForceMoveRoutine(Vector3 targetPosition, bool doBackstep)
        {
            // Animator の IsMove をオンにする
            if (animator != null)
                animator.SetBool("IsMove", true);

            // 強制移動開始時に一度だけ、移動方向から回転（YawAngle）を決定し、Animator の Angle を設定する
            Vector3 initialDirection = (targetPosition - transform.position).normalized;
            float forcedYawAngle = Mathf.Atan2(initialDirection.x, initialDirection.z) * Mathf.Rad2Deg;
            Quaternion desiredRotation = Quaternion.Euler(0, forcedYawAngle, 0);
            characterBrain.ForceSetRotation(desiredRotation);
            if (animator != null && Camera.main != null)
            {
                float cameraY = Camera.main.transform.eulerAngles.y;
                float relativeAngle = Mathf.DeltaAngle(cameraY, forcedYawAngle);
                // バックステップの場合は角度を反転
                if (doBackstep)
                {
                    // ①180° 反転 → ②-180〜180°に正規化
                    relativeAngle = Mathf.DeltaAngle(0f, relativeAngle - 180f);
                }
                relativeAngle = RoundAngleTo45(relativeAngle);
                animator.SetFloat("Angle", relativeAngle);
            }

            // 移動中は回転は固定し、位置だけ更新
            while (Vector3.Distance(transform.position, targetPosition) > 1f)
            {
                animator.SetBool("IsMove", true);
                Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                characterBrain.ForceSetPosition(newPosition);
                yield return null;
            }

            // 移動完了時に Animator のパラメータをリセット
            if (animator != null)
            {
                animator.SetBool("IsMove", false);
            }
            Debug.Log("ForceMoveRoutine");
        }


        private float RoundAngleTo45(float angle)
        {
            return Mathf.Round(angle / 45f) * 45f;
        }

        public void SetAngle(float angle)
        {
            if (animator != null)
            {
                animator.SetFloat("Angle", angle);
            }
        }

        public void EnableForceMode()
        {
            if (actionController != null)
                actionController.StopChasing();
            if (cameraAngleController != null)
                cameraAngleController.OnForcedMoveBegin();
        }

        public void DisableForceMode()
        {
            if (actionController != null)
                actionController.StartChasing();
            if (cameraAngleController != null)
                cameraAngleController.OnForcedMoveEnd();
        }
        
        // ★ 追加: コンテキストメニューから実行できるテスト用メソッド
        [ContextMenu("Test Force Move (doBackstep = false)")]
        private void TestForceMove()
        {
            if (testTarget != null)
            {
                ForceMoveTo(testTarget.position, false);
            }
            else
            {
                Debug.LogWarning("Test target is not assigned.");
            }
        }
    }
}