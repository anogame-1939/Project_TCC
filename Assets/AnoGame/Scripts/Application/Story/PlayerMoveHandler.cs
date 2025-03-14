using AnoGame.Application.Player.Control;
using UnityEngine;

namespace AnoGame.Application.Story
{
    public class PlayerMoveHandler : MonoBehaviour
    {   
        public void FaceTarget(GameObject target)
        {
            // シーン内から PlayerActionController (PAC) を取得する
            var pac = FindAnyObjectByType<PlayerActionController>();
            if (pac != null)
            {
                pac.FaceTarget(target);
            }
            else
            {
                Debug.LogWarning("PlayerActionController が見つかりません。");
            }
        }
    }
}