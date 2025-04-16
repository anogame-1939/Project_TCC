using UnityEngine;

namespace AnoGame.Application
{
    public class GameStateHandler : MonoBehaviour
    {
        /// <summary>
        /// 通常のゲームプレイ状態に変更します
        /// </summary>
        public void SetGameplay()
        {
            GameStateManager.Instance.SetState(GameState.Gameplay);
        }

        /// <summary>
        /// インベントリ表示状態に変更します
        /// </summary>
        public void SetInventory()
        {
            GameStateManager.Instance.SetState(GameState.Inventory);
        }

        /// <summary>
        /// オプション画面表示状態に変更します
        /// </summary>
        public void SetSettings()
        {
            GameStateManager.Instance.SetState(GameState.Settings);
        }

        /// <summary>
        /// ゲームオーバー状態に変更します
        /// </summary>
        public void SetGameOver()
        {
            GameStateManager.Instance.SetState(GameState.GameOver);
        }

        /// <summary>
        /// ゲーム内イベント中状態に変更します
        /// </summary>
        public void SetInGameEvent()
        {
            GameStateManager.Instance.SetState(GameState.InGameEvent);
        }
    }
}
