using UnityEngine;
using UnityEngine.UI;

namespace AnoGame.DialogueFeatures
{
    public class LocationViewManager : MonoBehaviour
    {
        private static LocationViewManager instance;
        public static LocationViewManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<LocationViewManager>();
                }
                return instance;
            }
        }

        private LocationViewDialogueUI dialogueUI;

        public void Initialize(LocationViewDialogueUI ui)
        {
            dialogueUI = ui;
        }

        public void ShowLocation(Sprite locationSprite)
        {
            if (dialogueUI != null)
            {
                dialogueUI.ShowLocation(locationSprite);
            }
        }
    }
}