using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AnoGame.Application.Player.Control
{
    public class PlayerInputToggle : MonoBehaviour
    {
        private PlayerInput playerInput;
        private float _delay = 3f;

        void Start()
        {
            // 同一GameObject上のPlayerInputコンポーネントを取得
            playerInput = GetComponent<PlayerInput>();

            if (playerInput != null)
            {
                // PlayerInputを無効化
                playerInput.enabled = false;
                // 3秒後に再度有効化するコルーチンを開始
                StartCoroutine(EnablePlayerInputAfterDelay(_delay));
            }
            else
            {
                Debug.LogWarning("PlayerInputコンポーネントが見つかりません。");
            }
        }

        private IEnumerator EnablePlayerInputAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            // 3秒経過後にPlayerInputを有効化
            playerInput.enabled = true;
        }
    }
}
