using System.Collections;
using UnityEngine;
using Unity.TinyCharacterController.Brain;
using AnoGame.Application.Player.Control;

public class ForcedMovementController : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("参照コンポーネント")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterBrain characterBrain;
    [SerializeField] private PlayerActionController playerActionController;
    [SerializeField] private CameraAngleToAnimatorAndSprite cameraAngleController;

    /// <summary>
    /// 指定した位置へ強制移動を開始する
    /// </summary>
    public void ForceMoveTo(Vector3 targetPosition)
    {
        StartCoroutine(ForceMoveRoutine(targetPosition));
    }

    private IEnumerator ForceMoveRoutine(Vector3 targetPosition)
    {
        // 通常操作を無効化
        if (playerActionController != null)
            playerActionController.OnForcedMoveBegin();
        if (cameraAngleController != null)
            cameraAngleController.OnForcedMoveBegin();

        // Animator の IsMove をオンに
        if (animator != null)
            animator.SetBool("IsMove", true);

        // 目的地に十分近づくまで移動する
        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            // 目的地に向かう方向を計算
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            // 移動方向から YawAngle を計算（※ Mathf.Atan2 はラジアンを返すので変換）
            float forcedYawAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Quaternion desiredRotation = Quaternion.Euler(0, forcedYawAngle, 0);
            
            // キャラクターの回転を更新
            // ※ CharacterBrain の内部状態（YawAngle も含む）を更新するため、SetRotationDirectly を使う
            characterBrain.ForceSetRotation(desiredRotation);

            // 位置を補間で更新（CharacterBrain 経由で移動）
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            characterBrain.ForceSetPosition(newPosition);

            // Animator の Angle を更新（カメラとの相対角度を算出）
            if (animator != null && Camera.main != null)
            {
                float cameraY = Camera.main.transform.eulerAngles.y;
                float relativeAngle = Mathf.DeltaAngle(cameraY, forcedYawAngle);
                animator.SetFloat("Angle", relativeAngle);
            }

            yield return null;
        }

        // 最終位置に固定
        characterBrain.ForceSetPosition(targetPosition);

        // Animator のパラメータをリセット
        if (animator != null)
        {
            animator.SetBool("IsMove", false);
            animator.SetFloat("Angle", 0f);
        }

        // 通常操作を再有効化
        if (playerActionController != null)
            playerActionController.OnForcedMoveEnd();
        if (cameraAngleController != null)
            cameraAngleController.OnForcedMoveEnd();
    }
}
