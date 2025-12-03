using AnoGame.Application.Player.Control;
using UnityEngine;

namespace AnoGame.Application.Story
{
    public class PlayerMoveHandler : MonoBehaviour
    {
        // TODO: InjectでPlayerControlを取得して、強制移動モードを制御する

        public void EnableForceMode()
        {
            // TODO: EventLockControlを取得して、ForceModeを有効にする
        }

        public void DisableForceMode()
        {
        }

        private void MoveToTarget(GameObject target, bool doBackstep = false)
        {
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
