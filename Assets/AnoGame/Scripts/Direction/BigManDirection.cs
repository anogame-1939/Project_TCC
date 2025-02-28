using System.Collections;
using UnityEngine;
using Unity.TinyCharacterController.Brain;
using Unity.TinyCharacterController.Control;

namespace AnoGame.Direction
{
    public class BigManDirection : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private MoveNavmeshControl moveNavmeshControl;  // インスペクターでアサイン可
        [SerializeField] private GameObject[] movePositions;      // 移動先のGameObject

        [Header("パラメーター")]
        [SerializeField, Tooltip("目的地までの移動にかける時間(秒)")]
        private float moveDuration = 2.0f;

        [SerializeField, Tooltip("移動後に待機する時間(秒)")]
        private float waitTime = 1.0f;

        private void OnEnable()
        {
            // インスペクターでセットしていない場合、GetComponentで補完
            if (moveNavmeshControl == null)
            {
                moveNavmeshControl = GetComponent<MoveNavmeshControl>();
                moveNavmeshControl.SetTargetPosition(movePositions[0].transform.position);
            }

            // 通常は自動で開始する場合
            StartCoroutine(FirstMove());
        }

        /// <summary>
        /// エディタ拡張から呼び出す用のpublicメソッド
        /// </summary>
        public void StartFirstMove()
        {
            StartCoroutine(FirstMove());
        }

        /// <summary>
        /// 最初の移動を実行するコルーチン
        /// </summary>
        private IEnumerator FirstMove()
        {
            // 要素0から1へ、moveDuration秒かけて移動
            Vector3 startPos = movePositions[0].transform.position;
            Vector3 endPos   = movePositions[1].transform.position;

            yield return StartCoroutine(MoveOverTime(startPos, endPos, moveDuration));

            // 移動後に waitTime 秒待機
            yield return new WaitForSeconds(waitTime);

            // ここに、さらに別の移動や処理を続けるなら追加
        }

        /// <summary>
        /// 一定時間かけて start から end へ移動するコルーチン
        /// </summary>
        private IEnumerator MoveOverTime(Vector3 start, Vector3 end, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Lerpで補間
                Vector3 newPos = Vector3.Lerp(start, end, t);

                // moveNavmeshControlのTransformを書き換え
                moveNavmeshControl.transform.position = newPos;

                yield return null; // 次のフレームまで待つ
            }

            // 最終的に位置を明示的に確定させる
            moveNavmeshControl.transform.position = end;
        }
    }
}
