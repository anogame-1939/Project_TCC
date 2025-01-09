using UnityEngine;
namespace AnoGame.Application.Event
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
            GameManager2.Instance.SaveData();

            Debug.Log("保存");

        }
    }
}