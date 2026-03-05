using UnityEngine;

namespace AnoGame.Application
{
    /// <summary>
    /// UnityEventからGameStateManager.Instanceへアクセスするためのヘルパークラス
    /// GameStateManagerのインスタンスがシーン内で直接参照できない場合（プレハブなど）に使用してください。
    /// </summary>
    public class GameStateEventHelper : MonoBehaviour
    {
        /// <summary>
        /// GameStateを変更します。UnityEventからEnumを選択して呼び出せます。
        /// </summary>
        /// <param name="state">変更するステート</param>
        public void SetState(GameState state)
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(state);
            }
            else
            {
                Debug.LogWarning("GameStateManager Instance is null.");
            }
        }

        /// <summary>
        /// GameStateをGameOverに設定します。
        /// </summary>
        public void SetGameOver()
        {
            SetState(GameState.GameOver);
        }

        /// <summary>
        /// GameStateをGameplayに設定します。
        /// </summary>
        public void SetGameplay()
        {
            SetState(GameState.Gameplay);
        }
    }
}
