using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "StoryStoryData", menuName = "Event System/Story StoryData")]
public class StoryData : ScriptableObject
{
    [Serializable]
    public class SceneData
    {
        public string sceneName;
        public SceneReference sceneReference;
        public List<EventData> events = new List<EventData>();
    }

    [Serializable]
    public class ChapterData
    {
        public string chapterName;
        public List<SceneData> scenes = new List<SceneData>();
    }

    [Serializable]
    public class EventData
    {
        public string eventName;
        public GameObject eventPrefab;
        // イベント固有のデータをここに追加...
    }

    public string storyName;
    public List<ChapterData> chapters = new List<ChapterData>();

    // 現在のストーリー進行状況を追跡するための変数
    [HideInInspector] public int currentChapterIndex = 0;
    [HideInInspector] public int currentSceneIndex = 0;

    public SceneData GetCurrentScene()
    {
        if (currentChapterIndex < chapters.Count && currentSceneIndex < chapters[currentChapterIndex].scenes.Count)
        {
            return chapters[currentChapterIndex].scenes[currentSceneIndex];
        }
        return null;
    }

    public void MoveToNextScene()
    {
        currentSceneIndex++;
        if (currentSceneIndex >= chapters[currentChapterIndex].scenes.Count)
        {
            currentChapterIndex++;
            currentSceneIndex = 0;
            if (currentChapterIndex >= chapters.Count)
            {
                Debug.Log("Story completed!");
                // ストーリー完了時の処理をここに追加
            }
        }
    }
}

[Serializable]
public class SceneReference
{
    public string scenePath;
    #if UNITY_EDITOR
    public SceneAsset sceneAsset;
    #endif
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferencePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var sceneAssetProperty = property.FindPropertyRelative("sceneAsset");
        var scenePathProperty = property.FindPropertyRelative("scenePath");
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        EditorGUI.BeginChangeCheck();
        var newSceneAsset = EditorGUI.ObjectField(position, sceneAssetProperty.objectReferenceValue, typeof(SceneAsset), false) as SceneAsset;
        if (EditorGUI.EndChangeCheck())
        {
            sceneAssetProperty.objectReferenceValue = newSceneAsset;
            if (newSceneAsset != null)
            {
                var scenePath = AssetDatabase.GetAssetPath(newSceneAsset);
                scenePathProperty.stringValue = scenePath;
            }
        }
        EditorGUI.EndProperty();
    }
}

[CustomEditor(typeof(StoryData))]
public class StoryDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("storyName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("chapters"), true);

        if (GUILayout.Button("Update Scene Paths"))
        {
            UpdateScenePaths();
        }

        if (GUILayout.Button("Update Build Settings"))
        {
            UpdateBuildSettings();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void UpdateScenePaths()
    {
        var chaptersProperty = serializedObject.FindProperty("chapters");
        for (int i = 0; i < chaptersProperty.arraySize; i++)
        {
            var scenesProperty = chaptersProperty.GetArrayElementAtIndex(i).FindPropertyRelative("scenes");
            for (int j = 0; j < scenesProperty.arraySize; j++)
            {
                var sceneProperty = scenesProperty.GetArrayElementAtIndex(j);
                var sceneReferenceProperty = sceneProperty.FindPropertyRelative("sceneReference");
                var sceneAssetProperty = sceneReferenceProperty.FindPropertyRelative("sceneAsset");
                var scenePathProperty = sceneReferenceProperty.FindPropertyRelative("scenePath");
                if (sceneAssetProperty.objectReferenceValue != null)
                {
                    var scenePath = AssetDatabase.GetAssetPath(sceneAssetProperty.objectReferenceValue);
                    scenePathProperty.stringValue = scenePath;
                }
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void UpdateBuildSettings()
    {
        StoryData StoryData = (StoryData)target;
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        HashSet<string> addedScenePaths = new HashSet<string>();

        // Add all scenes from the StoryData
        foreach (var chapter in StoryData.chapters)
        {
            foreach (var scene in chapter.scenes)
            {
                if (!string.IsNullOrEmpty(scene.sceneReference.scenePath) && !addedScenePaths.Contains(scene.sceneReference.scenePath))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(scene.sceneReference.scenePath, true));
                    addedScenePaths.Add(scene.sceneReference.scenePath);
                }
            }
        }

        // Optionally, keep existing scenes that are not in the StoryData
        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            if (!addedScenePaths.Contains(buildScene.path))
            {
                buildScenes.Add(buildScene);
            }
        }

        // Update the build settings
        EditorBuildSettings.scenes = buildScenes.ToArray();

        Debug.Log("Build settings updated with scenes from StoryData");
    }
}
#endif
