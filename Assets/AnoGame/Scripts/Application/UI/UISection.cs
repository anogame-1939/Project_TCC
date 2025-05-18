using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// 1つの画面(UI)に関する情報をまとめるクラス
    /// </summary>
    [System.Serializable]
    public class UISection
    {
        public string sectionName;              // 画面名(メインメニュー, Settingsなど)
        public GameObject panel;                // その画面全体のパネルオブジェクト
        public List<Selectable> selectables;    // その画面内のボタン等
        [HideInInspector] public int lastIndex; // 前回選択していたインデックス

        // 画面ごとのカーソルオフセット
        public Vector2 cursorOffset;

        // ★ キャンセル時に呼びたい処理を自由に設定できる
        public UnityEvent onCancel;

        public void EnableAllSelectables()
        {
            foreach (var selectable in selectables)
            {
                selectable.interactable = true;
            }
        }
        
        public void DisableAllSelectables()
        {
            foreach (var selectable in selectables)
            {
                selectable.interactable = false;
            }
        }
    }
}
