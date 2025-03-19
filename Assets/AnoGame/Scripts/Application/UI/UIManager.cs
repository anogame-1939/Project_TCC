using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AnoGame.Application.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private SelectionCursorController cursorController;

        // メイン画面用の Selectable リスト
        [SerializeField] private List<Selectable> mainMenuSelectables;
        // 設定画面用の Selectable リスト
        [SerializeField] private List<Selectable> settingsSelectables;

        [SerializeField] private GameObject settingsPanel; // 設定画面パネル

        private bool isSettingsOpen = false;

        // メイン画面を初期化
        private void Start()
        {
            // 起動時はメインメニューのリストを設定
            cursorController.SetSelectableObjects(mainMenuSelectables);
            settingsPanel.SetActive(false);
        }

        public void OpenSettings()
        {
            isSettingsOpen = true;
            settingsPanel.SetActive(true);

            // 設定画面用リストを渡す
            cursorController.SetSelectableObjects(settingsSelectables);
        }

        public void CloseSettings()
        {
            isSettingsOpen = false;
            settingsPanel.SetActive(false);

            // メインメニューに戻す
            cursorController.SetSelectableObjects(mainMenuSelectables);
        }
    }

}