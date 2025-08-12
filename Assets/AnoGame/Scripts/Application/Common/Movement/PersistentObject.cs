using UnityEngine;

namespace AnoGame.Application.Common
{
    public class PersistentObject : MonoBehaviour
    {
        void Start()
        {
            DontDestroyOnLoad(this);
        }
    }
}