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
            cameraAngleController.OnForcedMoveBegin(); // ※この処理は、Animator の Angle 更新のみ停止する実装になっている前提です

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
            relativeAngle = RoundAngleTo45(relativeAngle);
            animator.SetFloat("Angle", relativeAngle);
        }

        // 移動中は、回転は初期で決定したままとし、位置だけを更新する
        while (Vector3.Distance(transform.position, targetPosition) > 1f)
        {
            Debug.Log($"Distance: {Vector3.Distance(transform.position, targetPosition)} | Current: {transform.position} | Target: {targetPosition}");
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            Debug.Log($"New Position: {newPosition}");
            characterBrain.ForceSetPosition(newPosition);
            yield return null;
        }


        // 最終位置に固定
        characterBrain.ForceSetPosition(targetPosition);

        // 移動完了時に Animator のパラメータをリセットする
        if (animator != null)
        {
            animator.SetBool("IsMove", false);
            // animator.SetFloat("Angle", 0f);
        }

        // 通常操作を再有効化
        if (playerActionController != null)
            playerActionController.OnForcedMoveEnd();
        if (cameraAngleController != null)
            cameraAngleController.OnForcedMoveEnd();
    }

    private float RoundAngleTo45(float angle)
    {
        return Mathf.Round(angle / 45f) * 45f;
    }
}
