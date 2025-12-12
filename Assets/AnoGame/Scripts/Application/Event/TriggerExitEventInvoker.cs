using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TriggerExitEventInvoker : MonoBehaviour
{
    public UnityEvent onTriggerExit;

    private void OnTriggerExit(Collider other)
    {
        // 何らかの条件で判定する
        if (other.CompareTag("Player"))
        {
            // UnityEvent を呼び出す
            onTriggerExit?.Invoke();
        }
    }
}
