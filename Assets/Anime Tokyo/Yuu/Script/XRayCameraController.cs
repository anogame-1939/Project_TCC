using System.Collections.Generic;
using UnityEngine;

public class XRayCameraController : MonoBehaviour
{
    // ==========================================
    // 1. カメラ追従設定
    // ==========================================
    [Header("【追従設定】")]
    [Tooltip("追従するターゲット（Main Cameraなどを指定）")]
    [SerializeField] private Camera targetCamera;

    // ==========================================
    // 2. 障害物透過設定
    // ==========================================
    [Header("【透過設定】")]
    [Tooltip("プレイヤーのTransform")]
    [SerializeField] private Transform player;
    
    [Tooltip("透過判定を行う対象レイヤー（Wall, Obstacleなど）")]
    [SerializeField] private LayerMask obstacleLayers;

    [Tooltip("隠す際に割り当てるレイヤー名（事前にLayersで作成しておくこと）")]
    [SerializeField] private string hiddenLayerName = "HiddenFromXRay";
    
    [Tooltip("穴の判定の太さ（半径）")]
    [SerializeField] private float checkRadius = 0.5f;

    // 内部変数
    private Camera myCam;
    private int hiddenLayerID;
    // 隠したオブジェクトと、その元のレイヤーIDを覚えておく辞書
    private Dictionary<GameObject, int> hiddenObjects = new Dictionary<GameObject, int>();

    void Start()
    {
        myCam = GetComponent<Camera>();

        // ターゲット未設定ならメインカメラを自動取得
        if (targetCamera == null) targetCamera = Camera.main;

        // 隠しレイヤーのID取得
        hiddenLayerID = LayerMask.NameToLayer(hiddenLayerName);
        if (hiddenLayerID == -1)
        {
            Debug.LogError($"エラー: レイヤー '{hiddenLayerName}' が見つかりません。UnityのLayers設定で作成してください。");
            this.enabled = false;
        }
    }

    void LateUpdate()
    {
        // ------------------------------------------
        // A. カメラの追従処理
        // ------------------------------------------
        if (targetCamera != null)
        {
            transform.position = targetCamera.transform.position;
            transform.rotation = targetCamera.transform.rotation;
            if (myCam != null) myCam.fieldOfView = targetCamera.fieldOfView;
        }

        // ------------------------------------------
        // B. 障害物の透過処理
        // ------------------------------------------
        HandleObstacleTransparency();
    }

    void HandleObstacleTransparency()
    {
        if (player == null) return;

        // カメラからプレイヤーへの方向と距離
        Vector3 direction = player.position - transform.position;
        float distance = direction.magnitude;

        // 【修正】ターゲットのレイヤーに加えて、「現在隠しているレイヤー」も検索対象に含める（ビット演算で合成）
        int combinedMask = obstacleLayers.value | (1 << hiddenLayerID);

        // 太いビーム（SphereCast）で障害物を検出
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, checkRadius, direction, distance, combinedMask);
        
        // 今回ヒットしたオブジェクト一覧
        HashSet<GameObject> currentHitObjects = new HashSet<GameObject>();

        // 1. 新しく見つかった障害物を隠す
        foreach (var hit in hits)
        {
            GameObject obj = hit.collider.gameObject;

            // まだ隠していなければ処理
            if (!hiddenObjects.ContainsKey(obj))
            {
                hiddenObjects.Add(obj, obj.layer); // 元のレイヤーを保存
                SetLayerRecursive(obj, hiddenLayerID); // 隠しレイヤーに変更
            }
            currentHitObjects.Add(obj);
        }

        // 2. ヒットしなくなった（隠す必要がなくなった）ものを元に戻す
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var kvp in hiddenObjects)
        {
            GameObject obj = kvp.Key;
            int originalLayer = kvp.Value;

            // 今回のヒットリストに含まれていない = もう邪魔じゃない
            if (obj == null || !currentHitObjects.Contains(obj))
            {
                if (obj != null)
                {
                    SetLayerRecursive(obj, originalLayer); // 元のレイヤーに戻す
                }
                toRemove.Add(obj); // 削除リストに追加
            }
        }

        // 辞書から削除
        foreach (var obj in toRemove)
        {
            hiddenObjects.Remove(obj);
        }
    }

    // 子オブジェクトも含めてレイヤーを変える（再帰処理）
    void SetLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    // デバッグ用の可視化
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, player.position);
            Gizmos.DrawWireSphere(player.position, checkRadius);
        }
    }
}