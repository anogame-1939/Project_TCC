using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using UnityEngine.Playables;
using UnityEngine.Timeline; // Timeline用

namespace AnoGame.AnoFlow.Editor
{
    // --- AIからの入力JSONデータ構造 ---
    [Serializable]
    public class EventJsonData
    {
        public int eventId;
        public string prefabType;      // "Tpl_Inspect", "Tpl_ZoneEvent" など
        public string objectName;      // "EV_008_MetalPlate" など
        public TimelineJsonData timeline;
        public ComponentJsonData components;
    }

    [Serializable]
    public class TimelineJsonData
    {
        public bool createAsset;
        public string assetName;       // "TL_EV_008_MetalPlate"
    }

    [Serializable]
    public class ComponentJsonData
    {
        public string interactionText; // "調べる"
        public string requiredItemId;  // "Item_Magnet"
        public string nextEventId;     // 連鎖用
    }

    // --- ツール本体 ---
    public class EventGeneratorWindow : EditorWindow
    {
        string jsonInput = "";
        Vector3 nextSpawnPosition = Vector3.zero; // 次の配置位置（重ならないように）

        [MenuItem("Tools/SLFB Event Generator")]
        public static void ShowWindow()
        {
            GetWindow<EventGeneratorWindow>("Event Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("Step 2: Event Object Generator", EditorStyles.boldLabel);

            GUILayout.Label("Start Position (Playground):", EditorStyles.miniLabel);
            nextSpawnPosition = EditorGUILayout.Vector3Field("", nextSpawnPosition);

            GUILayout.Space(10);
            GUILayout.Label("Paste Single Event JSON Here:", EditorStyles.miniLabel);
            jsonInput = EditorGUILayout.TextArea(jsonInput, GUILayout.Height(150));

            if (GUILayout.Button("Generate Event Object", GUILayout.Height(40)))
            {
                GenerateEvent();
            }
        }

        void GenerateEvent()
        {
            if (string.IsNullOrEmpty(jsonInput)) return;

            try
            {
                EventJsonData data = JsonUtility.FromJson<EventJsonData>(jsonInput);

                // 1. プレハブのロード (Resources/EventTemplates/フォルダから)
                GameObject prefab = Resources.Load<GameObject>("EventTemplates/" + data.prefabType);
                if (prefab == null)
                {
                    Debug.LogError($"Prefab Not Found: Resources/EventTemplates/{data.prefabType}");
                    return;
                }

                // 2. インスタンス化
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = data.objectName;
                instance.transform.position = nextSpawnPosition;

                // 次の配置位置をX軸にずらす
                nextSpawnPosition += new Vector3(5f, 0, 0);

                Undo.RegisterCreatedObjectUndo(instance, "Create Event Object");

                // 3. データの注入
                ApplyDataToComponents(instance, data);

                // 4. Timelineアセットの自動生成
                if (data.timeline != null && data.timeline.createAsset)
                {
                    CreateTimelineAsset(instance, data.timeline.assetName);
                }

                Debug.Log($"Success: Created {data.objectName}");
            }
            catch (Exception e)
            {
                Debug.LogError("JSON Parse Error: " + e.Message);
            }
        }

        void ApplyDataToComponents(GameObject obj, EventJsonData data)
        {
            // ここでコンポーネントに値を流し込みます
            // 例: インタラクト時のテキスト設定
            /*
            var interact = obj.GetComponent<InteractableObject>();
            if (interact != null)
            {
                interact.text = data.components.interactionText;
            }
            */

            // アイテムIDの設定など
        }

        void CreateTimelineAsset(GameObject obj, string assetName)
        {
            string folderPath = "Assets/AnoGame/Resources/Timelines";
            // フォルダがない場合は作成
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            string fullPath = $"{folderPath}/{assetName}.playable";

            // Timelineアセットの新規作成
            // (既に同名がある場合はユニークな名前をつける処理を入れても良い)

            // 同名ファイルチェック
            if (File.Exists(fullPath))
            {
                fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            }

            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, fullPath);

            // シーン上のPlayableDirectorにセット
            var director = obj.GetComponent<PlayableDirector>();
            if (director != null)
            {
                director.playableAsset = timeline;
                EditorUtility.SetDirty(director);
            }
        }
    }
}
