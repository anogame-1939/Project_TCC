using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// 1つの画面(UI)に関する情報をまとめるクラス
    /// </summary>
    [System.Serializable]
    public class UISection
    {
        public string sectionName;              // 画面名(デバッグや識別用)
        public GameObject panel;                // 該当画面のパネル (非表示にする等)
        public List<Selectable> selectables;    // その画面内のボタンやスライダー等
        [HideInInspector] public int lastIndex; // 前回選択していたインデックス
    }
}
