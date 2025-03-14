using AnoGame.Application.Player.Control;
using UnityEngine;

namespace AnoGame.Application.Story
{
    public class PlayerMoveHandler : MonoBehaviour
    {   
        private void MoveToTarget(GameObject target, bool doBackstep = false)
        {
            // シーン内から PlayerActionController (PAC) を取得する
            var pac = FindAnyObjectByType<PlayerActionController>();
            if (pac != null)
            {
                pac.MoveToTarget(target, doBackstep);
            }
            else
            {
                Debug.LogWarning("PlayerActionController が見つかりません。");
            }
        }

        public void MoveToTarget(GameObject target)
        {
            MoveToTarget(target, false);
        }

        public void MoveToTargetBackstep(GameObject target)
        {
            MoveToTarget(target, true);
        }
    }
}