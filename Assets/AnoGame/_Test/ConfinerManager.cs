using UnityEngine;
using Cinemachine;

public class ConfinerManager : MonoBehaviour
{
    public static ConfinerManager Instance { get; private set; }

    public CinemachineVirtualCamera virtualCamera;
    private CinemachineConfiner confiner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        confiner = virtualCamera.GetComponent<CinemachineConfiner>();
        if (confiner == null)
        {
            confiner = virtualCamera.gameObject.AddComponent<CinemachineConfiner>();
        }
    }

    public void SwitchConfiner(Collider newConfiner)
    {
        if (confiner != null && newConfiner != null)
        {
            confiner.m_BoundingVolume = newConfiner;
            confiner.InvalidatePathCache();
            Debug.Log($"Switched confiner to: {newConfiner.name}");
        }
    }
}