using UnityEngine;

namespace AnoGame.SLFBDebug
{
    public class SimpleOutputLog : MonoBehaviour
    {
        public void Log()
        {
            Debug.Log($"はい-name:{name}", this);
        }
        public void Log(string message)
        {
            Debug.Log($"message:{message}-name:{name}", this);
        }
    }
}