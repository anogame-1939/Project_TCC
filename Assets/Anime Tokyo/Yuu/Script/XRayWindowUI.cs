using UnityEngine;
using UnityEngine.UI;

public class XRayWindowUI : MonoBehaviour
{
    [Header("ターゲット")]
    [SerializeField] private Transform player;       // プレイヤー
    [SerializeField] private Camera mainCamera;      // メインカメラ

    [Header("UI参照")]
    [SerializeField] private RectTransform maskRect; // 親のMask (丸い枠)
    [SerializeField] private RectTransform rawImageRect; // 子のRawImage (映像)

    [Header("調整")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.0f, 0); // プレイヤーの少し上を表示

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        
        // RawImageは常に画面サイズと一致させる必要があるため
        // アンカーを中心に設定してリセット
        rawImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        rawImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        rawImageRect.pivot = new Vector2(0.5f, 0.5f);
        
        // 解像度に合わせてサイズ設定（簡易的）
        // ※画面サイズが変わるゲームならUpdate内でやる必要があります
        rawImageRect.sizeDelta = new Vector2(Screen.width, Screen.height);
    }

    void LateUpdate()
    {
        if (player == null || mainCamera == null) return;

        // 1. プレイヤーのワールド座標をスクリーン座標に変換
        Vector3 screenPos = mainCamera.WorldToScreenPoint(player.position + offset);

        // プレイヤーがカメラの後ろにいる場合は表示しない（簡易カリング）
        if (screenPos.z < 0)
        {
            // 画面外に飛ばすなどして隠す
            maskRect.gameObject.SetActive(false);
            return;
        }
        else
        {
            maskRect.gameObject.SetActive(true);
        }

        // 2. Mask（窓）の位置を更新
        // CanvasのRenderModeがOverlayであることを前提としています
        maskRect.position = screenPos;

        // 3. RawImage（中身）の位置補正
        // Maskが動いた分だけ、中身を逆方向に動かして「背景と位置を合わせる」
        // これをしないと、窓の中に縮小された世界が表示されてしまいます
        rawImageRect.anchoredPosition = -maskRect.anchoredPosition;
    }
}