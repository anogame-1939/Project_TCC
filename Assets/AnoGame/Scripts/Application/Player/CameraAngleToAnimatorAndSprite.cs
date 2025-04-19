using UnityEngine;
using Unity.TinyCharacterController;          // CharacterSettings が含まれる名前空間
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Player.Control
{
    /// <summary>
    /// カメラから見たプレイヤーの角度を Animator に渡し、
    /// スプライト（またはモデル）をカメラの方向に向けるクラス。
    /// </summary>
    public class CameraAngleToAnimatorAndSprite : MonoBehaviour
    {
        /* ───────────── 既存フィールド ───────────── */
        [Header("▼ Animator関連")]
        [SerializeField] private Animator animator;
        [SerializeField] private string angleParamName = "Angle";

        [Header("▼ カメラ参照")]
        [SerializeField] private Transform cameraTransform;

        [Header("▼ 2Dゲームの左右反転（任意）")]
        [SerializeField] private bool use2DFlip = false;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("▼ キャラクター設定 (Brain参照用)")]
        [SerializeField] private CharacterBrain characterBrain;

        [Header("▼ キャラクター設定 (カメラ参照用)")]
        [SerializeField] private CharacterSettings characterSettings;

        /* ───────────── 追加項目 ★───────────── */
        [Header("▼ HD‑2D 角度スナップ設定")]
        [Tooltip("角度を一定ステップに量子化するか")]
        [SerializeField] private bool useAngleSnap = true;

        [Tooltip("スナップ単位（°）。45 なら 8 方向、90 なら 4 方向など")]
        [SerializeField, Range(1, 90)] private int snapStep = 45;
        /* ───────────────────────────────────── */

        private bool animatorEnabled = true;

        private void Start()
        {
            if (!animator)          animator = GetComponentInChildren<Animator>();
            if (!spriteRenderer)    spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (!characterBrain)    characterBrain = GetComponent<CharacterBrain>();
            if (!characterSettings) characterSettings = GetComponent<CharacterSettings>();
        }

        private void Update()
        {
            if (!animatorEnabled || !characterBrain) return;

            // 1) カメラ Transform を取得
            if (!cameraTransform)
            {
                cameraTransform = characterSettings?.CameraTransform ??
                                  Camera.main?.transform;
                if (!cameraTransform) return;
            }

            // 2) カメラから見た角度を取得
            float camY    = cameraTransform.eulerAngles.y;
            float yawAngle = characterBrain.YawAngle;      // -180～180

            float rawAngle = Mathf.DeltaAngle(camY, yawAngle);

            /* ★ 3) 角度をスナップ */
            float angle = rawAngle;
            if (useAngleSnap && snapStep > 0)
                angle = Mathf.Round(angle / snapStep) * snapStep;

            /* ★ 左右反転を使う場合の処理（任意） */
            if (use2DFlip && spriteRenderer)
                spriteRenderer.flipX = angle > 90 || angle < -90;

            // 4) Animator パラメータにセット
            if (animator)
                animator.SetFloat(angleParamName, angle);
        }

        private void LateUpdate()
        {
            if (!cameraTransform)
            {
                cameraTransform = characterSettings?.CameraTransform ??
                                  Camera.main?.transform;
                if (!cameraTransform) return;
            }

            // スプライト(モデル)を常にカメラ方向へ向ける
            if (animator != null)
                animator.transform.forward = cameraTransform.forward;
        }

        // ───────────── 強制移動コールバック ─────────────
        public void OnForcedMoveBegin() => animatorEnabled = false;
        public void OnForcedMoveEnd()  => animatorEnabled = true;
    }
}
