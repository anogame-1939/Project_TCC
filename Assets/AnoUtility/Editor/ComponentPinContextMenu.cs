using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AnoGame.Utility.Editor
{
    /// <summary>
    /// Inspector のコンポーネントヘッダー右クリックメニューに
    /// 「Pin with Values」を追加するユーティリティ。
    /// </summary>
    [InitializeOnLoad]
    internal static class ComponentPinContextMenu
    {
        static ComponentPinContextMenu()
        {
            EditorApplication.contextualPropertyMenu += OnContextualPropertyMenu;
        }

        private static void OnContextualPropertyMenu(GenericMenu menu, SerializedProperty property)
        {
            // SerializedProperty のルートが Component でなければ無視
            if (property == null || property.serializedObject == null)
                return;

            var target = property.serializedObject.targetObject;
            if (target == null || !(target is Component component))
                return;

            // メニューに「Pin with Values」を追加
            menu.AddItem(
                new GUIContent("Pin with Values (Quick Component Adder)"),
                false,
                () => PinComponent(component)
            );
        }

        private static void PinComponent(Component component)
        {
            if (component == null) return;

            var type = component.GetType();
            var json = EditorJsonUtility.ToJson(component, false);

            // 表示ラベル作成（型名 + 主要値プレビュー）
            var displayLabel = BuildDisplayLabel(type, json);

            var entry = new QuickComponentAdderWindow.PinnedComponentEntry
            {
                assemblyQualifiedTypeName = type.AssemblyQualifiedName,
                displayLabel = displayLabel,
                jsonValues = json
            };

            QuickComponentAdderWindow.AddPinnedEntry(entry);

            Debug.Log($"[QuickComponentAdder] Pinned: {displayLabel}");
        }

        private static string BuildDisplayLabel(Type type, string json)
        {
            // JSON から主要プロパティの値をサマリーとして抽出
            // 長すぎる場合は切り詰める
            const int maxPreviewLen = 40;

            var preview = json.Length > maxPreviewLen
                ? json.Substring(0, maxPreviewLen) + "..."
                : json;

            return $"{type.Name} ({preview})";
        }
    }
}
