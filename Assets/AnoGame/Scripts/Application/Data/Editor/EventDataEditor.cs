#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using AnoGame.Data;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace AnoGame.Data.Editor
{
    [CustomEditor(typeof(EventData)), CanEditMultipleObjects]
    public class EventDataEditor : UnityEditor.Editor
    {
        SerializedProperty _eventId;
        SerializedProperty _eventName;
        SerializedProperty _description;
        SerializedProperty _isOneTime;
        SerializedProperty _requiredItems;
        SerializedProperty _requiredEvents;

        void OnEnable()
        {
            _eventId = serializedObject.FindProperty("eventId");
            _eventName = serializedObject.FindProperty("eventName");
            _description = serializedObject.FindProperty("description");
            _isOneTime = serializedObject.FindProperty("isOneTime");
            _requiredItems = serializedObject.FindProperty("requiredItemIds");
            _requiredEvents = serializedObject.FindProperty("requiredEventIds");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // eventId (ReadOnly)
            var eventIdField = new PropertyField(_eventId, "Event Id");
            eventIdField.SetEnabled(false);
            root.Add(eventIdField);

            // Access to eventId string for buttons
            var eventIdProp = serializedObject.FindProperty("eventId");

            // Buttons Row
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginTop = 2;
            buttonRow.style.marginBottom = 2;

            var btnGenerate = new Button(() =>
            {
                GenerateGuidForTargets(onlyIfEmpty: false);
            })
            { text = "GUIDを生成/再割当" };
            btnGenerate.style.flexGrow = 1;

            var btnAuto = new Button(() =>
            {
                GenerateGuidForTargets(onlyIfEmpty: true);
            })
            { text = "空欄ならGUID自動生成" };
            btnAuto.style.flexGrow = 1;

            var btnCopy = new Button(() =>
            {
                EditorGUIUtility.systemCopyBuffer = eventIdProp.stringValue ?? string.Empty;
            })
            { text = "IDをコピー" };
            btnCopy.style.flexGrow = 1;

            buttonRow.Add(btnGenerate);
            buttonRow.Add(btnAuto);
            buttonRow.Add(btnCopy);
            root.Add(buttonRow);

            // Spacer
            root.Add(new VisualElement { style = { height = 8 } });

            // Other Properties
            root.Add(new PropertyField(_eventName));
            root.Add(new PropertyField(_description));
            root.Add(new PropertyField(_isOneTime));

            // Spacer
            root.Add(new VisualElement { style = { height = 8 } });

            // Conditions Header
            var conditionsLabel = new Label("Conditions");
            conditionsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(conditionsLabel);

            // Condition Lists
            root.Add(new PropertyField(_requiredItems));
            root.Add(new PropertyField(_requiredEvents));

            return root;
        }

        void GenerateGuidForTargets(bool onlyIfEmpty)
        {
            foreach (var obj in targets)
            {
                var so = new SerializedObject(obj);
                var idProp = so.FindProperty("eventId");

                if (onlyIfEmpty && !string.IsNullOrEmpty(idProp.stringValue))
                    continue;

                Undo.RecordObject(obj, "Generate EventData GUID");
                idProp.stringValue = Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(obj);
            }

            // プロジェクト保存（任意）
            AssetDatabase.SaveAssets();

            // 画面を更新
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────
        // Assets メニューから一括実行（選択中の EventData 対象）
        // ─────────────────────────────────────────────────────────────

        [MenuItem("Assets/AnoGame/EventData/選択にGUIDを再割当", true)]
        static bool ValidateReassignGuids() => Selection.objects.Any(o => o is EventData);

        [MenuItem("Assets/AnoGame/EventData/選択にGUIDを再割当")]
        static void ReassignGuidsToSelection()
        {
            var targets = Selection.objects.OfType<EventData>().ToArray();
            if (targets.Length == 0) return;

            Undo.RecordObjects(targets, "Reassign EventData GUIDs");
            foreach (var ed in targets)
            {
                var so = new SerializedObject(ed);
                var idProp = so.FindProperty("eventId");
                idProp.stringValue = Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(ed);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("EventData", $"GUIDを {targets.Length} 件に再割り当てしました。", "OK");
        }

        [MenuItem("Assets/AnoGame/EventData/選択で空欄のみGUID自動生成", true)]
        static bool ValidateFillEmptyGuids() => Selection.objects.Any(o => o is EventData);

        [MenuItem("Assets/AnoGame/EventData/選択で空欄のみGUID自動生成")]
        static void FillEmptyGuidsInSelection()
        {
            var targets = Selection.objects.OfType<EventData>().ToArray();
            if (targets.Length == 0) return;

            int count = 0;
            Undo.RecordObjects(targets, "Fill Empty EventData GUIDs");
            foreach (var ed in targets)
            {
                var so = new SerializedObject(ed);
                var idProp = so.FindProperty("eventId");
                if (string.IsNullOrEmpty(idProp.stringValue))
                {
                    idProp.stringValue = Guid.NewGuid().ToString();
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(ed);
                    count++;
                }
            }
            if (count > 0) AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("EventData", $"空欄の GUID を {count} 件生成しました。", "OK");
        }
    }
}
#endif