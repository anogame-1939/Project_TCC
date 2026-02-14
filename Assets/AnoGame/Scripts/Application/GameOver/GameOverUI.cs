using UnityEngine;
using AnoGame.AnoFlow;
using AnoGame.Application.UI;

namespace AnoGame.Application.GameOver
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField]
        private UISection section;

        private void Awake()
        {
            GameOverManager.Instance.GameOver += ShowGameOverPanel;

            // パネル自体はActiveにしておく（CanvasGroupで制御するため）
            if (section.panel != null)
            {
                section.panel.SetActive(true);
            }

            HideGameOverPanel();
        }

        private void ShowGameOverPanel()
        {
            if (section.canvasGroup != null)
            {
                section.canvasGroup.alpha = 1f;
                section.canvasGroup.interactable = true;
                section.canvasGroup.blocksRaycasts = true;
            }

            section.selectables[0].Select();

            // カーソルを表示する
            Cursor.visible = true;
            // 必要に応じてロックを解除する場合は以下も追加
            Cursor.lockState = CursorLockMode.None;
        }

        private void HideGameOverPanel()
        {
            if (section.canvasGroup != null)
            {
                section.canvasGroup.alpha = 0f;
                section.canvasGroup.interactable = false;
                section.canvasGroup.blocksRaycasts = false;
            }

            // ゲーム中はカーソルを非表示にする
            Cursor.visible = false;
            // 必要に応じてロック状態に戻す場合は以下も追加
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void Retry()
        {
            Debug.Log("Retry Button Clicked!");
            HideGameOverPanel();
            // 現在のシーンをやり直す
            // StoryManager.Instance.RetyrCurrentScene();

            GameOverManager.Instance.OnRetryGame();
        }
    }
}
