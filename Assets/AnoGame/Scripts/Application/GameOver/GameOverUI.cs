using UnityEngine;
using AnoGame.Application.Event;

namespace AnoGame.Application.GameOver
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject gameOverPanel;

        private void Awake()
        {
            GameOverManager.Instance.GameOver += ShowGameOverPanel;
            HideGameOverPanel();

            DontDestroyOnLoad(this);
        }

        private void ShowGameOverPanel()
        {
            gameOverPanel.SetActive(true);
            // カーソルを表示する
            Cursor.visible = true;
            // 必要に応じてロックを解除する場合は以下も追加
            Cursor.lockState = CursorLockMode.None;
        }

        private void HideGameOverPanel()
        {
            gameOverPanel.SetActive(false);
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
