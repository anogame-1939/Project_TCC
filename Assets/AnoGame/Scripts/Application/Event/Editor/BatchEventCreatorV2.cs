#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.IO;
using System.Collections.Generic;
using AnoGame.Application.Event;
using AnoGame.Application.Event.Editor.UIToolkit;
using AnoGame.Data;
using System.Reflection;

public class BatchEventCreatorV2
{
    private const string JSON_PATH = "Assets/AnoGame/Data/ItemsResources/events_story2.json";
    private const string PREFAB_DIR_PATH = "Assets/AnoGame/Prefabs/EventZone";
    private const string TIMELINE_DIR_PATH = "Assets/AnoGame/Data/ItemsResources/Timelines";
    private const string EVENTDATA_DIR_PATH = "Assets/AnoGame/Data/Events/Story2";
    private readonly static Vector3 PLACEMENT_OFFSET = new Vector3(0, 0, 5f);

    [MenuItem("Tools/Run Batch Event Creation V2")]
    public static void RunBatch()
    {
        if (!File.Exists(JSON_PATH))
        {
            Debug.LogError($"JSON file not found: {JSON_PATH}");
            return;
        }

        string jsonContent = File.ReadAllText(JSON_PATH);
        // string wrappedJson = "{\"events\":" + jsonContent + "}"; // Removed wrapping as file has root
        EventList dataList = JsonUtility.FromJson<EventList>(jsonContent);

        if (dataList == null || dataList.events == null)
        {
            Debug.LogError("Failed to parse JSON.");
            return;
        }

        if (!Directory.Exists(TIMELINE_DIR_PATH)) Directory.CreateDirectory(TIMELINE_DIR_PATH);
        if (!Directory.Exists(EVENTDATA_DIR_PATH)) Directory.CreateDirectory(EVENTDATA_DIR_PATH);

        // Root Object
        GameObject root = GameObject.Find("GeneratedEventsV2");
        if (root == null)
        {
            root = new GameObject("GeneratedEventsV2");
            Undo.RegisterCreatedObjectUndo(root, "Create GeneratedEventsV2 Root");
        }

        // Load Prefabs
        var tplContact = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR_PATH}/Tpl_ContactZone.prefab");
        var tplInspect = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR_PATH}/Tpl_InspectZone.prefab");
        var tplItem = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR_PATH}/Tpl_ItemZone.prefab");
        var tplTrigger = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR_PATH}/Tpl_Trigger.prefab");

        if (tplTrigger == null) Debug.LogError("Tpl_Trigger not found!");

        int count = 0;
        Vector3 currentPos = Vector3.zero;

        // 1. First Pass: Update/Create all EventData Assets from JSON (Migration)
        // This ensures the Assets are the Source of Truth and contain all necessary data including Category.
        foreach (var evt in dataList.events)
        {
            CreateEventData(evt);
        }
        AssetDatabase.SaveAssets(); // Ensure assets are written

        // 2. Second Pass: Build Scene from EventData ASSETS
        // We reload them from disk to ensure we are using the Asset data.
        var eventAssets = LoadAllEventDataInFolder(EVENTDATA_DIR_PATH);
        
        // Sort specifically to match the order in JSON for layout consistency? 
        // Or just trust the folder order? The user table implies an order (ID order).
        // Let's sort by ID to be safe and consistent.
        eventAssets.Sort((a, b) => string.Compare(a.EventId, b.EventId, System.StringComparison.Ordinal));

        foreach (var data in eventAssets)
        {
            // Select Prefab based on Category stored in ASSET
            GameObject receptorPrefab = null;
            switch (data.Category) // Uses the new field
            {
                case "初回": // "Contact" mapped to Japanese category
                case "Contact":
                    receptorPrefab = tplContact;
                    break;
                case "井戸": // "Inspect" mapped to Japanese category
                case "ラスト":
                case "Inspect":
                case "Interact":
                case "Search": // "調べる"
                case "調べる":
                    receptorPrefab = tplInspect;
                    break;
                case "ItemUse":
                case "アイテム使用":
                    receptorPrefab = tplItem;
                    break;
                case "Chain":
                case "連鎖":
                    receptorPrefab = null; 
                    break;
                case "専用インタラクト":
                case "UniqueInteract":
                    receptorPrefab = tplInspect; // Default to Inspect for unique?
                    break;
                default:
                    // Fallback based on name or just Trigger
                    if (data.EventName.Contains("接触")) receptorPrefab = tplContact;
                    else if (data.EventName.Contains("調べる")) receptorPrefab = tplInspect;
                    else if (data.EventName.Contains("アイテム")) receptorPrefab = tplItem;
                    else receptorPrefab = null;
                    break;
            }

            // Create Scene Objects using the ASSET data
            CreateEventSet(data, receptorPrefab, tplTrigger, root.transform, currentPos);
            currentPos += PLACEMENT_OFFSET;
            count++;
        }
        
        // 3. Update EventGraphMeta with scene bindings
        var sceneName = EditorSceneManager.GetActiveScene().name;
        var meta = EventGraphMeta.Load();
        if (!meta.targetScenes.Contains(sceneName))
        {
            meta.targetScenes.Add(sceneName);
        }
        foreach (Transform child in root.transform)
        {
            // Find eventId from container name (format: "{eventId}_{eventName}")
            string containerName = child.name;
            int underscoreIdx = -1;
            // EventId format: EV_XXX_Name — find the third section boundary
            // We match against loaded EventData assets instead
            EventData matchedData = null;
            foreach (var ed in eventAssets)
            {
                if (containerName.StartsWith(ed.EventId))
                {
                    matchedData = ed;
                    break;
                }
            }
            if (matchedData == null) continue;

            string receptorName = null;
            string triggerName = null;
            foreach (Transform grandchild in child)
            {
                if (grandchild.name.Contains("Receptor")) receptorName = grandchild.name;
                if (grandchild.name.Contains("Trigger")) triggerName = grandchild.name;
            }

            meta.SetSceneBinding(
                matchedData.EventId,
                sceneName,
                $"{root.name}/{containerName}",
                receptorName ?? "",
                triggerName ?? ""
            );
        }
        meta.Save();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = root;
        Debug.Log($"Batch Event Creation V2 Complete! Created {count} events using EventData Assets. Meta file updated.");
    }

    private static void CreateEventData(EventJsonItem evt)
    {
        string path = $"{EVENTDATA_DIR_PATH}/{evt.eventId}.asset";
        EventData asset = AssetDatabase.LoadAssetAtPath<EventData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<EventData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        SerializedObject so = new SerializedObject(asset);
        so.Update();
        
        SetProp(so, "eventId", evt.eventId);
        SetProp(so, "eventName", evt.name);
        SetProp(so, "description", evt.description);
        SetProp(so, "category", evt.category); // Populate Category
        
        // --- V2: Apply Conditions to EventData ---
        var requiredItemsProp = so.FindProperty("requiredItemIds");
        var requiredEventsProp = so.FindProperty("requiredEventIds");

        if (requiredItemsProp != null && requiredEventsProp != null)
        {
            requiredItemsProp.ClearArray();
            requiredEventsProp.ClearArray();

            if (evt.requiredItemIds != null && evt.requiredItemIds.Count > 0)
            {
                for (int i = 0; i < evt.requiredItemIds.Count; i++)
                {
                    requiredItemsProp.InsertArrayElementAtIndex(i);
                    var itemElem = requiredItemsProp.GetArrayElementAtIndex(i);
                    itemElem.FindPropertyRelative("itemId").stringValue = evt.requiredItemIds[i];
                }
            }

            if (evt.requiredEventIds != null && evt.requiredEventIds.Count > 0)
            {
                for (int i = 0; i < evt.requiredEventIds.Count; i++)
                {
                    requiredEventsProp.InsertArrayElementAtIndex(i);
                    var eventElem = requiredEventsProp.GetArrayElementAtIndex(i);
                    eventElem.FindPropertyRelative("eventId").stringValue = evt.requiredEventIds[i];
                }
            }
        }

        // --- V2: Apply Results to EventData ---
        var resultsProp = so.FindProperty("results");
        if (resultsProp != null)
        {
            resultsProp.ClearArray();
            if (evt.results != null && evt.results.Count > 0)
            {
                for (int i = 0; i < evt.results.Count; i++)
                {
                    resultsProp.InsertArrayElementAtIndex(i);
                    resultsProp.GetArrayElementAtIndex(i).stringValue = evt.results[i];
                }
            }
        }

        so.ApplyModifiedProperties();
    }

    private static List<EventData> LoadAllEventDataInFolder(string folderPath)
    {
        var list = new List<EventData>();
        var guids = AssetDatabase.FindAssets("t:EventData", new[] { folderPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<EventData>(path);
            if (asset != null) list.Add(asset);
        }
        return list;
    }

    private static void SetProp(SerializedObject so, string name, string val)
    {
        var p = so.FindProperty(name);
        if (p != null) p.stringValue = val;
    }

    private static void CreateEventSet(EventData evtData, GameObject receptorPrefab, GameObject triggerPrefab, Transform parent, Vector3 localPosition)
    {
        // 1. Container Naming: ID + Name
        string containerName = $"{evtData.EventId}_{evtData.EventName}";
        
        GameObject container = new GameObject(containerName);
        container.transform.SetParent(parent);
        container.transform.localPosition = localPosition;
        Undo.RegisterCreatedObjectUndo(container, StringPool.GetUniqueString());

        // Instantiate Receptor
        if (receptorPrefab != null)
        {
            GameObject receptor = (GameObject)PrefabUtility.InstantiatePrefab(receptorPrefab, container.transform);
            if (receptor != null)
            {
                receptor.name = $"{evtData.EventId}_Receptor_{evtData.Category}";
                Undo.RegisterCreatedObjectUndo(receptor, StringPool.GetUniqueString());
                ApplyReceptorData(receptor, evtData);
            }
            else Debug.LogError($"Failed instantiate receptor for {evtData.EventId}");
        }

        // Instantiate Trigger
        if (triggerPrefab != null)
        {
            GameObject trigger = (GameObject)PrefabUtility.InstantiatePrefab(triggerPrefab, container.transform);
            if (trigger != null)
            {
                trigger.name = $"{evtData.EventId}_Trigger";
                Undo.RegisterCreatedObjectUndo(trigger, StringPool.GetUniqueString());
                
                ApplyTriggerData(trigger, evtData);

                // Check timeline requirement? 
                // Currently stored in JSON 'timeline' bool. 
                // We might need to add this to EventData or infer it.
                // For now, let's assume if category is specific or by name?
                // Or purely rely on existing timeline assets matching the name?
                PrepareTimeline(trigger, evtData);
            }
            else Debug.LogError($"Failed instantiate trigger for {evtData.EventId}");
        }
    }

    private static void ApplyReceptorData(GameObject goo, EventData evtData)
    {
        var contact = goo.GetComponent<ContactReceptor>();
        if (contact != null) UpdateSO(contact, "targetEventId", evtData.EventId);

        var inspect = goo.GetComponent<InspectReceptor>();
        if (inspect != null)
        {
             UpdateSO(inspect, "targetEventId", evtData.EventId);
             
             // Prompt text logic? 
             // If Description is used as prompt? Or we need a specific prompt field?
             // For now, let's use Description if it's short, or generic "Check". 
             // Or leave it to manual edit.
             if (!string.IsNullOrEmpty(evtData.Description)) UpdateSO(inspect, "prompt", evtData.Description);
             
             // V2: Set EventData reference
             if (evtData != null)
             {
                 var so = new SerializedObject(inspect);
                 so.Update();
                 var p = so.FindProperty("eventData");
                 if (p != null) p.objectReferenceValue = evtData;
                 so.ApplyModifiedProperties();
             }
        }
        
        var itemRep = goo.GetComponent<ItemReceptor>();
        if (itemRep != null)
        {
            UpdateSO(itemRep, "targetEventId", evtData.EventId);
            // paramItemId logic needs to be in EventData if we want validation?
            // If Category is ItemUse, we might need a "TargetItemId" field in EventData.
            // For now, skip auto-setting targetItemId from EventData unless we add it.
        }
    }

    private static void ApplyTriggerData(GameObject goo, EventData evtData)
    {
        // 1. Component check
        var oldComp = goo.GetComponent("AnoGame.Application.Event.Legacy.InstantEventTrigger_Old");
        if (oldComp != null) Object.DestroyImmediate(oldComp);

        var trig = goo.GetComponent<InstantEventTrigger>();
        if (trig == null) trig = goo.AddComponent<InstantEventTrigger>();

        // 3. Set Data
        UpdateSO(trig, "targetEventId", evtData.EventId);

        // 4. V2: Clear local conditions as they are now in EventData
        SerializedObject so = new SerializedObject(trig);
        so.Update();
        
        var requiredItemsProp = so.FindProperty("requiredItems");
        var requiredEventsProp = so.FindProperty("requiredEvents");

        if (requiredItemsProp != null) requiredItemsProp.ClearArray();
        if (requiredEventsProp != null) requiredEventsProp.ClearArray();
        
        so.ApplyModifiedProperties();
    }

    private static void UpdateSO(Object target, string propName, string value)
    {
        var so = new SerializedObject(target);
        so.Update();
        var p = so.FindProperty(propName);
        if (p != null) p.stringValue = value;
        so.ApplyModifiedProperties();
    }

    private static void PrepareTimeline(GameObject goo, EventData evtData)
    {
        var director = goo.GetComponent<PlayableDirector>();
        if (director == null) return;

        string assetName = $"{evtData.EventId}_Timeline";
        string path = $"{TIMELINE_DIR_PATH}/{assetName}.playable";
        
        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        
        // If timeline exists, assign it. We don't auto-create unless logic dictates.
        // If we want to support auto-creation for specific events:
        if (timeline != null)
        {
            director.playableAsset = timeline;
        }
    }

    static class StringPool { public static string GetUniqueString() => System.Guid.NewGuid().ToString(); }

    [System.Serializable]
    private class EventList
    {
        public List<EventJsonItem> events;
    }

    [System.Serializable]
    private class EventJsonItem
    {
        public string eventId;
        public string name;
        public string category; 
        public string description;
        public string paramText;
        public string paramItemId;
        public bool timeline;
        public List<string> requiredItemIds;
        public List<string> requiredEventIds;
        public List<string> results;
    }
}
#endif
