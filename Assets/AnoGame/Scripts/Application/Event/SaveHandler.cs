using UnityEngine;
namespace AnoGame.Application.Event
{
    public class SaveHandler : MonoBehaviour
    {
        public void Save()
        {
            GameManager.Instance.SaveData();

        }
    }
}