using UnityEngine;

namespace AnoGame.Application.Objects
{

    /// <summary>
    /// 子の SpriteRenderer が常にメインカメラを向くよう、
    /// 親オブジェクトの回転を更新するシンプルなビルボード処理。
    /// </summary>
    [ExecuteAlways]               // エディタ上でも向きを更新したい場合は付ける
    public class BillboardToCamera : MonoBehaviour
    {
        [Tooltip("モデルの前後が逆向きの場合はチェックを入れてください")]
        [SerializeField] private bool flip = true;

        private Camera mainCam;

        private void Awake()
        {
            mainCam = Camera.main;
        }

        private void LateUpdate()
        {
            if (mainCam == null)
                mainCam = Camera.main;

            if (mainCam == null)
                return;   // シーンにカメラが無いときは何もしない

            // カメラ → このオブジェクト 方向ベクトル
            Vector3 forward = transform.position - mainCam.transform.position;

            // カメラ正面へ向ける
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            // 前後が逆なら 180° 回転
            if (flip)
                transform.Rotate(0f, 180f, 0f);
        }
    }
}