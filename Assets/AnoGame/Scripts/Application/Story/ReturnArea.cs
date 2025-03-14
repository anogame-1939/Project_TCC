using UnityEngine;
using AnoGame.Application.Player.Control;

namespace AnoGame.Application.Story
{
    [RequireComponent(typeof(Collider))]
    public class ReturnArea : MonoBehaviour
    {
        [SerializeField]
        private Transform returnPosition; // プレイヤーを戻す地点

        // トリガーに入ったら移動処理を開始
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // プレイヤーにアタッチされている MoveControl を取得
            PlayerActionController pac = other.GetComponent<PlayerActionController>();
            if (pac == null) return;

            pac.MoveToTarget(returnPosition.gameObject);
        }
    }
}
