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
                Debug.Log($"Backstep: {relativeAngle}");
                relativeAngle = relativeAngle - 180f ;
                Debug.Log($"Backstep: {relativeAngle}");
            }
            relativeAngle = RoundAngleTo45(relativeAngle);
            animator.SetFloat("Angle", relativeAngle);
        }

        // 移動中は回転は固定し、位置だけ更新
        while (Vector3.Distance(transform.position, targetPosition) > 1f)
        {
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            characterBrain.ForceSetPosition(newPosition);
            yield return null;
        }

        // 移動完了時に Animator のパラメータをリセット
        if (animator != null)
        {
            animator.SetBool("IsMove", false);
        }
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
        if (playerActionController != null)
            playerActionController.OnForcedMoveBegin();
        if (cameraAngleController != null)
            cameraAngleController.OnForcedMoveBegin();
    }

    public void DisableForceMode()
    {
        if (playerActionController != null)
            playerActionController.OnForcedMoveEnd();
        if (cameraAngleController != null)
            cameraAngleController.OnForcedMoveEnd();
    }
}
