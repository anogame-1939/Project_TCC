#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.IO;
using System.Collections.Generic;
using AnoGame.Application.Event;
using AnoGame.Data;
using System.Reflection;

public class BatchEventCreatorV2
{
    private const string JSON_PATH = "Assets/AnoGame/Data/ItemsResources/events_batch.json";
    private const string PREFAB_DIR_PATH = "Assets/AnoGame/Prefabs/EventZone";
    private const string TIMELINE_DIR_PATH = "Assets/AnoGame/Data/ItemsResources/Timelines";
    private const string EVENTDATA_DIR_PATH = "Assets/AnoGame/Data/ItemsResources/Events";
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
        string wrappedJson = "{\"events\":" + jsonContent + "}";
        EventList dataList = JsonUtility.FromJson<EventList>(wrappedJson);

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
        foreach (var evt in dataList.events)
        {
            // 1. Create EventData Asset and Set Conditions (V2 Change)
            CreateEventData(evt);

            // 2. Select Prefab based on Category
            GameObject receptorPrefab = null;
            switch (evt.category)
            {
                case "Contact":
                    receptorPrefab = tplContact;
                    break;
                case "Inspect":
                case "Interact":
                    receptorPrefab = tplInspect;
                    break;
                case "ItemUse":
                    receptorPrefab = tplItem;
                    break;
                case "Chain":
                    receptorPrefab = null; 
                    break;
                default:
                    Debug.LogWarning($"Unknown category {evt.category} for {evt.eventId}, defaulting to Tpl_Trigger only.");
                    break;
            }

            // 3. Create Scene Objects
            CreateEventSet(evt, receptorPrefab, tplTrigger, root.transform, currentPos);
            currentPos += PLACEMENT_OFFSET;
            count++;
        }
        
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets(); 
        
        Selection.activeGameObject = root;
        Debug.Log($"Batch Event Creation V2 Complete! Created {count} events under 'GeneratedEventsV2'.");
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
        
        // --- V2: Apply Conditions to EventData ---
        var requiredItemsProp = so.FindProperty("requiredItemIds");
        var requiredEventsProp = so.FindProperty("requiredEventIds");

        if (requiredItemsProp != null && requiredEventsProp != null)
        {
            requiredItemsProp.ClearArray();
            requiredEventsProp.ClearArray();

            if (evt.conditions != null && evt.conditions.Count > 0)
            {
                 foreach (var condId in evt.conditions)
                {
                    // Check if Item ID exists (validation only, or trust ID)
                    // Since we store string, we can just store condId if we assume it might be an item.
                    // But we want to separate Items and Events.
                    // Heuristic: Try FindItemData. If not null, it's an Item.
                    
                    var item = FindItemData(condId);
                    if (item != null)
                    {
                        int itemIndex = requiredItemsProp.arraySize;
                        requiredItemsProp.InsertArrayElementAtIndex(itemIndex);
                        requiredItemsProp.GetArrayElementAtIndex(itemIndex).stringValue = condId; // Store ID
                        continue;
                    }

                    // Assume Event if not Item
                    // Since we store string IDs, we don't need the Asset to exist yet (forward reference support).
                    // Just add to RequiredEvents.
                    
                    int eventIndex = requiredEventsProp.arraySize;
                    requiredEventsProp.InsertArrayElementAtIndex(eventIndex);
                    requiredEventsProp.GetArrayElementAtIndex(eventIndex).stringValue = condId; // Store ID
                    continue;
                }
            }
        }

