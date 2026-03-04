using UnityEngine;
using Cinemachine;

public class DynamicConfinerControl : MonoBehaviour
{
    public CinemachineConfiner confiner;
    public Collider[] availableColliders;
    
    private void Update()
    {
        // 例: スペースキーを押すたびに次のコライダーに切り替え
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwitchToNextCollider();
        }
    }

    private int currentColliderIndex = 0;
    private void SwitchToNextCollider()
    {
        if (availableColliders.Length == 0) return;

        currentColliderIndex = (currentColliderIndex + 1) % availableColliders.Length;
        confiner.m_BoundingVolume = availableColliders[currentColliderIndex];
        
        // コライダー変更後、カメラの位置を再計算
        confiner.InvalidatePathCache();
    }

    // 特定の条件に基づいてコライダーを切り替える例
    public void SwitchColliderBasedOnCondition(string condition)
    {
        Collider newCollider = null;
        switch (condition)
        {
            case "indoor":
                newCollider = availableColliders[0]; // 屋内用のコライダー
                break;
            case "outdoor":
                newCollider = availableColliders[1]; // 屋外用のコライダー
                break;
            // 他の条件を追加...
        }

        if (newCollider != null)
        {
            confiner.m_BoundingVolume = newCollider;
            confiner.InvalidatePathCache();
        }
    }
}