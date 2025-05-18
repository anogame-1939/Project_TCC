using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;

namespace AnoGame.DialogueFeatures
{
    public class LocationDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private Sprite locationSprite;
        private DialogueSystemTrigger dialogueTrigger;

        private void Start()
        {
            dialogueTrigger = GetComponent<DialogueSystemTrigger>();
            if (dialogueTrigger != null)
            {
                dialogueTrigger.onExecute.AddListener(HandleConversationStart);
            }
            else
            {
                Debug.LogWarning("DialogueSystemTrigger not found on GameObject", this);
            }
        }

        private void HandleConversationStart(GameObject actor)
        {
            if (locationSprite != null)
            {
                LocationViewManager.Instance.ShowLocation(locationSprite);
            }
        }

        private void OnDestroy()
        {
            if (dialogueTrigger != null)
            {
                dialogueTrigger.onExecute.RemoveListener(HandleConversationStart);
            }
        }
    }
}