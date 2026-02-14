using UnityEngine;
using AnoGame.AnoDialogue.Data;

namespace AnoGame.AnoDialogue.UI
{
    public abstract class DialogueUIBase : MonoBehaviour
    {
        [Header("Style Settings")]
        [Tooltip("Reference to the DialogueStyle DB to validate names.")]
        [SerializeField] protected DialogueStyle styleData;

        [Tooltip("The name of the style this UI handles (e.g. 'Standard', 'Narration')")]
        [SerializeField] protected string styleName = "Standard";

        public DialogueStyle StyleData => styleData;
        public string StyleName => styleName;

        public abstract bool IsDialogueActive { get; }

        protected virtual void Start()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterUI(this, styleName);
            }
        }

        protected virtual void OnEnable()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterUI(this, styleName);
            }
        }

        public abstract void ShowConversation(ConversationUnit unit);
        public abstract void PreviewConversation(ConversationUnit unit);
        public abstract void Close();
        public abstract void OnClickNext();
    }
}
