using UnityEngine;
using UnityEngine.UI;

namespace AnoGame.Application.UI
{
    public class FlashbackUIController : MonoBehaviour
    {
        public void ShowFlashback(Sprite sprite)
        {
            FlashbackUIManager.Instance.SetImage(sprite);
            FlashbackUIManager.Instance.Show();
        }

        public void HideFlashback()
        {
            FlashbackUIManager.Instance.Hide();
        }
    }
}