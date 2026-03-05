using UnityEngine.InputSystem;

namespace AnoGame.Application.Input
{
    public interface IInputActionProvider
    {
        /// <summary>「UI」Action Map を取得</summary>
        InputActionMap GetUIActionMap();

        /// <summary>「Player」Action Map を取得</summary>
        InputActionMap GetPlayerActionMap();

        /// <summary>Action Map 切り替え（Player→UI など）</summary>
        void SwitchToUI();
        void SwitchToPlayer();
    }
}
