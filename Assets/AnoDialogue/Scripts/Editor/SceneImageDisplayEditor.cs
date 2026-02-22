using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;

namespace AnoGame.AnoDialogue.Editor
{
    [CustomEditor(typeof(UI.SceneImageDisplay))]
    public class SceneImageDisplayEditor : UnityEditor.Editor
    {
        private Data.DialogueStyle _cachedStyleDb;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // ── Style Dropdown ──
            var styleSection = new VisualElement();
            styleSection.style.marginBottom = 4;
            root.Add(styleSection);

            // DialogueStyle DB を自動検索
            if (_cachedStyleDb == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:DialogueStyle");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _cachedStyleDb = AssetDatabase.LoadAssetAtPath<Data.DialogueStyle>(path);
                }
            }

            var styleNameProp = serializedObject.FindProperty("styleName");
            if (_cachedStyleDb != null && _cachedStyleDb.styleNames != null && _cachedStyleDb.styleNames.Count > 0)
            {
                var list = _cachedStyleDb.styleNames.ToList();
                int index = list.IndexOf(styleNameProp.stringValue);
                if (index < 0)
                {
                    index = 0;
                    // 初期値が空または未登録の場合、最初のスタイルを自動設定
                    styleNameProp.stringValue = list[0];
                    serializedObject.ApplyModifiedProperties();
                }

                var dropdown = new DropdownField("Style Name", list, index);
                dropdown.RegisterValueChangedCallback(evt =>
                {
                    styleNameProp.stringValue = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                });
                styleSection.Add(dropdown);
            }
            else
            {
                // DB が見つからない場合はテキストフィールドにフォールバック
                var fallback = new PropertyField(styleNameProp, "Style Name");
                styleSection.Add(fallback);

                if (_cachedStyleDb == null)
                {
                    styleSection.Add(new HelpBox("DialogueStyle asset not found. Using text input.", HelpBoxMessageType.Warning));
                }
            }

            // ── Image ──
            var imgProp = serializedObject.FindProperty("targetImage");
            if (imgProp != null)
            {
                root.Add(new PropertyField(imgProp, "Target Image"));
            }

            // ── Fade ──
            var fadeProp = serializedObject.FindProperty("fadeDuration");
            if (fadeProp != null)
            {
                root.Add(new PropertyField(fadeProp, "Fade Duration"));
            }

            return root;
        }
    }
}
