using UnityEngine;
namespace AnoGame.Application.Event
{
    public class SaveHandler : MonoBehaviour
    {
        public void Save()
        {
            GameManager2.Instance.SaveData();

        }
    }
}