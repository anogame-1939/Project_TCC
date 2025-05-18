using System.Collections;
using UnityEngine;
using Unity.TinyCharacterController.Control; // MoveControl がある名前空間

namespace AnoGame.Application.Player.Control
{
    public class PlayerForcedTransformMover : MonoBehaviour
    {
        [Header("▼ MoveControlを介した強制移動設定")]
        [SerializeField] private MoveControl moveControl;

        [Tooltip("目標位置に近づいたとみなす閾値（m）")]
        [SerializeField] private float arrivalThreshold = 0.2f;

        [Tooltip("アナログ入力を8方向にスナップするときのしきい値")]
        [SerializeField] private float snapThreshold = 0.5f;

        private Coroutine forceMoveRoutine;

        private void Awake()
        {
            if (moveControl == null)
            {
                moveControl = GetComponent<MoveControl>();
            }
        }

        /// <summary>
        /// 強制移動を開始する（がくがく回避版）
        /// </summary>
        /// <param name="targetTransform">移動先</param>
        public void ForceMove(Transform targetTransform)
        {
            if (targetTransform == null)
            {
                Debug.LogError("targetTransformが設定されていません。");
                return;
            }

            // すでに強制移動中なら一旦停止
            if (forceMoveRoutine != null)
            {
                StopCoroutine(forceMoveRoutine);
            }

            // PlayerActionController だけを無効化して、通常の入力を遮断
            PlayerActionController pac = GetComponent<PlayerActionController>();
            if (pac != null)
            {
                pac.StopChasing();
            }

            // コルーチン開始
            forceMoveRoutine = StartCoroutine(ForceMoveRoutine(targetTransform, pac));
        }

        private IEnumerator ForceMoveRoutine(Transform targetTransform, PlayerActionController pac)
        {
            // 1) 最初に一度だけ方向ベクトルを計算 & スナップ
            Vector2 forcedInput = CalculateSnappedInputOnce(targetTransform);

            // もし見た目上も最初にターゲット方向を向きたい場合:
            LookAtTargetOnce(targetTransform);

            // アニメーション開始
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsMove", true);
            }

            // 2) 毎フレーム、同じ入力ベクトルを与えつつ距離をチェック
            while (true)
            {
                Vector3 toTarget = targetTransform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude <= arrivalThreshold * arrivalThreshold)
                {
                    // 到着
                    break;
                }

                // 同じ入力ベクトルをMoveControlに渡す
                moveControl.Move(forcedInput);

                yield return null;
            }

            // 移動完了なので入力をゼロにして停止
            moveControl.Move(Vector2.zero);

            // PlayerActionControllerを再有効化
            if (pac != null)
            {
                pac.StartChasing();
            }

            // アニメーション終了
            if (animator != null)
            {
                animator.SetBool("IsMove", false);
            }

            forceMoveRoutine = null;
        }

        /// <summary>
        /// 最初に一度だけ方向ベクトルを計算し、カメラインバース+8方向スナップして返す
        /// </summary>
        private Vector2 CalculateSnappedInputOnce(Transform targetTransform)
        {
            // ワールド座標での目標方向
            Vector3 direction3D = targetTransform.position - transform.position;
            direction3D.y = 0f;

            // カメラ回転を取得して逆回転をかける
            Transform cameraTransform = Camera.main?.transform;
            if (cameraTransform != null)
            {
                Quaternion cameraYawRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
                direction3D = Quaternion.Inverse(cameraYawRotation) * direction3D;
            }

            // 正規化 & 8方向スナップ
            Vector2 leftStickInput = new Vector2(direction3D.x, direction3D.z).normalized;
            Vector2 snappedInput = SnapToKeyboardDirections(leftStickInput, snapThreshold);

            return snappedInput;
        }

        /// <summary>
        /// 一度だけ実際のオブジェクトをターゲット方向に向ける（必要に応じて）
        /// </summary>
        private void LookAtTargetOnce(Transform targetTransform)
        {
            // 水平方向だけLookAt
            Vector3 dir = targetTransform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        /// <summary>
        /// アナログ入力ベクトルを 8方向(上下左右＋斜め)にスナップ(0/1化)するヘルパーメソッド
        /// </summary>
        private Vector2 SnapToKeyboardDirections(Vector2 input, float threshold)
        {
            float x = 0f;
            float y = 0f;

            if (Mathf.Abs(input.x) >= threshold)
            {
                x = Mathf.Sign(input.x);
            }
            if (Mathf.Abs(input.y) >= threshold)
            {
                y = Mathf.Sign(input.y);
            }

            return new Vector2(x, y);
        }
    }
}
