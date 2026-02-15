using UnityEngine;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// Flashback用の薄いラッパー。
    /// AnoDialogue側のFlashbackUIControllerに背景画像制御が統合されたため、
    /// 必要に応じてこのクラスから呼び出す。
    /// </summary>
    public class FlashbackUIController : MonoBehaviour
    {
        public void ShowFlashback(Sprite sprite)
        {
            if (AnoGame.AnoDialogue.DialogueManager.Instance != null)
            {
                var flashbackUI = AnoGame.AnoDialogue.DialogueManager.Instance.GetUI("Flashback")
                    as AnoGame.AnoDialogue.UI.FlashbackUIController;
                if (flashbackUI != null)
                {
                    flashbackUI.SetBackgroundImage(sprite);
                }
            }
        }

        public void HideFlashback()
        {
            if (AnoGame.AnoDialogue.DialogueManager.Instance != null)
            {
                var flashbackUI = AnoGame.AnoDialogue.DialogueManager.Instance.GetUI("Flashback")
                    as AnoGame.AnoDialogue.UI.FlashbackUIController;
                if (flashbackUI != null)
                {
                    flashbackUI.ClearBackgroundImage();
                }
            }
        }
    }
}