using System;
using UnityEngine;

namespace AnoGame.Application
{
    public class GameStateManager : MonoBehaviour
    {
        // シングルトンインスタンス
        public static GameStateManager Instance { get; private set; }

        // 現在のゲーム状態（初期状態は Gameplay とする）
        public GameState CurrentState { get; private set; } = GameState.Gameplay;
        public GameSubState CurrentSubState { get; private set; } = GameSubState.None;

        // ゲーム状態変更時に通知するためのイベント
        public event Action<GameState> OnStateChanged;

        [SerializeField] private bool isDebug = false;

        private void Awake()
        {
            // 既に存在する場合は重複を避けるため削除する
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // シーン遷移しても破棄されないようにする
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (isDebug)
            {
                Debug.Log(CurrentState);
            }

        }

        /// <summary>
        /// ゲーム状態を変更し、変更時にイベントで通知します。
        /// </summary>
        /// <param name="newState">新しいゲーム状態</param>
        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
        
        public void SetSubState(GameSubState newState)
        {
            if (CurrentSubState == newState) return;

            CurrentSubState = newState;
            // OnStateChanged?.Invoke(newState);
        }
    }
}