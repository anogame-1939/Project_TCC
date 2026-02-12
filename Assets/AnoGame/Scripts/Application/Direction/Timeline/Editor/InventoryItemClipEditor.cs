using UnityEngine;
using UnityEditor;
using AnoGame.Application.Direction.Timeline;
using AnoGame.Data;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.Application.Direction.Timeline.Editor
{
    [CustomEditor(typeof(InventoryItemClip))]
    public class InventoryItemClipEditor : UnityEditor.Editor
    {
        private SerializedProperty _itemData;
        private SerializedProperty _handlerOverride;

        private List<ItemData> _cachedItems = new List<ItemData>();
        private string[] _itemNames;

        private void OnEnable()
        {
            _itemData = serializedObject.FindProperty("itemData");
            _handlerOverride = serializedObject.FindProperty("handlerOverride");

            RefreshItemList();
        }

        private void RefreshItemList()
        {
            _cachedItems.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ItemData");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                {
                    _cachedItems.Add(item);
                }
            }

            // Sort by ItemName
            _cachedItems.Sort((a, b) => string.Compare(a.ItemName, b.ItemName));

            // Create display names
            _itemNames = _cachedItems.Select(x => x.ItemName).ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw Handler Override
            EditorGUILayout.PropertyField(_handlerOverride);

            EditorGUILayout.Space();

            // Item Data Check
            ItemData currentItem = _itemData.objectReferenceValue as ItemData;

            // Find current index
            int currentIndex = -1;
            if (currentItem != null)
            {
                currentIndex = _cachedItems.IndexOf(currentItem);

                // If not found in cache (e.g. newly created), refresh and try again
                if (currentIndex == -1)
                {
                    RefreshItemList();
                    currentIndex = _cachedItems.IndexOf(currentItem);
                }
            }

            // Dropdown
            // If list is empty, show just the label
            if (_itemNames != null && _itemNames.Length > 0)
            {
                int newIndex = EditorGUILayout.Popup("Item Name", currentIndex, _itemNames);
                if (newIndex >= 0 && newIndex < _cachedItems.Count)
                {
                    if (newIndex != currentIndex)
                    {
                        _itemData.objectReferenceValue = _cachedItems[newIndex];
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("Item Name", "No ItemData found");
            }

            // Refresh button
            if (GUILayout.Button("Refresh Item List"))
            {
                RefreshItemList();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
