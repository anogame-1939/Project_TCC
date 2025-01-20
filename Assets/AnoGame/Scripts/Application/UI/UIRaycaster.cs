using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using AnoGame.Application.Inventory;

namespace AnoGame.Application.UI
{
    public class UIRaycaster : MonoBehaviour
    {
        private GraphicRaycaster raycaster;
        private PointerEventData pointerEventData;
        private EventSystem eventSystem;

        void Start()
        {
            // GraphicRaycasterの取得
            raycaster = GetComponent<GraphicRaycaster>();
            
            // EventSystemの取得
            eventSystem = GetComponent<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = FindFirstObjectByType<EventSystem>();
            }
        }

        void Update()
        {
            // マウス位置でレイキャストを実行
            CheckUIElement();
        }

        private void CheckUIElement()
        {
            // マウスの現在位置を取得
            pointerEventData = new PointerEventData(eventSystem);
            pointerEventData.position = Input.mousePosition;

            // レイキャストの結果を格納するリスト
            List<RaycastResult> results = new List<RaycastResult>();

            // レイキャストの実行
            raycaster.Raycast(pointerEventData, results);

            // 結果の処理
            foreach (RaycastResult result in results)
            {
                // UI要素の情報を取得
                GameObject detectedUI = result.gameObject;
                
                // UI要素の種類に応じた処理
                if (detectedUI.GetComponent<InventorySlot>() != null)
                {
                    Debug.Log("InventorySlotを検出: " + detectedUI.name);
                }
                else if (detectedUI.GetComponent<Image>() != null)
                {
                    Debug.Log("画像を検出: " + detectedUI.name);
                }
                else if (detectedUI.GetComponent<Text>() != null)
                {
                    Debug.Log("テキストを検出: " + detectedUI.name);
                }
                
                // 必要に応じて他のUI要素の判定を追加
            }
        }
    }
}