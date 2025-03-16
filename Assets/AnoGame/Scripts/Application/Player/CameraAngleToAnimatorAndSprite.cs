using UnityEngine;
using Unity.TinyCharacterController; // CharacterSettings が含まれる名前空間
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Player.Control
{
    /// <summary>
    /// カメラから見たプレイヤーの角度をAnimatorに渡し、
    /// スプライト（またはモデル）をカメラの方向に向けるクラス。
    /// 一部処理で CharacterSettings を使用し、カメラTransformを取得します。
    /// </summary>
    public class CameraAngleToAnimatorAndSprite : MonoBehaviour
    {
        [Header("▼ Animator関連")]
        [SerializeField]
        private Animator animator;

        [Tooltip("Animatorのパラメータ名。デフォルトでは \"Angle\" を使用。")]
        [SerializeField]
        private string angleParamName = "Angle";

        [Header("▼ カメラ参照")]
        [Tooltip("CharacterSettings から優先的にカメラを取得。無ければ Camera.main を使用。")]
        [SerializeField]
        private Transform cameraTransform;

        [Header("▼ 2Dゲームで左右反転を使う場合などに使用（任意）")]
        [SerializeField]
        private bool use2DFlip = false;

        [Tooltip("スプライトの左右反転を行う際に使用するSpriteRenderer。")]
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [Header("▼ キャラクター設定 (Brain参照用)")]
        [Tooltip("CharacterBrain / BrainBase を参照する")]
        [SerializeField]
        private CharacterBrain characterBrain;

        [Header("▼ キャラクター設定 (カメラ参照用)")]
        [Tooltip("CameraTransformを持っている CharacterSettings")]
        [SerializeField]
        private CharacterSettings characterSettings;

        private void Start()
        {
            // コンポーネント自動取得など必要なら
            if (!animator)
            {
                animator = GetComponentInChildren<Animator>();
            }
            if (!spriteRenderer)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (!characterBrain)
            {
                characterBrain = GetComponent<CharacterBrain>();
            }
            if (!characterSettings)
            {
                characterSettings = GetComponent<CharacterSettings>();
            }
        }

        private void Update()
        {
            // CharacterBrain がアタッチされていなければ処理を中断
            if (!characterBrain)
            {
                return;
            }

            // 1) カメラTransformを取得（CharacterSettings → Camera.main の順で参照）
            if (!cameraTransform)
            {
                if (characterSettings != null && characterSettings.CameraTransform != null)
                {
                    cameraTransform = characterSettings.CameraTransform;
                }
                else if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }
                else
                {
                    return; // カメラが見つからなければ処理中断
                }
            }

            // 2) カメラから見たプレイヤーの角度を算出
            //    ここでは "カメラのY軸" と "プレイヤーのY軸" を比較して DeltaAngle を取る例
            float camY = cameraTransform.eulerAngles.y;
            float yawAngle = characterBrain.YawAngle;

            // Mathf.DeltaAngle は -180 ~ 180 の範囲で角度差を返す
            float rawAngle = Mathf.DeltaAngle(camY, yawAngle);

            // 3) Animatorのパラメーターにセット
            if (animator)
            {
                animator.SetFloat(angleParamName, rawAngle);
            }
        }

        private void LateUpdate()
        {
            // 1) カメラTransformを再チェック
            if (!cameraTransform)
            {
                if (characterSettings != null && characterSettings.CameraTransform != null)
                {
                    cameraTransform = characterSettings.CameraTransform;
                }
                else if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }
                else
                {
                    return; // カメラが見つからなければ処理中断
                }
            }
            animator.transform.forward = cameraTransform.forward;
        }
    }
}
