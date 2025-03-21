using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> panels; // 複数の画面をまとめたリスト
    private int currentPanelIndex = 0;

    private void Start()
    {
        // 起動時にメイン画面(0番)を開く
        ShowPanel(0);
    }

    private GameObject lastSelected = null;

    void Update()
    {
        // 現在 EventSystem が選択しているUIオブジェクト
        var current = EventSystem.current.currentSelectedGameObject;

        // もし前回と違うオブジェクトを選択していたらログを出す
        if (current != lastSelected)
        {
            lastSelected = current;
            if (current != null)
            {
                Debug.Log("Selected object: " + current.name);
            }
        }
    }
    
    public void ShowPanel(int index)
    {
        if (index < 0 || index >= panels.Count)
        {
            Debug.LogWarning($"Invalid panel index: {index}");
            return;
        }

        // 現在のパネルを非表示
        if (currentPanelIndex >= 0 && currentPanelIndex < panels.Count)
        {
            panels[currentPanelIndex].SetActive(false);
        }

        currentPanelIndex = index;
        panels[currentPanelIndex].SetActive(true);

        // そのパネル内の「最初に選択しておきたいUI」(例: ボタン)を探す
        var defaultButton = panels[currentPanelIndex].GetComponentInChildren<Button>();
        if (defaultButton != null)
        {
            // デフォルト選択を設定
            defaultButton.Select();
            // または
            // EventSystem.current.SetSelectedGameObject(defaultButton.gameObject);
        }
    }

    // 例: 別のパネルを開くメソッド
    public void OpenSettingsPanel()
    {
        ShowPanel(1); // 1番目が設定画面想定
    }

    // 例: 現在のパネルを閉じてメインに戻る
    public void CloseCurrentAndOpenMain()
    {
        ShowPanel(0); // 0番目がメインメニュー想定
    }
}
