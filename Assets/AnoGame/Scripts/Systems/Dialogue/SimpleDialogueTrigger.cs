using UnityEngine;
using UnityEngine.Events;

namespace AnoGame.Systems.Dialogue
{
    public class SimpleDialogueTrigger : MonoBehaviour
    {
        [Tooltip("The ID of the conversation to start (e.g. Story1_Intro_1)")]
        public string ConversationID;

        [Tooltip("If true, triggers automatically on Start")]
        public bool TriggerOnStart = false;

        public UnityEvent OnConversationStart;
        public UnityEvent OnConversationEnd;

        private void Start()
        {
            if (TriggerOnStart)
            {
                Trigger();
            }
        }

        public void Trigger()
        {
            if (string.IsNullOrEmpty(ConversationID))
            {
                UnityEngine.Debug.LogWarning("[SimpleDialogueTrigger] No ConversationID assigned.", this);
                return;
            }

            DialogueManager.Instance.StartConversation(ConversationID);
            OnConversationStart?.Invoke();
        }
    }
}
