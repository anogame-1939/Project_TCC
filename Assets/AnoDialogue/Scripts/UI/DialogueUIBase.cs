using UnityEngine;
using AnoGame.AnoDialogue.Data;

namespace AnoGame.AnoDialogue.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class DialogueUIBase : MonoBehaviour
    {
        [Header("Style Settings")]
        [Tooltip("Reference to the DialogueStyle DB to validate names.")]
        [SerializeField] protected DialogueStyle styleData;

        [Tooltip("The name of the style this UI handles (e.g. 'Standard', 'Narration')")]
        [SerializeField] protected string styleName = "Standard";

        public DialogueStyle StyleData => styleData;
        public string StyleName => styleName;

        private CanvasGroup _canvasGroup;

        /// <summary>
        /// CanvasGroup の blocksRaycasts で判定。
        /// </summary>
        public bool IsDialogueActive => _canvasGroup != null && _canvasGroup.blocksRaycasts;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            SetPanelActive(false);
        }

        protected virtual void Start()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterUI(this, styleName);
            }
        }

        /// <summary>
        /// CanvasGroup の alpha / interactable / blocksRaycasts を一括制御。
        /// </summary>
        protected void SetPanelActive(bool isActive)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) return;

            _canvasGroup.alpha = isActive ? 1f : 0f;
            _canvasGroup.interactable = isActive;
            _canvasGroup.blocksRaycasts = isActive;
        }

        public abstract void ShowConversation(ConversationUnit unit);
        public abstract void PreviewConversation(ConversationUnit unit);
        public abstract void Close();
        public abstract void OnClickNext();
    }
}
