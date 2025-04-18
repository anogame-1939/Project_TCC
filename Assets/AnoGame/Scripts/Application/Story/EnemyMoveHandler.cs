using AnoGame.Application.Enemy;
using AnoGame.Application.Player.Control;
using UnityEngine;

namespace AnoGame.Application.Story
{
    public class EnemyMoveHandler : MonoBehaviour
    {
        // ---------------------------------------------------------
        // まず、条件に合う ForcedMovementController を探すメソッドを用意
        // ---------------------------------------------------------
        private ForcedMovementController FindActiveEnemyForcedMover()
        {
            // シーン上にあるすべての ForcedMovementController を取得
            ForcedMovementController[] allControllers = FindObjectsOfType<ForcedMovementController>();
            
            // その中から、下記条件を満たす最初のものを返す
            //  - ゲームオブジェクトのタグが "EnemyTag"
            //  - ゲームオブジェクト自体がアクティブ
            //  - スクリプトが有効 (isActiveAndEnabled)
            foreach (var controller in allControllers)
            {
                if (controller.CompareTag("Enemy") && controller.isActiveAndEnabled)
                {
                    Debug.Log($"Enemy ForcedMover Found : {controller.gameObject.name}");
                    return controller;
                }
            }

            // 見つからなかった場合は null を返す
            return null;
        }

        // ---------------------------------------------------------
        // 以降、EnableForceMode / DisableForceMode / MoveToTarget 等で
        // 上記のメソッドを使って取得したものを使うように変更
        // ---------------------------------------------------------

        public void EnableForceMode()
        {
            ForcedMovementController enemyForcedMover = FindActiveEnemyForcedMover();
            if (enemyForcedMover == null) return;

            enemyForcedMover.EnableForceMode();
        }

        public void DisableForceMode()
        {
            ForcedMovementController enemyForcedMover = FindActiveEnemyForcedMover();
            if (enemyForcedMover == null) return;

            enemyForcedMover.DisableForceMode();

            // 敵の移動を通常モードに戻す
            // NOTE:雑にいれたけど問題があれば見直す
            EnemySpawnManager.Instance.SetupToNormalMode();
        }

        private void MoveToTarget(GameObject target, bool doBackstep = false)
        {
            ForcedMovementController enemyForcedMover = FindActiveEnemyForcedMover();
            if (enemyForcedMover == null) return;

            enemyForcedMover.ForceMoveTo(target.transform.position, doBackstep);
        }

        public void MoveToTarget(GameObject target)
        {
            MoveToTarget(target, false);
        }

        public void MoveToTargetBackstep(GameObject target)
        {
            MoveToTarget(target, true);
        }

        public void SetAngle(float angle)
        {
            ForcedMovementController enemyForcedMover = FindActiveEnemyForcedMover();
            if (enemyForcedMover == null) return;

            enemyForcedMover.SetAngle(angle);
        }

        public void FaceToTarget(GameObject target)
        {
            // こちらは PlayerActionController を使う例のままですが、
            // 同じ要領で EnemyTag かどうかを判定して探したい場合は
            // 別途 FindObjectsOfType<PlayerActionController>() などで
            // フィルタリングする実装に変更可能です。
            PlayerActionController playerForcedTransformMover = FindAnyObjectByType<PlayerActionController>();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.FaceTarget(target);
        }
    }
}
