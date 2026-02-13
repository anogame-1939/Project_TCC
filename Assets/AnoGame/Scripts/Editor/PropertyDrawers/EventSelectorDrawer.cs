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
        private static int _lastAssetCount = -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [EventSelector] with string.");
                return;
            }

            // アセット数が変わったらキャッシュをリフレッシュ
            var guids = AssetDatabase.FindAssets("t:EventData");
            if (_cachedIds == null || _cachedDisplayOptions == null || guids.Length != _lastAssetCount)
            {
                LoadData(guids);
            }

            // 現在の選択値のインデックスを探す
            int currentIndex = 0;
            string currentId = property.stringValue;

            if (_cachedIds != null && _cachedIds.Length > 0)
            {
                bool found = false;
                for (int i = 0; i < _cachedIds.Length; i++)
                {
                    if (_cachedIds[i] == currentId)
                    {
                        currentIndex = i;
                        found = true;
                        break;
                    }
                }

                // 現在の値がリストに見つからない場合、警告表示
                if (!found && !string.IsNullOrEmpty(currentId))
                {
                    // 不正な値（インデックス番号など）が入っている場合
                    Debug.LogWarning($"[EventSelector] プロパティ '{label.text}' に不正な値 '{currentId}' が設定されています。ドロップダウンから正しいイベントを選択してください。 (Object: {property.serializedObject.targetObject.name})");
                }

                // ドロップダウン描画
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUI.Popup(position, label.text, currentIndex, _cachedDisplayOptions);

                if (EditorGUI.EndChangeCheck())
                {
                    if (newIndex >= 0 && newIndex < _cachedIds.Length)
                    {
                        property.stringValue = _cachedIds[newIndex];
                    }
                }
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "No EventData assets found.");
            }
        }

        private void LoadData(string[] guids)
        {
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
                    string displayName = string.IsNullOrEmpty(eventData.EventName)
                        ? eventData.name
                        : eventData.EventName;
                    displayList.Add(displayName);
                    idList.Add(eventData.EventId);
                }
            }

            _cachedDisplayOptions = displayList.ToArray();
            _cachedIds = idList.ToArray();
            _lastAssetCount = guids.Length;
        }
    }
}
