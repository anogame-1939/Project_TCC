using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class TreeVisibilityTrigger : MonoBehaviour
{
    [Tooltip("桜の木に設定したタグ名")]
    public string treeTag = "Tree";

    [Tooltip("追従したいプレイヤーオブジェクトのタグ名")]
    public string playerTag = "Player";

    private Transform playerTransform;
    private SphereCollider sphereCol;

    private void Awake()
    {
        // トリガー設定を保証
        // sphereCol = GetComponent<SphereCollider>();
        // phereCol.isTrigger = true;

        // Playerタグのオブジェクトを探してTransformをキャッシュ
        var playerGO = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGO != null)
            playerTransform = playerGO.transform;
        else
            Debug.LogWarning($"[TreeVisibilityTrigger] タグ「{playerTag}」のオブジェクトが見つかりません");
    }

    private void Update()
    {
        // Playerを見つけていれば、自分の位置を常にPlayerに合わせる
        if (playerTransform != null)
            transform.position = playerTransform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(treeTag))
        {
            var rend = other.GetComponent<MeshRenderer>();
            if (rend != null && !rend.enabled)
                rend.enabled = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(treeTag))
        {
            var rend = other.GetComponent<MeshRenderer>();
            if (rend != null && rend.enabled)
                rend.enabled = false;
        }
    }
}
