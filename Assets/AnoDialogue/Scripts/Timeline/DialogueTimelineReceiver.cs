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

        public void Play(string conversationID, string styleName, Sprite sceneImage)
        {
            if (DialogueManager.Instance != null)
            {
                // スタイルに応じたシーン画像設定（ロケーション画像/背景等）
                if (sceneImage != null)
                {
                    var ui = DialogueManager.Instance.GetUI(styleName);
                    if (ui != null)
                    {
                        ui.SetSceneImage(sceneImage);
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
