#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnoGame.AnoFlow;
using AnoGame.AnoFlow.Editor;
using AnoGame.Application.Event;
using AnoGame.Data;

namespace AnoGame.Editor.EventPlacement
{
    /// <summary>
    /// Receptor の種別
    /// </summary>
    public enum ReceptorType
    {
        Contact,
        Inspect,
        Item
    }

    /// <summary>
    /// ワンクリックでイベントオブジェクトをシーン上に配置するためのサービス。
    /// EventData、シーンオブジェクト階層、.playable ファイルを一括生成する。
    /// </summary>
    public static class EventPlacementService
    {
        // ── 定数 ──
        private const string PREFAB_DIR = "Assets/AnoGame/Prefabs/EventZone";
        private const string TIMELINE_DIR = "Assets/AnoGame/Data/ItemsResources/Timelines";
        private const string TEMPLATE_TIMELINE = "Assets/AnoGame/Data/ItemsResources/Timelines/Tpl_Timeline.playable";
        private const string SCENE_ROOT_NAME = "GeneratedEventsV2";

        // EventGraphWindow と共通の EditorPrefs キー
        private const string PREF_ROOT_PATH = "AnoFlow.EventGraph.RootPath";
        private const string PREF_SELECTED_FOLDER = "AnoFlow.EventGraph.SelectedFolder";
        private const string DEFAULT_ROOT_PATH = "Assets/AnoGame/Data/Events";

        /// <summary>
        /// 現在 EventGraph で選択中のフォルダパスを取得する
        /// </summary>
        public static string GetActiveFolderPath()
        {
            string root = EditorPrefs.GetString(PREF_ROOT_PATH, DEFAULT_ROOT_PATH);
            string folder = EditorPrefs.GetString(PREF_SELECTED_FOLDER, "");
            if (string.IsNullOrEmpty(folder))
                return root;
            return $"{root}/{folder}";
        }

        /// <summary>
        /// メインのエントリポイント。シーン上の指定座標にイベント一式を配置する。
        /// </summary>
        /// <returns>生成された Container の GameObject</returns>
        public static GameObject PlaceEvent(Vector3 position, ReceptorType receptorType)
        {
            string folderPath = GetActiveFolderPath();

            // フォルダ存在確認
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogError($"[EventPlacement] フォルダが存在しません: {folderPath}");
                return null;
            }

            // 1. 次のEventID算出
            string eventId = GenerateNextEventId(folderPath);
            string category = receptorType.ToString();

            // 2. EventData 作成
            EventData eventData = CreateEventDataAsset(folderPath, eventId, category);
            if (eventData == null)
            {
                Debug.LogError($"[EventPlacement] EventData の作成に失敗しました: {eventId}");
                return null;
            }

            // 3. シーン階層構築
            GameObject container = BuildSceneHierarchy(eventData, receptorType, position);
            if (container == null)
            {
                Debug.LogError($"[EventPlacement] シーンオブジェクトの構築に失敗しました: {eventId}");
                return null;
            }

            // 4. テンプレート Timeline 複製
            CopyTemplateTimeline(eventData, container);

            // 5. SceneBinding 登録
            RegisterSceneBinding(eventData, folderPath, container);

            // 6. シーンをダーティに
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            // 7. 選択
            Selection.activeGameObject = container;
            EditorGUIUtility.PingObject(container);

            Debug.Log($"[EventPlacement] イベント配置完了: {eventId} ({category}) at {position}");
            return container;
        }

        /// <summary>
        /// フォルダ内の既存 EventData から次の連番 ID を算出する。
        /// 形式: EV_XXX (3桁ゼロ埋め)
        /// </summary>
        public static string GenerateNextEventId(string folderPath)
        {
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { folderPath });
            int maxNum = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (asset == null || asset.IsDeleted) continue;

