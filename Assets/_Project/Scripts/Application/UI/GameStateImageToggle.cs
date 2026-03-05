using UnityEngine;
using UnityEngine.UI;

namespace AnoGame.Application.UI
{
    [System.Serializable]
    public class GameStateImageToggle : MonoBehaviour
    {
        [SerializeField]
        private Image reticleImage;

        void Start()
        {
            reticleImage = GetComponent<Image>();
        }

        void Update()
        {
            // 画面がアクティブな場合のみ処理を行う
            if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
            {
                reticleImage.enabled = true;
            }
            else
            {
                reticleImage.enabled = false;
            }
        }
    }
}
