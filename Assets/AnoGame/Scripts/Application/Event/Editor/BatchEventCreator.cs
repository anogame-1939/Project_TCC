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

public class BatchEventCreator
{
    private const string JSON_PATH = "Assets/AnoGame/Data/ItemsResources/events_batch.json";
    private const string PREFAB_DIR_PATH = "Assets/AnoGame/Prefabs/EventZone";
    private const string TIMELINE_DIR_PATH = "Assets/AnoGame/Data/ItemsResources/Timelines";
    private const string EVENTDATA_DIR_PATH = "Assets/AnoGame/Data/ItemsResources/Events";

    [MenuItem("Tools/Run Batch Event Creation")]
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
        GameObject root = GameObject.Find("GeneratedEvents");
        if (root == null)
        {
            root = new GameObject("GeneratedEvents");
            Undo.RegisterCreatedObjectUndo(root, "Create GeneratedEvents Root");
        }

        // Load Prefabs
        var tplContact = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR_PATH}/Tpl_ContactZone.prefab");
        var tplInspect = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR_PATH}/Tpl_InspectZone.prefab");
        var tplItem = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR_PATH}/Tpl_ItemZone.prefab");
        var tplTrigger = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR_PATH}/Tpl_Trigger.prefab");

        if (tplTrigger == null) Debug.LogError("Tpl_Trigger not found!");

        int count = 0;
        foreach (var evt in dataList.events)
        {
            // 1. Create EventData Asset
            CreateEventData(evt);

            // 2. Select Prefab based on Category
            GameObject receptorPrefab = null;
            switch (evt.category)
            {
                case "Event":
                case "Gimmick":
                case "Background":
                    receptorPrefab = tplContact;
                    break;
                case "Object":
                case "Inspect":
                case "ItemGet":
                    receptorPrefab = tplInspect;
                    break;
                case "Action":
                    receptorPrefab = tplItem;
                    break;
                case "System":
                case "FlagGet":
                    // System events might just need the trigger without a receptor zone
                    receptorPrefab = null; 
                    break;
                default:
                    Debug.LogWarning($"Unknown category {evt.category} for {evt.eventId}, defaulting to Tpl_Trigger only.");
                    break;
            }

            // 3. Create Scene Objects
            CreateEventSet(evt, receptorPrefab, tplTrigger, root.transform);
            count++;
        }
        
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets(); 
        
        Selection.activeGameObject = root;
        Debug.Log($"Batch Event Creation Complete! Created {count} events under 'GeneratedEvents'.");
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
        // IsOneTime Logic? Default to true/false based on type? User didn't specify. Assumed handled manually or by defaults.

        so.ApplyModifiedProperties();
    }

    private static void SetProp(SerializedObject so, string name, string val)
    {
        var p = so.FindProperty(name);
        if (p != null) p.stringValue = val;
    }

    private static void CreateEventSet(EventJsonItem evt, GameObject receptorPrefab, GameObject triggerPrefab, Transform parent)
    {
        // 1. Container Naming: ID + Name
        string containerName = $"{evt.eventId}_{evt.name}";
        
        GameObject container = new GameObject(containerName);
        container.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(container, StringPool.GetUniqueString());

        // Instantiate Receptor
        if (receptorPrefab != null)
        {
            GameObject receptor = (GameObject)PrefabUtility.InstantiatePrefab(receptorPrefab, container.transform);
            if (receptor != null)
            {
                receptor.name = $"{evt.eventId}_Receptor"; // Clean English name for children
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
                trigger.name = $"{evt.eventId}_Trigger"; // Clean English name for children
                Undo.RegisterCreatedObjectUndo(trigger, StringPool.GetUniqueString());
                
                // Fix Component (Old -> New) and Apply Data
                ApplyTriggerData(trigger, evt);

                if (evt.timeline) PrepareTimeline(trigger, evt);
            }
            else Debug.LogError($"Failed instantiate trigger for {evt.eventId}");
        }
    }

    private static void ApplyReceptorData(GameObject goo, EventJsonItem evt)
    {
        var contact = goo.GetComponent<ContactReceptor>();
        if (contact != null) UpdateSO(contact, "targetEventId", evt.eventId);

        var inspect = goo.GetComponent<InspectReceptor>();
        if (inspect != null)
        {
             UpdateSO(inspect, "targetEventId", evt.eventId);
             if (!string.IsNullOrEmpty(evt.paramText)) UpdateSO(inspect, "prompt", evt.paramText);
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
        // 1. Remove Old Component if exists
        // We use reflection/string check to avoid strict dependency if possible, or use the type if widely available.
        // Assuming Legacy namespace is available or we add using.
        // For safety, let's use string checks or GetComponent by name if we want to be loose, 
        // but explicit type is better. We need to add 'using AnoGame.Application.Event.Legacy;' to the file top or use full name.
        
        var oldComp = goo.GetComponent("AnoGame.Application.Event.Legacy.InstantEventTrigger_Old");
        if (oldComp != null)
        {
             Object.DestroyImmediate(oldComp);
        }

        // 2. Ensure New Component exists
        var trig = goo.GetComponent<InstantEventTrigger>();
        if (trig == null)
        {
            trig = goo.AddComponent<InstantEventTrigger>();
        }

        // 3. Set Data
        UpdateSO(trig, "targetEventId", evt.eventId);
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
        public string category; // Changed from type/prefabType
        public string description;
        public string paramText;
        public string paramItemId;
        public bool timeline;
    }
}
#endif

