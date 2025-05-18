using UnityEngine;

namespace AnoGame.Application.UI
{
    public class PersistentOverlayUI : MonoBehaviour
    {
        void Start()
        {
            DontDestroyOnLoad(this);
        }
    }
}