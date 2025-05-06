using UnityEngine;

namespace AnoGame.SLFBDebug
{
    public class SimpleOutputLog : MonoBehaviour
    {
        public void Log()
        {
            Debug.Log($"はい-name:{name}");
        }
        public void Log(string message)
        {
            Debug.Log($"message:{message}-name:{name}");
        }
    }
}