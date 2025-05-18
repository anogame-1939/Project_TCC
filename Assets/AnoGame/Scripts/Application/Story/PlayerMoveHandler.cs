using AnoGame.Application.Player.Control;
using UnityEngine;

namespace AnoGame.Application.Story
{
    public class PlayerMoveHandler : MonoBehaviour
    {
        /// <summary>
        /// シーン上にあるすべての ForcedMovementController のうち、
        /// タグが "Player" かつアクティブで有効なものを探して返す。
        /// 見つからなければ null を返す。
        /// </summary>
        private ForcedMovementController FindActivePlayerForcedMover()
        {
            // シーン上にあるすべての ForcedMovementController を取得
            ForcedMovementController[] allControllers = FindObjectsOfType<ForcedMovementController>();

            // その中から、タグが "Player" で、アクティブ＆有効なものを探す
            foreach (var controller in allControllers)
            {
                if (controller.CompareTag("Player") && controller.isActiveAndEnabled)
                {
                    Debug.Log($"Player ForcedMover Found : {controller.gameObject.name}");
                    return controller;
                }
            }

            // 見つからなかった場合は null を返す
            return null;
        }

        public void EnableForceMode()
        {
            ForcedMovementController playerForcedTransformMover = FindActivePlayerForcedMover();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.EnableForceMode();
        }

        public void DisableForceMode()
        {
            ForcedMovementController playerForcedTransformMover = FindActivePlayerForcedMover();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.DisableForceMode();
        }

        private void MoveToTarget(GameObject target, bool doBackstep = false)
        {
            ForcedMovementController playerForcedTransformMover = FindActivePlayerForcedMover();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.ForceMoveTo(target.transform.position, doBackstep);
        }

        public void MoveToTarget(GameObject target)
        {
            MoveToTarget(target, false);
        }

        public void MoveToTargetBackstep(GameObject target)
        {
            Debug.Log("MoveToTargetBackstep");
            MoveToTarget(target, true);
            Debug.Log("MoveToTargetBackstep");
        }

        public void SetAngle(float angle)
        {
            ForcedMovementController forcedMovementController = FindActivePlayerForcedMover();
            if (forcedMovementController == null) return;

            forcedMovementController.SetAngle(angle);
        }

        public void FaceToTarget(GameObject target)
        {
            // こちらは PlayerActionController を使う例のままですが、
            // 同じ要領で Player タグを持つものだけを探したい場合は
            // FindObjectsOfType<PlayerActionController>() + タグ判定 で実装可能です。
            PlayerActionController playerForcedTransformMover = FindAnyObjectByType<PlayerActionController>();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.FaceTarget(target);
        }
    }
}
