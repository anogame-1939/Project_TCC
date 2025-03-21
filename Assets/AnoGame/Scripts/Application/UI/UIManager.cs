using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace AnoGame.Application.UI
{
    public class UIManager : MonoBehaviour
    {
        // UISectionのリストをインスペクターから設定
        [SerializeField] private List<UISection> sections;
        private int currentSectionIndex = 0;

        private void Start()
        {
            // 起動時はメインメニュー（例：0番）を表示
            ShowSection(0);
            
            foreach (var section in sections)
            {
                section.DisableAllSelectables();
            }
        }

        private void Initialize()
        {
            foreach(var selectable in sections)
            {
                if(selectable != null)
                {
                    // selectable.DisableAllSelectables();
                }
            }
            
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

        /// <summary>
        /// セクション切り替え時に、前のセクションのSelectableを無効化し、新しいセクションのSelectableを有効化します。
        /// また、新しいセクションでは前回の選択状態（lastIndex）を利用してフォーカスを設定します。
        /// </summary>
        /// <param name="index">表示したいセクションのインデックス</param>
        public void ShowSection(int index)
        {
            if(index < 0 || index >= sections.Count)
            {
                Debug.LogWarning("Invalid section index: " + index);
                return;
            }
            SaveCurrentSelectedIndex();

            // 現在のセクションのSelectableを無効化し、パネルを非表示にする
            UISection previousSection = sections[currentSectionIndex];
            SetSelectablesInteractable(previousSection, false);
            previousSection.panel.SetActive(false);

            // 現在のセクションを更新
            currentSectionIndex = index;
            UISection currentSection = sections[currentSectionIndex];

            // 新しいセクションのパネルを表示し、Selectableを有効化
            currentSection.panel.SetActive(true);
            SetSelectablesInteractable(currentSection, true);

            // 前回選択していたSelectableがあればそのオブジェクトにフォーカス、
            // なければリストの先頭を選択
            int indexToSelect = currentSection.lastIndex;
            if(indexToSelect < 0 || indexToSelect >= currentSection.selectables.Count)
            {
                indexToSelect = 0;
            }
            if(currentSection.selectables.Count > 0 && currentSection.selectables[indexToSelect] != null)
            {
                currentSection.selectables[indexToSelect].Select();
                EventSystem.current.SetSelectedGameObject(currentSection.selectables[indexToSelect].gameObject);
            }
        }

        /// <summary>
        /// 指定されたUISection内のSelectable群のinteractable状態を切り替えます
        /// </summary>
        private void SetSelectablesInteractable(UISection section, bool interactable)
        {
            foreach(var selectable in section.selectables)
            {
                if(selectable != null)
                {
                    selectable.interactable = interactable;
                }
            }
        }

        /// <summary>
        /// 現在フォーカスされているSelectableのインデックスを保存します
        /// （セクション切り替え前に呼び出して、後で戻る際に利用します）
        /// </summary>
        private void SaveCurrentSelectedIndex()
        {
            UISection currentSection = sections[currentSectionIndex];
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            if(currentSelected != null)
            {
                int index = currentSection.selectables.FindIndex(s => s.gameObject == currentSelected);
                if(index >= 0)
                {
                    currentSection.lastIndex = index;
                }
            }
        }

        /// <summary>
        /// メインメニューから設定画面に移動する際の処理例
        /// </summary>
        public void OpenSettings()
        {
            // 現在のセクション（メインメニュー）の最後の選択状態を保存
            SaveCurrentSelectedIndex();
            // 設定画面（例：1番目）へ切り替え
            ShowSection(1);
        }

        /// <summary>
        /// 設定画面などでキャンセルボタンが押されたときの処理例
        /// </summary>
        public void BackToMain()
        {
            ShowSection(0);
        }
    }
}