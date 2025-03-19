using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

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
            StartCoroutine(StartCor());
        }

        /// <summary>
        /// UIの初期化がバグるので1フレーム遅らせる
        /// </summary>
        /// <returns></returns>
        private IEnumerator StartCor()
        {
            yield return null;
            // 例: 起動時に 0 番目 (メインメニュー想定) を開く
            OpenSection(0);

            // 残りは非表示
            for (int i = 0; i < uiSections.Count; i++)
            {
                if (i == 0)
                {
                    continue;
                }

                var section = uiSections[i];
                if (section.panel != null)
                {
                    section.panel.SetActive(false);
                }
                
            }

            // 起動時は「選択モード」を有効, 「スクロールバー操作モード」は無効
            cursorController.enabled = true;
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

        public void OpenScrollbarMode(Scrollbar sb)
        {
            // 選択モードを無効化
            cursorController.enabled = false;

            // スクロールバー操作モードを有効化
        }

        public void CloseScrollbarMode()
        {

            // 選択モードを再度有効化
            cursorController.enabled = true;
        }

        //========================
        // Dropdownを別UISection扱いにする場合
        //========================

        public void OpenDropdownSection(/*...*/)
        {
            // 例: selectionCursorController.SetSelectableObjects(dropdownItems, 0, offset);
            // またはUISectionを使ってOpenSection("DropdownMenu")等
        }

        public void CloseDropdownSection(/*...*/)
        {
            // メイン画面に戻る etc
        }
    }
}
