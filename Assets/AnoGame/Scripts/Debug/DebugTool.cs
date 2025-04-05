using UnityEngine;

namespace AnoGame.Application.SLFBDebug
{
    public class DebugTool : MonoBehaviour
    {
        // CanvasGroupコンポーネントを格納する変数
        private CanvasGroup targetCanvasGroup;

        // ツールの表示状態（true: 表示, false: 非表示）
        private bool isToolActive = false;

        void Start()
        {
            // 同じGameObjectにアタッチされているCanvasGroupを取得
            targetCanvasGroup = GetComponent<CanvasGroup>();
            targetCanvasGroup.alpha = 0; // 初期状態で非表示にする
        }

        void Update()
        {
            // '.'キーが押された場合に処理を実行
            if (Input.GetKeyDown(KeyCode.Period))
            {
                // 表示状態を反転させる
                isToolActive = !isToolActive;

                // CanvasGroupが存在する場合、透明度を設定する
                if (targetCanvasGroup != null)
                {
                    targetCanvasGroup.alpha = isToolActive ? 1 : 0;
                }
            }
        }
    }
}
