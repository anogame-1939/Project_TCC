using UnityEngine;

namespace AnoGame.SLFBDebug
{
    public class DebugTool : MonoBehaviour
    {
        // CanvasGroup コンポーネントを保持
        private CanvasGroup targetCanvasGroup;

        // ツールの表示状態
        private bool isToolActive = false;

        void Start()
        {
            // 同じ GameObject にアタッチされている CanvasGroup を取得
            targetCanvasGroup = GetComponent<CanvasGroup>();
            // 初期状態を反映
            UpdateToolState();
        }

        void Update()
        {
            // '.'キーが押されたらトグル
            if (Input.GetKeyDown(KeyCode.Period))
            {
                Toggle();
            }
        }

        /// <summary>
        /// デバッグツールの表示／非表示と
        /// カーソルロック＆可視化を切り替える
        /// </summary>
        public void Toggle()
        {
            isToolActive = !isToolActive;
            UpdateToolState();
        }

        /// <summary>
        /// 現在の isToolActive に応じて
        /// CanvasGroup、Cursor.lockState、Cursor.visible を設定
        /// </summary>
        private void UpdateToolState()
        {
            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.alpha = isToolActive ? 1f : 0f;
                targetCanvasGroup.interactable = isToolActive;
                targetCanvasGroup.blocksRaycasts = isToolActive;
            }

            // ツール表示中はカーソルを解放＆可視化、
            // 非表示中はロック＆非表示
            Cursor.lockState = isToolActive
                ? CursorLockMode.None
                : CursorLockMode.Locked;
            Cursor.visible = isToolActive;
        }
    }
}
