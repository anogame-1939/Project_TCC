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

        /// <summary>
        /// SceneImageTrack から呼ばれる。指定スタイルの SceneImageDisplay にシーン画像を表示する。
        /// </summary>
        public void ShowSceneImage(string styleName, Sprite sprite)
        {
            if (DialogueManager.Instance != null)
            {
                var display = DialogueManager.Instance.GetSceneImageDisplay(styleName);
                if (display != null)
                {
                    display.Show(sprite);
                }
                else
                {
                    Debug.LogWarning($"[DialogueTimelineReceiver] SceneImageDisplay not found for style: {styleName}");
                }
            }
        }

        /// <summary>
        /// SceneImageTrack から呼ばれる。指定スタイルの SceneImageDisplay のシーン画像を非表示にする。
        /// </summary>
        public void HideSceneImage(string styleName)
        {
            if (DialogueManager.Instance != null)
            {
                var display = DialogueManager.Instance.GetSceneImageDisplay(styleName);
                if (display != null)
                {
                    display.Hide();
                }
            }
        }

        private void SimulatePlay(string conversationID)
        {
            Debug.Log($"<color=cyan>[DialogueTimelineReceiver] Simulation Mode: Playing conversation '{conversationID}' (No DialogueManager found)</color>");
        }
    }
}
