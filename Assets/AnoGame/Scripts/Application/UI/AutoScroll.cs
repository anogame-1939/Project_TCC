using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AnoGame.Application.UI
{
    public class AutoScroll : MonoBehaviour
    {
        public ScrollRect scrollRect;   // ScrollRect コンポーネントへの参照
        public float scrollSpeed = 0.1f;  // スクロール速度（調整可能）

        void Update()
        {
            // verticalNormalizedPosition が 0 になるまでスクロール（1→0）
            if (scrollRect.verticalNormalizedPosition > 0)
            {
                scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;
                // 0 以下にならないように調整
                if (scrollRect.verticalNormalizedPosition < 0)
                    scrollRect.verticalNormalizedPosition = 0;
            }
        }
    }

}