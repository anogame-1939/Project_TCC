using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AnoGame.Application.Attributes;
using AnoGame.Data;

namespace AnoGame.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ItemSelectorAttribute))]
    public class ItemSelectorDrawer : PropertyDrawer
    {
        private static string[] _cachedIds;
        private static string[] _cachedDisplayOptions;
        private static int _lastAssetCount = -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [ItemSelector] with string.");
                return;
            }

            // アセット数が変わったらキャッシュをリフレッシュ
            var guids = AssetDatabase.FindAssets("t:ItemData");
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
                    Debug.LogWarning($"[ItemSelector] プロパティ '{label.text}' に不正な値 '{currentId}' が設定されています。ドロップダウンから正しいアイテムを選択してください。 (Object: {property.serializedObject.targetObject.name})");
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
                EditorGUI.LabelField(position, label.text, "No ItemData assets found.");
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
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

                if (item != null)
                {
                    string displayName = string.IsNullOrEmpty(item.ItemName)
                        ? item.name
                        : item.ItemName;
                    displayList.Add(displayName);
                    idList.Add(item.ItemId);
                }
            }

            _cachedDisplayOptions = displayList.ToArray();
            _cachedIds = idList.ToArray();
            _lastAssetCount = guids.Length;
        }
    }
}
