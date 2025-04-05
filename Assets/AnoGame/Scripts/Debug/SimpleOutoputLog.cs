using UnityEngine;

namespace AnoGame.Application.SLFBDebug
{
    public class SimpleOutputLog : MonoBehaviour
    {
        public void Log()
        {
            Debug.Log("はい");
        }
        public void Log(string message)
        {
            Debug.Log(message);
        }
    }
}