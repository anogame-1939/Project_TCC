using UnityEngine;
using System.Collections.Generic;

namespace AnoGame.Application.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private SelectionCursorController cursorController;

        // 複数のUI画面(メインメニュー, 設定画面など)をまとめて管理
        [SerializeField] private List<UISection> uiSections;

        // 現在アクティブな画面
        private UISection currentSection;

        private void Start()
        {
            // 例: 起動時に 0 番目 (メインメニュー想定) を開く
            OpenSection(0);
        }

        /// <summary>
        /// インデックス指定でUI画面を開く
        /// </summary>
        public void OpenSection(int sectionIndex)
        {
            if (sectionIndex < 0 || sectionIndex >= uiSections.Count)
            {
                Debug.LogWarning($"Invalid section index: {sectionIndex}");
                return;
            }

            // 今のセクションを閉じる
            if (currentSection != null)
            {
                // 1) 現在のカーソル位置を記憶
                currentSection.lastIndex = cursorController.GetCurrentIndex();
                // 2) パネルを非表示
                if (currentSection.panel != null)
                {
                    currentSection.panel.SetActive(false);
                }
            }

            // 新しいセクションをアクティブに
            currentSection = uiSections[sectionIndex];

            // パネルを表示
            if (currentSection.panel != null)
            {
                currentSection.panel.SetActive(true);
            }

            // SelectionCursorController に “selectables” と “lastIndex”, “cursorOffset”, “UISection自体” を渡す
            cursorController.SetUISection(currentSection);
        }

        /// <summary>
        /// セクション名で画面を開くオーバーロード
        /// </summary>
        public void OpenSection(string sectionName)
        {
            int index = uiSections.FindIndex(s => s.sectionName == sectionName);
            if (index >= 0)
            {
                OpenSection(index);
            }
            else
            {
                Debug.LogWarning($"Section not found: {sectionName}");
            }
        }

        /// <summary>
        /// 現在開いている画面を閉じて、別の画面に戻る例
        /// </summary>
        public void CloseCurrentSection()
        {
            if (currentSection != null && currentSection.panel != null)
            {
                // カーソル位置を記憶しておく
                currentSection.lastIndex = cursorController.GetCurrentIndex();
                currentSection.panel.SetActive(false);
            }

            // 例: 強制的に "MainMenu" という名前のセクションを開く
            OpenSection("MainMenu");
        }
    }
}
