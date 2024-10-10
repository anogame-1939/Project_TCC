using UnityEngine;

public class ConfinerTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        ConfinerManager.Instance.SwitchConfiner(other.GetComponent<Collider>());
    }
}