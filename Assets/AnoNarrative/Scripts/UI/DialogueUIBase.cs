using UnityEngine;
using AnoGame.AnoNarrative.Data;

namespace AnoGame.AnoNarrative.UI
{
    public abstract class DialogueUIBase : MonoBehaviour
    {
        [Header("Style Settings")]
        [SerializeField] protected DialogueStyle dialogueStyle;
        public DialogueStyle Style => dialogueStyle;

        public abstract bool IsDialogueActive { get; }

        protected virtual void Start()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterUI(this, dialogueStyle);
            }
        }

        protected virtual void OnEnable()
        {
            // Re-register if needed when enabled, though Start typically handles it.
            // Safe to call multiple times if Manager handles duplicates.
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterUI(this, dialogueStyle);
            }
        }

        public abstract void ShowConversation(ConversationUnit unit);
        public abstract void PreviewConversation(ConversationUnit unit);
        public abstract void Close();
        public abstract void OnClickNext();
    }
}
