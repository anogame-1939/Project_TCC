using UnityEngine;
using System.Collections.Generic;

namespace AnoGame.Application.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private SelectionCursorController cursorController;

        // 画面ごとにまとめた UISection をリストや配列で持つ
        [SerializeField] private List<UISection> uiSections;

        // 現在アクティブなセクションを指す
        private UISection currentSection;

        private void Start()
        {
            // 例として、最初にメインメニュー(0番目)を開く
            // リストの先頭をメインメニューと仮定
            OpenSection(0);
        }

        /// <summary>
        /// 指定したインデックスの UISection を開く
        /// </summary>
        public void OpenSection(int sectionIndex)
        {
            if (sectionIndex < 0 || sectionIndex >= uiSections.Count)
            {
                Debug.LogWarning("Invalid section index: " + sectionIndex);
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

            // 新たに開くセクションをセット
            currentSection = uiSections[sectionIndex];

            // 新セクションのパネルを表示
            if (currentSection.panel != null)
            {
                currentSection.panel.SetActive(true);
            }

            // SelectionCursorController に対して、以前の lastIndex で表示する
            cursorController.SetSelectableObjects(
                currentSection.selectables,
                currentSection.lastIndex
            );
        }

        /// <summary>
        /// セクション名で開く場合の例
        /// </summary>
        public void OpenSection(string sectionName)
        {
            // 名前で検索して OpenSection(index) を呼ぶなど
            int index = uiSections.FindIndex(s => s.sectionName == sectionName);
            if (index >= 0)
            {
                OpenSection(index);
            }
            else
            {
                Debug.LogWarning("Section not found: " + sectionName);
            }
        }

        /// <summary>
        /// 設定画面を開く
        /// </summary>
        public void OpenSettings()
        {
            // 例: "Settings" という名前のセクションを探して開く
            OpenSection("Settings");
        }

        /// <summary>
        /// メインメニューに戻る
        /// </summary>
        public void OpenMainMenu()
        {
            OpenSection("MainMenu");
        }
    }
}
