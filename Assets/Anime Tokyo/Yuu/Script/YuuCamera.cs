using UnityEngine;

public class YuuCamera : MonoBehaviour
{
    [Header("追従対象")]
    [SerializeField] private Transform target;

    [Header("位置オフセット")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -10f);

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;
    }
}