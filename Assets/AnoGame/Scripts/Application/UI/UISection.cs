using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

namespace AnoGame.Application.UI
{
    [System.Serializable]
    public class UISection
    {
        public string sectionName;              // 画面名 (例：MainMenu, Settings など)
        public GameObject panel;                // 画面全体のパネル
        public CanvasGroup canvasGroup;         // ← 追加: パネルにアタッチした CanvasGroup をセット
        public List<Selectable> selectables;    // 画面内のボタンなど
        [HideInInspector] public int lastIndex; // 前回選択していたインデックス

        // 画面ごとのカーソルオフセット
        public Vector2 cursorOffset;

        // キャンセル時に呼びたい処理を自由に設定できる
        public UnityEvent onCancel;
    }
}
