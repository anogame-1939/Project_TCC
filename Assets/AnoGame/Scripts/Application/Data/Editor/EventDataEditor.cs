#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using AnoGame.Data;

namespace AnoGame.Data.Editor
{
    [CustomEditor(typeof(EventData)), CanEditMultipleObjects]
    public class EventDataEditor : UnityEditor.Editor
    {
        SerializedProperty _eventId;
        SerializedProperty _eventName;
        SerializedProperty _description;
        SerializedProperty _isOneTime;

        void OnEnable()
        {
            _eventId = serializedObject.FindProperty("eventId");
            _eventName = serializedObject.FindProperty("eventName");
            _description = serializedObject.FindProperty("description");
            _isOneTime = serializedObject.FindProperty("isOneTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // eventId は誤編集防止のため読み取り専用表示
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_eventId, new GUIContent("Event Id"));
            }

            // 実用ボタン群
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("GUIDを生成/再割当"))
            {
                GenerateGuidForTargets(onlyIfEmpty: false);
                return;
            }
            if (GUILayout.Button("空欄ならGUID自動生成"))
            {
                GenerateGuidForTargets(onlyIfEmpty: true);
                return;
            }
            if (GUILayout.Button("IDをコピー"))
            {
                EditorGUIUtility.systemCopyBuffer = _eventId.stringValue ?? string.Empty;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // 残りのプロパティ
            EditorGUILayout.PropertyField(_eventName);
            EditorGUILayout.PropertyField(_description);
            EditorGUILayout.PropertyField(_isOneTime);

            serializedObject.ApplyModifiedProperties();
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