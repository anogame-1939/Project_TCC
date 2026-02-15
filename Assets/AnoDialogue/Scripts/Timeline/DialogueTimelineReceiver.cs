using UnityEngine;

namespace AnoGame.AnoDialogue.Timeline
{
    /// <summary>
    /// Handler component for Timeline integration.
    /// Acts as a bridge between the Timeline Track and the global DialogueManager.
    /// Used primarily for checking conversation active state to pause timeline.
    /// </summary>
    public class DialogueTimelineReceiver : MonoBehaviour
    {
        public bool IsConversationActive
        {
            get
            {
                if (DialogueManager.Instance != null)
                {
                    return DialogueManager.Instance.IsConversationActive;
                }

                // If no Manager, we are not active
                return false;
            }
        }

        public void Play(string conversationID, string styleName = null)
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(conversationID, styleName);
            }
            else
            {
                SimulatePlay(conversationID);
            }
        }

        public void Play(string conversationID, string styleName, Sprite backgroundSprite, Sprite locationSprite)
        {
            if (DialogueManager.Instance != null)
            {
                // Flashback用背景画像
                if (backgroundSprite != null)
                {
                    var flashbackUI = DialogueManager.Instance.GetUI(styleName) as UI.FlashbackUIController;
                    if (flashbackUI != null)
                    {
                        flashbackUI.SetBackgroundImage(backgroundSprite);
                    }
                }

                // ロケーション画像
                if (locationSprite != null)
                {
                    var dialogueUI = DialogueManager.Instance.GetUI(styleName) as UI.DialogueUIController;
                    if (dialogueUI != null)
                    {
                        dialogueUI.SetLocationImage(locationSprite);
                    }
                }
            }
            Play(conversationID, styleName);
        }

        private void SimulatePlay(string conversationID)
        {
            Debug.Log($"<color=cyan>[DialogueTimelineReceiver] Simulation Mode: Playing conversation '{conversationID}' (No DialogueManager found)</color>");
        }
    }
}
