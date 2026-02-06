using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AnoGame.Application.Attributes;
using AnoGame.Data;

namespace AnoGame.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(EventSelectorAttribute))]
    public class EventSelectorDrawer : PropertyDrawer
    {
        private static string[] _cachedIds;
        private static string[] _cachedDisplayOptions;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [EventSelector] with string.");
                return;
            }

            // キャッシュがない場合のみロード
            if (_cachedIds == null || _cachedDisplayOptions == null)
            {
                LoadData();
            }

            // 現在の選択値のインデックスを探す
            int currentIndex = 0;
            string currentId = property.stringValue;

            // キャッシュが有効な場合のみ処理
            if (_cachedIds != null)
            {
                for (int i = 0; i < _cachedIds.Length; i++)
                {
                    if (_cachedIds[i] == currentId)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                // ドロップダウン描画
                int newIndex = EditorGUI.Popup(position, label.text, currentIndex, _cachedDisplayOptions);

                if (newIndex != currentIndex && newIndex >= 0 && newIndex < _cachedIds.Length)
                {
                    property.stringValue = _cachedIds[newIndex];
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }

        private void LoadData()
        {
            // EventDataアセットをすべて検索
            var guids = AssetDatabase.FindAssets("t:EventData");

            var displayList = new List<string>();
            var idList = new List<string>();

            displayList.Add("None");
            idList.Add("");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);

                if (eventData != null)
                {
                    // 名前 + (ID) の形式で表示
                    string displayName = string.IsNullOrEmpty(eventData.EventName) ? eventData.name : eventData.EventName;
                    displayList.Add($"{displayName} ({eventData.EventId})");
                    idList.Add(eventData.EventId);
                }
            }

            _cachedDisplayOptions = displayList.ToArray();
            _cachedIds = idList.ToArray();
        }
    }
}
