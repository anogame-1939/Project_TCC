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
        public event Action<GameState>      OnStateChanged;
        // サブステート変更時に通知するためのイベント
        public event Action<GameSubState>   OnSubStateChanged;

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
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (isDebug)
            {
                Debug.Log($"State: {CurrentState}, SubState: {CurrentSubState}");
            }
        }

        /// <summary>
        /// ゲーム状態を変更し、OnStateChanged を発火
        /// </summary>
        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// サブステートを変更し、OnSubStateChanged を発火
        /// </summary>
        public void SetSubState(GameSubState newSubState)
        {
            if (CurrentSubState == newSubState) return;

            CurrentSubState = newSubState;
            OnSubStateChanged?.Invoke(newSubState);
        }
    }
}
