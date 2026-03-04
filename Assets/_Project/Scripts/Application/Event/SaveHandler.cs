using AnoGame.Application.Story;
using AnoGame.Application.Story.Manager;
using UnityEngine;

namespace AnoGame.Application
{
    public class SaveHandler : MonoBehaviour
    {
        [SerializeField]
        bool _isAutoSave = false;

        void Start()
        {
            if (_isAutoSave)
            {
                Save();
            }
        }

        public void Save()
        {
            StoryStateManager.Instance.UpdatePlayerPosition();
            GameManager.Instance.SaveData();

            Debug.Log("保存");

        }
    }
}