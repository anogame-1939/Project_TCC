using System;
using UnityEngine.InputSystem;

namespace AnoGame.Application.Input
{
    public class InputActionSwitcher : IInputActionProvider, IDisposable
    {
        private readonly PlayerInput _playerInput;  // VContainer で注入
        public InputActionSwitcher(PlayerInput playerInput)
        {
            _playerInput = playerInput;
        }

        public InputActionMap GetUIActionMap()
            => _playerInput.actions.FindActionMap("UI", throwIfNotFound: true);

        public InputActionMap GetPlayerActionMap()
            => _playerInput.actions.FindActionMap("Player", throwIfNotFound: true);

        public void SwitchToUI()
        {
            // 必要に応じて currentMap.Disable() → UIMap.Enable()
            GetPlayerActionMap().Disable();
            // GetUIActionMap().Enable();
        }

        public void SwitchToPlayer()
        {
            // GetUIActionMap().Disable();
            GetPlayerActionMap().Enable();
        }

        /// <summary>
        /// たとえばシーン終了時やオブジェクト無効化時に全マップを無効化
        /// </summary>
        public void Dispose()
        {
            GetUIActionMap().Disable();
            GetPlayerActionMap().Disable();
        }
    }

}
