using UnityEngine;
using Unity.TinyCharacterController;          // CharacterSettings が含まれる名前空間

namespace AnoGame.Application.Objects
{
    /// <summary>
    /// 子の SpriteRenderer が常にカメラを向くように
    /// 親オブジェクトの回転を更新するビルボード処理。
    /// </summary>
    [ExecuteAlways]   // エディタ上でも向きを更新したい場合
    public class BillboardToCamera : MonoBehaviour
    {
        [Tooltip("モデルの前後が逆向きの場合はチェックを入れてください")]
        [SerializeField] private bool flip = true;

        [Header("▼ キャラクター設定 (カメラ参照用)")]
        [SerializeField] private CharacterSettings characterSettings;

        private Transform camTransform;

        private void Awake()
        {
            // 自動取得（同階層か親にある想定）
            if (!characterSettings)
                characterSettings = GetComponentInParent<CharacterSettings>();

            camTransform = GetCameraFromSettings();
        }

        private void LateUpdate()
        {
            // 毎フレーム取得しておく（カットシーンなどで差し替わる場合に対応）
            camTransform = GetCameraFromSettings();
            if (!camTransform) return;

            // カメラ→オブジェクト方向ベクトル
            Vector3 forward = transform.position - camTransform.position;

            // カメラ正面へ向ける
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            // 前後が逆なら 180° 反転
            if (flip)
                transform.Rotate(0f, 180f, 0f);
        }

        /// <summary>
        /// CharacterSettings からカメラ Transform を取得。
        /// 無ければ Camera.main を返す。
        /// </summary>
        private Transform GetCameraFromSettings()
        {
            if (characterSettings && characterSettings.CameraTransform)
                return characterSettings.CameraTransform;

            return Camera.main ? Camera.main.transform : null;
        }
    }
}
