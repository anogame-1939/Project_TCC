using UnityEngine;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// Flashback用の薄いラッパー。
    /// SceneImageDisplay で背景画像を制御する。
    /// </summary>
    public class FlashbackUIController : MonoBehaviour
    {
        public void ShowFlashback(Sprite sprite)
        {
            if (AnoGame.AnoDialogue.DialogueManager.Instance != null)
            {
                var display = AnoGame.AnoDialogue.DialogueManager.Instance.GetSceneImageDisplay("Flashback");
                if (display != null)
                {
                    display.Show(sprite);
                }
            }
        }

        public void HideFlashback()
        {
            if (AnoGame.AnoDialogue.DialogueManager.Instance != null)
            {
                var display = AnoGame.AnoDialogue.DialogueManager.Instance.GetSceneImageDisplay("Flashback");
                if (display != null)
                {
                    display.Hide();
                }
            }
        }
    }
}