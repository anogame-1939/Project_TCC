using UnityEngine;
using AnoGame.Application.Event;
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
            HideGameOverPanel();

            // DontDestroyOnLoad(this);
            section.panel.SetActive(false);
        }

        private void ShowGameOverPanel()
        {
            section.panel.SetActive(true);
            section.selectables[0].Select();

            // カーソルを表示する
            Cursor.visible = true;
            // 必要に応じてロックを解除する場合は以下も追加
            Cursor.lockState = CursorLockMode.None;
        }

        private void HideGameOverPanel()
        {
            section.panel.SetActive(false);
            // ゲーム中はカーソルを非表示にする
            Cursor.visible = false;
            // 必要に応じてロック状態に戻す場合は以下も追加
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        public void OnClickRetryButton()
        {
            Debug.Log("Retry Button Clicked!");
            HideGameOverPanel();
            // 現在のシーンをやり直す
            // StoryManager.Instance.RetyrCurrentScene();

            GameOverManager.Instance.OnRetryGame();
        }
    }
}