                string id = asset.EventId;
                if (!string.IsNullOrEmpty(id) && id.StartsWith("EV_"))
                {
                    // EV_XXX の数値部分を抽出
                    string numPart = id.Substring(3);
                    // EV_001_Something の場合、最初のアンダースコアで分割
                    int underscoreIdx = numPart.IndexOf('_');
                    if (underscoreIdx >= 0)
                        numPart = numPart.Substring(0, underscoreIdx);

                    if (int.TryParse(numPart, out int num))
                    {
                        if (num > maxNum) maxNum = num;
                    }
                }
            }

            return $"EV_{(maxNum + 1):D3}";
        }

        /// <summary>
        /// EventData の ScriptableObject を作成する。
        /// </summary>
        private static EventData CreateEventDataAsset(string folderPath, string eventId, string category)
        {
            string assetPath = $"{folderPath}/{eventId}.asset";

            var asset = ScriptableObject.CreateInstance<EventData>();

            // private fields への設定は SerializedObject 経由
            AssetDatabase.CreateAsset(asset, assetPath);

            var so = new SerializedObject(asset);
            so.Update();

            var eventIdProp = so.FindProperty("eventId");
            if (eventIdProp != null) eventIdProp.stringValue = eventId;

            var eventNameProp = so.FindProperty("eventName");
            if (eventNameProp != null) eventNameProp.stringValue = "";

            var categoryProp = so.FindProperty("category");
            if (categoryProp != null) categoryProp.stringValue = category;

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            return asset;
        }

        /// <summary>
        /// Container > Receptor + Trigger の階層をシーン上に構築する。
        /// </summary>
        private static GameObject BuildSceneHierarchy(EventData eventData, ReceptorType receptorType, Vector3 position)
        {
            // ルートオブジェクト取得/作成
            GameObject root = GameObject.Find(SCENE_ROOT_NAME);
            if (root == null)
            {
                root = new GameObject(SCENE_ROOT_NAME);
                Undo.RegisterCreatedObjectUndo(root, "Create GeneratedEventsV2 Root");
            }

            // プレハブ読み込み
            string receptorPrefabName = GetReceptorPrefabName(receptorType);
            var receptorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/{receptorPrefabName}");
            var triggerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/Tpl_Trigger.prefab");

            if (triggerPrefab == null)
            {
                Debug.LogError("[EventPlacement] Tpl_Trigger.prefab が見つかりません");
                return null;
            }

            // Container 作成
            string containerName = $"{eventData.EventId}_{receptorType}";
            var container = new GameObject(containerName);
            container.transform.SetParent(root.transform);
            container.transform.position = position;
            Undo.RegisterCreatedObjectUndo(container, $"Create Event {eventData.EventId}");

            // AnoEventRoot をアタッチ
            var eventRoot = container.AddComponent<AnoEventRoot>();

            GameObject receptorGo = null;
            GameObject triggerGo = null;

            // Receptor インスタンス化
            if (receptorPrefab != null)
            {
                receptorGo = (GameObject)PrefabUtility.InstantiatePrefab(receptorPrefab, container.transform);
                if (receptorGo != null)
                {
                    receptorGo.name = $"{eventData.EventId}_Receptor_{receptorType}";
                    Undo.RegisterCreatedObjectUndo(receptorGo, $"Create Receptor {eventData.EventId}");
                    ApplyReceptorData(receptorGo, eventData);
                }
            }

            // Trigger インスタンス化
            triggerGo = (GameObject)PrefabUtility.InstantiatePrefab(triggerPrefab, container.transform);
            if (triggerGo != null)
            {
                triggerGo.name = $"{eventData.EventId}_Trigger";
                Undo.RegisterCreatedObjectUndo(triggerGo, $"Create Trigger {eventData.EventId}");
                ApplyTriggerData(triggerGo, eventData);
            }

            // AnoEventRoot.Setup()
            eventRoot.Setup(eventData, receptorGo, triggerGo);
            EditorUtility.SetDirty(eventRoot);

            return container;
        }

        /// <summary>
        /// Receptor タイプに対応するプレハブ名を返す。
        /// </summary>
        private static string GetReceptorPrefabName(ReceptorType type)
        {
            switch (type)
            {
                case ReceptorType.Contact: return "Tpl_ContactZone.prefab";
                case ReceptorType.Inspect: return "Tpl_InspectZone.prefab";
                case ReceptorType.Item: return "Tpl_ItemZone.prefab";
                default: return "Tpl_InspectZone.prefab";
            }
        }

        /// <summary>
        /// Receptor コンポーネントに EventData を設定する。
        /// </summary>
        private static void ApplyReceptorData(GameObject go, EventData eventData)
        {
            // ContactReceptor
            var contact = go.GetComponent<ContactReceptor>();
            if (contact != null)
            {
                SetSerializedField(contact, "targetEventId", eventData.EventId);
                SetSerializedObjectRef(contact, "eventData", eventData);
            }

            // InspectReceptor
            var inspect = go.GetComponent<InspectReceptor>();
            if (inspect != null)
            {
                SetSerializedField(inspect, "targetEventId", eventData.EventId);
                SetSerializedObjectRef(inspect, "eventData", eventData);
            }

            // ItemReceptor
            var itemReceptor = go.GetComponent<ItemReceptor>();
            if (itemReceptor != null)
            {
                SetSerializedField(itemReceptor, "targetEventId", eventData.EventId);
                SetSerializedObjectRef(itemReceptor, "eventData", eventData);
            }
        }

        /// <summary>
        /// Trigger コンポーネントに EventData を設定する。
        /// </summary>
        private static void ApplyTriggerData(GameObject go, EventData eventData)
        {
            // 旧コンポーネント削除
            var oldComp = go.GetComponent("AnoGame.Application.Event.Legacy.InstantEventTrigger_Old");
            if (oldComp != null) Object.DestroyImmediate(oldComp);

            var trigger = go.GetComponent<InstantEventTrigger>();
            if (trigger == null) trigger = go.AddComponent<InstantEventTrigger>();

            SetSerializedField(trigger, "targetEventId", eventData.EventId);

            // EventTriggerBase の eventData フィールドを設定
            var so = new SerializedObject(trigger);
            so.Update();
            var eventDataProp = so.FindProperty("eventData");
            if (eventDataProp != null)
            {
                eventDataProp.objectReferenceValue = eventData;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// テンプレート .playable を複製して PlayableDirector に割り当てる。
        /// </summary>
        private static void CopyTemplateTimeline(EventData eventData, GameObject container)
        {
            // Trigger 子オブジェクトの PlayableDirector を取得
            PlayableDirector director = null;
            foreach (Transform child in container.transform)
            {
                director = child.GetComponent<PlayableDirector>();
                if (director != null) break;
            }

            if (director == null) return;

            // テンプレートが存在するか確認
            var templateTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TEMPLATE_TIMELINE);
            if (templateTimeline == null)
            {
                Debug.LogWarning($"[EventPlacement] テンプレート Timeline が見つかりません: {TEMPLATE_TIMELINE}");
                return;
            }

            // 複製先パス
            if (!Directory.Exists(TIMELINE_DIR))
                Directory.CreateDirectory(TIMELINE_DIR);

            string destPath = $"{TIMELINE_DIR}/{eventData.EventId}_Timeline.playable";

            // 既に存在する場合はそれを使用
            var existingTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(destPath);
            if (existingTimeline != null)
            {
                director.playableAsset = existingTimeline;
                EditorUtility.SetDirty(director);
                return;
            }

            // テンプレートを複製
            if (AssetDatabase.CopyAsset(TEMPLATE_TIMELINE, destPath))
            {
                AssetDatabase.Refresh();
                var newTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(destPath);
                if (newTimeline != null)
                {
                    director.playableAsset = newTimeline;
                    EditorUtility.SetDirty(director);
                }
            }
            else
            {
                Debug.LogError($"[EventPlacement] Timeline の複製に失敗: {destPath}");
            }
        }

        /// <summary>
        /// EventGraphMeta に SceneBinding を登録する。
        /// </summary>
        private static void RegisterSceneBinding(EventData eventData, string folderPath, GameObject container)
        {
            var meta = EventGraphMeta.Load(folderPath);
            var sceneName = EditorSceneManager.GetActiveScene().name;

            // targetScenes に追加
            if (!meta.targetScenes.Contains(sceneName))
            {
                meta.targetScenes.Add(sceneName);
            }

            // Receptor / Trigger 名を取得
            string receptorName = "";
            string triggerName = "";
            foreach (Transform child in container.transform)
            {
                if (child.name.Contains("Receptor")) receptorName = child.name;
                if (child.name.Contains("Trigger")) triggerName = child.name;
            }

            // ルートからの相対パス
            string containerPath = $"{SCENE_ROOT_NAME}/{container.name}";

            meta.SetSceneBinding(
                eventData.EventId,
                sceneName,
                containerPath,
                receptorName,
                triggerName
            );
            meta.Save(folderPath);
        }

        // ── ユーティリティ ──

        private static void SetSerializedField(Object target, string propName, string value)
        {
            var so = new SerializedObject(target);
            so.Update();
            var prop = so.FindProperty(propName);
            if (prop != null) prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedObjectRef(Object target, string propName, Object value)
        {
            var so = new SerializedObject(target);
            so.Update();
            var prop = so.FindProperty(propName);
            if (prop != null) prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
