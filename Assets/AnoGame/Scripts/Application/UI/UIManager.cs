using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AnoGame.Application.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private SelectionCursorController cursorController;

        [SerializeField] private List<Selectable> mainMenuSelectables;
        [SerializeField] private List<Selectable> settingsSelectables;

        [SerializeField] private GameObject settingsPanel; // 設定画面パネル

        private bool isSettingsOpen = false;

        // ★ メイン画面の “前回の選択インデックス” を保存する変数
        private int 
         = 0;

        private void Start()
        {
            // 起動時はメインメニューのリストを設定
            cursorController.SetSelectableObjects(mainMenuSelectables, 0);
            settingsPanel.SetActive(false);
        }

        public void OpenSettings()
        {
            isSettingsOpen = true;
            settingsPanel.SetActive(true);

            // 1) メインメニューの現在インデックスを退避
            lastMainMenuIndex = cursorController.GetCurrentIndex();

            // 2) 設定画面用リストを渡す（最初は0番目を選択しても良いし、別途記憶してもOK）
            cursorController.SetSelectableObjects(settingsSelectables, 0);
        }

        public void CloseSettings()
        {
            isSettingsOpen = false;
            settingsPanel.SetActive(false);

            // 3) メインメニューに戻すとき、前に記憶したインデックスでリストを復元
            cursorController.SetSelectableObjects(mainMenuSelectables, lastMainMenuIndex);
        }
    }
}
