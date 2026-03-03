using System.Collections.Generic;
using System.Linq;
using AnoGame.Data;
using UnityEditor;
using UnityEngine;

namespace AnoGame.Data.Editor
{
    [CustomEditor(typeof(EventTagRegistry))]
    public class EventTagRegistryEditor : UnityEditor.Editor
    {
        private const string EVENTDATA_DIR_PATH = "Assets/_Project/Data/Events";

        // 参照元キャッシュ
        private Dictionary<string, List<ReferenceInfo>> _referenceCache;
        private bool _referenceCacheDirty = true;

        // 折りたたみ状態
        private readonly HashSet<string> _expandedTags = new HashSet<string>();

        // インポート用折りたたみ
        private bool _showImportSection = false;

        private struct ReferenceInfo
        {
            public EventData EventData;
            public string FieldType; // "Result" or "Condition"
        }

        public override void OnInspectorGUI()
        {
            var registry = (EventTagRegistry)target;
            serializedObject.Update();

            // ---- ヘッダー ----
            EditorGUILayout.LabelField("Event Tag Registry", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // ---- インポートセクション ----
            _showImportSection = EditorGUILayout.Foldout(_showImportSection, "Utilities", true);
            if (_showImportSection)
            {
                EditorGUI.indentLevel++;
                if (GUILayout.Button("既存EventDataからタグをインポート", GUILayout.Height(24)))
                {
                    ImportTagsFromEventData(registry);
                }
                EditorGUILayout.HelpBox(
                    "全EventDataの resultTags / conditionTags をスキャンし、レジストリに未登録のタグを追加します。",
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            // ---- 参照元スキャン ----
            if (_referenceCacheDirty)
            {
                _referenceCache = BuildReferenceCache(registry);
                _referenceCacheDirty = false;
            }

            // ---- タグ一覧 ----
            var tagsProp = serializedObject.FindProperty("tags");
            EditorGUILayout.LabelField($"Tags ({tagsProp.arraySize})", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                var entryProp = tagsProp.GetArrayElementAtIndex(i);
                var tagNameProp = entryProp.FindPropertyRelative("tagName");
                var descProp = entryProp.FindPropertyRelative("description");
                string tagName = tagNameProp.stringValue;

                // 参照件数
                int refCount = 0;
                if (_referenceCache != null && _referenceCache.TryGetValue(tagName, out var refs))
                    refCount = refs.Count;

                // 行の背景色
                bool isUnused = refCount == 0;
                if (isUnused)
                {
                    var rect = EditorGUILayout.BeginVertical();
                    EditorGUI.DrawRect(rect, new Color(0.5f, 0.2f, 0.2f, 0.15f));
                }
                else
                {
                    EditorGUILayout.BeginVertical();
                }

                EditorGUILayout.BeginHorizontal();

                // 折りたたみ
                bool isExpanded = _expandedTags.Contains(tagName);
                string arrow = isExpanded ? "\u25BC" : "\u25B6";
                if (GUILayout.Button(arrow, GUILayout.Width(20), GUILayout.Height(18)))
                {
                    if (isExpanded) _expandedTags.Remove(tagName);
                    else _expandedTags.Add(tagName);
                }

                // タグ名
                EditorGUILayout.PropertyField(tagNameProp, GUIContent.none, GUILayout.MinWidth(100));

                // 参照カウント
                string refLabel = isUnused ? "未使用" : $"({refCount})";
                var style = new GUIStyle(EditorStyles.miniLabel);
                style.normal.textColor = isUnused ? new Color(1f, 0.4f, 0.4f) : Color.gray;
                EditorGUILayout.LabelField(refLabel, style, GUILayout.Width(50));

                // 削除ボタン
                if (GUILayout.Button("\u00d7", GUILayout.Width(20), GUILayout.Height(18)))
                {
                    tagsProp.DeleteArrayElementAtIndex(i);
                    _referenceCacheDirty = true;
                    serializedObject.ApplyModifiedProperties();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                // 展開時: 説明 + 参照元
                if (isExpanded)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(descProp, new GUIContent("Description"));

                    if (_referenceCache != null && _referenceCache.TryGetValue(tagName, out var references))
                    {
                        EditorGUILayout.LabelField("References:", EditorStyles.miniLabel);
                        foreach (var r in references)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("  ", GUILayout.Width(16));

                            string label = $"[{r.FieldType}] {r.EventData.EventId} - {r.EventData.EventName}";
                            if (GUILayout.Button(label, EditorStyles.linkLabel))
                            {
                                EditorGUIUtility.PingObject(r.EventData);
                                Selection.activeObject = r.EventData;
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("  (参照なし)", EditorStyles.miniLabel);
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();

                // 区切り線
                var lineRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f));
            }

            EditorGUILayout.Space(4);

            // ---- 新規追加ボタン ----
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Tag", GUILayout.Height(22)))
            {
                int idx = tagsProp.arraySize;
                tagsProp.InsertArrayElementAtIndex(idx);
                var newEntry = tagsProp.GetArrayElementAtIndex(idx);
                newEntry.FindPropertyRelative("tagName").stringValue = "";
                newEntry.FindPropertyRelative("description").stringValue = "";
                _referenceCacheDirty = true;
            }

            if (GUILayout.Button("Refresh References", GUILayout.Height(22)))
            {
                _referenceCacheDirty = true;
            }
            EditorGUILayout.EndHorizontal();

            if (serializedObject.ApplyModifiedProperties())
            {
                _referenceCacheDirty = true;
            }
        }

        private Dictionary<string, List<ReferenceInfo>> BuildReferenceCache(EventTagRegistry registry)
        {
            var cache = new Dictionary<string, List<ReferenceInfo>>();

            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EVENTDATA_DIR_PATH });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ed = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (ed == null) continue;

                foreach (var tag in ed.ResultTags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    if (!cache.ContainsKey(tag)) cache[tag] = new List<ReferenceInfo>();
                    cache[tag].Add(new ReferenceInfo { EventData = ed, FieldType = "Result" });
                }

                foreach (var tag in ed.ConditionTags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    // 否定タグは ! を除いた実タグ名で記録
                    string realTag = tag.StartsWith("!") ? tag.Substring(1) : tag;
                    string fieldType = tag.StartsWith("!") ? "Condition(!)" : "Condition";
                    if (!cache.ContainsKey(realTag)) cache[realTag] = new List<ReferenceInfo>();
                    cache[realTag].Add(new ReferenceInfo { EventData = ed, FieldType = fieldType });
                }
            }

            return cache;
        }

        private void ImportTagsFromEventData(EventTagRegistry registry)
        {
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EVENTDATA_DIR_PATH });
            int importedCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ed = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (ed == null) continue;

                foreach (var tag in ed.ResultTags)
                {
                    if (!string.IsNullOrEmpty(tag) && !registry.Contains(tag))
                    {
                        registry.AddTag(tag);
                        importedCount++;
                    }
                }

                foreach (var tag in ed.ConditionTags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    string realTag = tag.StartsWith("!") ? tag.Substring(1) : tag;
                    if (!registry.Contains(realTag))
                    {
                        registry.AddTag(realTag);
                        importedCount++;
                    }
                }
            }

            if (importedCount > 0)
            {
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssetIfDirty(registry);
                _referenceCacheDirty = true;
            }

            Debug.Log($"[EventTagRegistry] {importedCount} tags imported.");
        }
    }
}