        so.ApplyModifiedProperties();
    }

    private static void SetProp(SerializedObject so, string name, string val)
    {
        var p = so.FindProperty(name);
        if (p != null) p.stringValue = val;
    }

    private static void CreateEventSet(EventJsonItem evt, GameObject receptorPrefab, GameObject triggerPrefab, Transform parent, Vector3 localPosition)
    {
        // 1. Container Naming: ID + Name
        string containerName = $"{evt.eventId}_{evt.name}";
        
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
                receptor.name = $"{evt.eventId}_Receptor_{evt.category}";
                Undo.RegisterCreatedObjectUndo(receptor, StringPool.GetUniqueString());
                ApplyReceptorData(receptor, evt);
            }
            else Debug.LogError($"Failed instantiate receptor for {evt.eventId}");
        }

        // Instantiate Trigger
        if (triggerPrefab != null)
        {
            GameObject trigger = (GameObject)PrefabUtility.InstantiatePrefab(triggerPrefab, container.transform);
            if (trigger != null)
            {
                trigger.name = $"{evt.eventId}_Trigger";
                Undo.RegisterCreatedObjectUndo(trigger, StringPool.GetUniqueString());
                
                ApplyTriggerData(trigger, evt);

                if (evt.timeline) PrepareTimeline(trigger, evt);
            }
            else Debug.LogError($"Failed instantiate trigger for {evt.eventId}");
        }
    }

    private static void ApplyReceptorData(GameObject goo, EventJsonItem evt)
    {
        var eventData = FindEventData(evt.eventId);

        var contact = goo.GetComponent<ContactReceptor>();
        if (contact != null) UpdateSO(contact, "targetEventId", evt.eventId);

        var inspect = goo.GetComponent<InspectReceptor>();
        if (inspect != null)
        {
             UpdateSO(inspect, "targetEventId", evt.eventId);
             if (!string.IsNullOrEmpty(evt.paramText)) UpdateSO(inspect, "prompt", evt.paramText);
             
             // V2: Set EventData reference
             if (eventData != null)
             {
                 var so = new SerializedObject(inspect);
                 so.Update();
                 var p = so.FindProperty("eventData");
                 if (p != null) p.objectReferenceValue = eventData;
                 so.ApplyModifiedProperties();
             }
        }
        
        var itemRep = goo.GetComponent<ItemReceptor>();
        if (itemRep != null)
        {
            UpdateSO(itemRep, "targetEventId", evt.eventId);
            if (!string.IsNullOrEmpty(evt.paramItemId)) UpdateSO(itemRep, "targetItemId", evt.paramItemId);
        }
    }

    private static void ApplyTriggerData(GameObject goo, EventJsonItem evt)
    {
        // 1. Component check
        var oldComp = goo.GetComponent("AnoGame.Application.Event.Legacy.InstantEventTrigger_Old");
        if (oldComp != null) Object.DestroyImmediate(oldComp);

        var trig = goo.GetComponent<InstantEventTrigger>();
        if (trig == null) trig = goo.AddComponent<InstantEventTrigger>();

        // 3. Set Data
        UpdateSO(trig, "targetEventId", evt.eventId);

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

    private static void PrepareTimeline(GameObject goo, EventJsonItem evt)
    {
        var director = goo.GetComponent<PlayableDirector>();
        if (director == null) return;

        string assetName = $"{evt.eventId}_Timeline";
        string path = $"{TIMELINE_DIR_PATH}/{assetName}.playable";
        
        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        if (timeline == null)
        {
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, path);
        }

        director.playableAsset = timeline;
    }

    private static ItemData FindItemData(string itemId)
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData asset = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (asset != null && asset.ItemId == itemId) return asset;
        }
        return null;
    }

    private static EventData FindEventData(string eventId)
    {
        string path = $"{EVENTDATA_DIR_PATH}/{eventId}.asset";
        EventData asset = AssetDatabase.LoadAssetAtPath<EventData>(path);
        if (asset != null) return asset;

        string[] guids = AssetDatabase.FindAssets("t:EventData");
        foreach (var guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            EventData a = AssetDatabase.LoadAssetAtPath<EventData>(p);
            if (a != null && a.EventId == eventId) return a;
        }
        return null; // Return null if not found (might be created later in loop)
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
        public List<string> conditions;
        public List<string> results;
    }
}
#endif
