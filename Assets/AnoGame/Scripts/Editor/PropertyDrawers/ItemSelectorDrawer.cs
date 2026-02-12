using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using AnoGame.Application.Attributes;
using AnoGame.Data;

namespace AnoGame.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ItemSelectorAttribute))]
    public class ItemSelectorDrawer : PropertyDrawer
    {
        private static string[] _cachedIds;
        private static string[] _cachedDisplayOptions;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [ItemSelector] with string.");
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
            var guids = AssetDatabase.FindAssets("t:ItemData");

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
                    // 名前 + (ID) の形式で表示
                    string displayName = string.IsNullOrEmpty(item.ItemName) ? item.name : item.ItemName;
                    displayList.Add($"{displayName} ({item.ItemId})");
                    idList.Add(item.ItemId);
                }
            }

            _cachedDisplayOptions = displayList.ToArray();
            _cachedIds = idList.ToArray();
        }
    }
}
