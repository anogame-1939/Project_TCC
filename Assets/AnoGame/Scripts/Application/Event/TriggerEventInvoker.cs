using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TriggerEventInvoker : MonoBehaviour
{
    public UnityEvent onTriggerEnter;

    private void OnTriggerEnter(Collider other)
    {
        // 何らかの条件で判定する
        if (other.CompareTag("Player"))
        {
            // UnityEvent を呼び出す
            onTriggerEnter?.Invoke();
        }
    }
}