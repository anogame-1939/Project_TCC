using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Navigation を操作するために必要
using System.Collections.Generic;

namespace AnoGame.Application.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private List<UISection> sections;
        [SerializeField] private bool startToShow = false;
        private int currentSectionIndex = 0;

        // 「Navigation を元に戻す」ために、各セクションの Selectable ごとに元の Navigation を保存する辞書
        private Dictionary<Selectable, Navigation> originalNavigations = new Dictionary<Selectable, Navigation>();

        // ------------------------------------------------------------
        private void Start()
        {
            // 起動時は、すべてのセクションを「操作不可＆非フォーカス状態」にしておく
            foreach (var section in sections)
            {
                DisableSectionInteraction(section);
                section.panel.SetActive(false);
                section.lastIndex = 0; // 初期化
            }

            if (startToShow)
            {
                ShowSection(0);
            }
        }

        // ------------------------------------------------------------
        private GameObject lastSelected = null;

        private void Update()
        {
            var current = EventSystem.current.currentSelectedGameObject;
            if (current != lastSelected)
            {
                lastSelected = current;
                if (current != null)
                {
                    Debug.Log("Selected object: " + current.name);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// セクション切り替えメソッド
        /// </summary>
        /// <param name="index">表示したいセクションのインデックス</param>
        public void ShowSection(int index)
        {
            if (index < 0 || index >= sections.Count)
            {
                Debug.LogWarning("Invalid section index: " + index);
                return;
            }

            // 1) 現在のセクション情報を保存してから操作不可にする
            SaveCurrentSelectedIndex(); // フォーカスされている Selectable の index を保存

            UISection previousSection = sections[currentSectionIndex];
            if (previousSection != null)
            {
                // ── 操作を受け付けないように CanvasGroup を切り替え ──
                DisableSectionInteraction(previousSection);
                // ── Navigation を None にしておく ──
                DisableNavigation(previousSection);
                // ── パネルは Active のままにする（見た目だけ残す）
                // previousSection.panel.SetActive(false); // 削除：非アクティブ化しない
            }

            // 2) 新しいセクションをアクティブ化して操作を許可
            currentSectionIndex = index;
            UISection currentSection = sections[currentSectionIndex];

            if (!currentSection.panel.activeSelf)
                currentSection.panel.SetActive(true);

            EnableSectionInteraction(currentSection);
            RestoreNavigation(currentSection);

            // 3) フォーカス設定: lastIndex が正しければそのボタンを選択、なければ先頭
            int indexToSelect = currentSection.lastIndex;
            if (indexToSelect < 0 || indexToSelect >= currentSection.selectables.Count)
                indexToSelect = 0;

            if (currentSection.selectables.Count > 0 && currentSection.selectables[indexToSelect] != null)
            {
                var toSelect = currentSection.selectables[indexToSelect];
                toSelect.Select();
                EventSystem.current.SetSelectedGameObject(toSelect.gameObject);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 指定したインデックスのセクションを「操作不可＋Navigation 無効＋非アクティブ化」する
        /// </summary>
        public void HideSection(int index)
        {
            if (index < 0 || index >= sections.Count)
            {
                Debug.LogWarning("Invalid section index: " + index);
                return;
            }

            UISection target = sections[index];

            // ① フォーカスされている Selectable の index を保存しておく（戻るときに使いたい場合）
            if (currentSectionIndex == index)
            {
                SaveCurrentSelectedIndex();
            }

            // ② 操作不可にする
            DisableSectionInteraction(target);

            // ③ Navigation を None にする
            DisableNavigation(target);

            // ④ パネルを非アクティブ化して見た目を消す
            if (target.panel.activeSelf)
            {
                Debug.Log("target.panel.activeSelf:" + target.panel.name);
                target.panel.SetActive(false);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 指定した UISection を「操作不可＆レイキャストも受け付けない」状態にする
        /// CanvasGroup を通して実現。見た目はそのまま残る
        /// </summary>
        private void DisableSectionInteraction(UISection section)
        {
            if (section.canvasGroup == null)
            {
                Debug.LogWarning($"CanvasGroup が設定されていません: {section.sectionName}");
                return;
            }

            // すべての子 UI（Selectable を含む）を操作不可にする
            section.canvasGroup.interactable = false;
            // クリックやタップ、ポインターが当たらないようにする
            section.canvasGroup.blocksRaycasts = false;
            // ※ 念のため alpha は変更しないので、色味はそのまま
        }

        /// <summary>
        /// 指定した UISection を「操作可能＆レイキャストを受け付ける」状態に戻す
        /// </summary>
        private void EnableSectionInteraction(UISection section)
        {
            if (section.canvasGroup == null)
            {
                Debug.LogWarning($"CanvasGroup が設定されていません: {section.sectionName}");
                return;
            }

            section.canvasGroup.interactable = true;
            section.canvasGroup.blocksRaycasts = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 現在フォーカスされている Selectable のインデックスを保存する
        /// </summary>
        private void SaveCurrentSelectedIndex()
        {
            UISection currentSection = sections[currentSectionIndex];
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            if (currentSelected == null) return;

            int idx = currentSection.selectables.FindIndex(s => s != null && s.gameObject == currentSelected);
            if (idx >= 0)
            {
                currentSection.lastIndex = idx;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 指定した UISection 内のすべての Selectable の Navigation を None にして
        /// キーボード/ゲームパッドによるフォーカス移動を止める。元の設定は辞書に保存。
        /// </summary>
        private void DisableNavigation(UISection section)
        {
            foreach (var selectable in section.selectables)
            {
                if (selectable == null) continue;

                // 元の Navigation を保存しておく
                if (!originalNavigations.ContainsKey(selectable))
                {
                    originalNavigations[selectable] = selectable.navigation;
                }

                // Navigation を完全にオフ
                Navigation nav = selectable.navigation;
                nav.mode = Navigation.Mode.None;
                selectable.navigation = nav;
            }
        }

        /// <summary>
        /// 指定した UISection の Selectable について、保存しておいた
        /// Navigation 設定を復元する
        /// </summary>
        private void RestoreNavigation(UISection section)
        {
            foreach (var selectable in section.selectables)
            {
                if (selectable == null) continue;

                if (originalNavigations.ContainsKey(selectable))
                {
                    selectable.navigation = originalNavigations[selectable];
                }
                // ※ もし辞書に保存がない場合は、そもそも初期化がされていないので何もしない
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// メインメニューから設定画面に移動する例
        /// </summary>
        public void OpenSettings()
        {
            SaveCurrentSelectedIndex();
            ShowSection(1);
        }

        /// <summary>
        /// 設定画面からメインに戻る例
        /// </summary>
        public void BackToMain()
        {
            ShowSection(0);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 現在のセクションに設定された onCancel (UnityEvent) を実行する
        /// </summary>
        public void InvokeCurrentSectionOnCancel()
        {
            if (currentSectionIndex < 0 || currentSectionIndex >= sections.Count)
                return;

            var section = sections[currentSectionIndex];
            if (section != null && section.onCancel != null)
            {
                section.onCancel.Invoke();
            }
        }

        public void Quit()
        {
#if UNITY_EDITOR
            // エディタ再生を停止
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // ビルドした実行ファイルを終了
            Application.Quit();
#endif
        }
    }
}
