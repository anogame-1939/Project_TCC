#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.IO;
using System.Collections.Generic;
using AnoGame.Application.Event; // InstantEventTrigger, ContactReceptor, etc.
using System.Reflection;

public class BatchEventCreator
{
    private const string JSON_PATH = "Assets/AnoGame/Data/ItemsResources/events_batch.json";
    private const string PREFAB_DIR_PATH = "Assets/AnoGame/Prefabs/EventZone";
    private const string TIMELINE_DIR_PATH = "Assets/AnoGame/Data/ItemsResources/Timelines";

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

        // Root Object for cleanup
        GameObject root = GameObject.Find("GeneratedEvents");
        if (root == null) root = new GameObject("GeneratedEvents");

        // Load Prefabs
        Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        foreach(var evt in dataList.events)
        {
            if (!templates.ContainsKey(evt.prefabType))
            {
                string path = $"{PREFAB_DIR_PATH}/{evt.prefabType}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError($"Prefab not found: {path}");
                }
                else
                {
                    templates[evt.prefabType] = prefab;
                }
            }
        }
        
        GameObject triggerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR_PATH}/Tpl_Trigger.prefab");
        if(triggerPrefab == null)
        {
            Debug.LogError("Tpl_Trigger.prefab needed for Timeline/Trigger logic.");
        }

        int count = 0;
        foreach (var evt in dataList.events)
        {
            if (!templates.ContainsKey(evt.prefabType)) continue;

            CreateEventSet(evt, templates[evt.prefabType], triggerPrefab, root.transform);
            count++;
        }
        
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets(); // Save Timelines
        Debug.Log($"Batch Event Creation Complete! Created {count} events.");
    }

    private static void CreateEventSet(EventJsonItem evt, GameObject receptorPrefab, GameObject triggerPrefab, Transform parent)
    {
        // 1. Create Container per event
        GameObject container = new GameObject(evt.eventId);
        container.transform.SetParent(parent);
        
        // 2. Instantiate Receptor (Zone)
        if (receptorPrefab != null)
        {
            GameObject receptor = (GameObject)PrefabUtility.InstantiatePrefab(receptorPrefab, container.transform);
            receptor.name = $"{evt.eventId}_Receptor";
            // Apply Data
            ApplyReceptorData(receptor, evt);
        }

        // 3. Instantiate Trigger (Logic/Timeline)
        // Only if timeline is requested or implicitly needed
        if (triggerPrefab != null)
        {
            GameObject trigger = (GameObject)PrefabUtility.InstantiatePrefab(triggerPrefab, container.transform);
            trigger.name = $"{evt.eventId}_Trigger";
            
            // Apply Trigger Data (Set EventID)
            ApplyTriggerData(trigger, evt);

            // Create/Assign Timeline if needed
            if (evt.timeline)
            {
                PrepareTimeline(trigger, evt);
            }
        }
    }

    private static void ApplyReceptorData(GameObject goo, EventJsonItem evt)
    {
        // Try known components
        var contact = goo.GetComponent<ContactReceptor>();
        if (contact != null)
        {
            // use reflection or public field
             var so = new SerializedObject(contact);
             so.Update();
             var p = so.FindProperty("targetEventId");
             if(p!=null) p.stringValue = evt.eventId;
             so.ApplyModifiedProperties();
        }

        var inspect = goo.GetComponent<InspectReceptor>();
        if (inspect != null)
        {
             var so = new SerializedObject(inspect);
             so.Update();
             var pId = so.FindProperty("targetEventId");
             if(pId!=null) pId.stringValue = evt.eventId;
             
             if (!string.IsNullOrEmpty(evt.paramText))
             {
                 var pTxt = so.FindProperty("prompt");
                 if(pTxt!=null) pTxt.stringValue = evt.paramText; // promptに表示するテキスト
             }
             so.ApplyModifiedProperties();
        }
        
        var itemRep = goo.GetComponent<ItemReceptor>();
        if (itemRep != null)
        {
            var so = new SerializedObject(itemRep);
             so.Update();
             var pId = so.FindProperty("targetEventId");
             if(pId!=null) pId.stringValue = evt.eventId;
             
             if(!string.IsNullOrEmpty(evt.paramItemId))
             {
                 var pItem = so.FindProperty("targetItemId");
                 if(pItem!=null) pItem.stringValue = evt.paramItemId;
             }
             so.ApplyModifiedProperties();
        }
    }

    private static void ApplyTriggerData(GameObject goo, EventJsonItem evt)
    {
        var trig = goo.GetComponent<InstantEventTrigger>();
        if (trig != null)
        {
             var so = new SerializedObject(trig);
             so.Update();
             // Find property backing the field 'targetEventId' 
             var p = so.FindProperty("targetEventId");
             if(p!=null) p.stringValue = evt.eventId;
             so.ApplyModifiedProperties();
        }
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
        public string type;
        public string prefabType;
        public string description;
        public string paramText;
        public string paramItemId;
        public bool timeline;
    }
}
#endif
