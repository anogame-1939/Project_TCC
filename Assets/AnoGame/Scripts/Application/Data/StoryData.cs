using UnityEngine;
using System;
using System.Collections.Generic;
using AnoGame.Application.Core.Scene;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Application.Data
{
    [CreateAssetMenu(fileName = "StoryStoryData", menuName = "Event System/Story StoryData")]
    public class StoryData : ScriptableObject
    {
        [Serializable]
        public class SceneData
        {
            public string sceneName;
            public SceneReference sceneReference; // 完全修飾名で指定

            public List<EventData> events = new List<EventData>();
        }

        // それ以外のクラス定義は変更なし
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
        }

        // 以下、変更なし
        public string storyName;
        public List<ChapterData> chapters = new List<ChapterData>();

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
                }
            }
        }
    }

#if UNITY_EDITOR
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
            StoryData storyData = (StoryData)target;
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            HashSet<string> addedScenePaths = new HashSet<string>();

            foreach (var chapter in storyData.chapters)
            {
                foreach (var scene in chapter.scenes)
                {
                    if (!string.IsNullOrEmpty(scene.sceneReference.ScenePath) && 
                        !addedScenePaths.Contains(scene.sceneReference.ScenePath))
                    {
                        buildScenes.Add(new EditorBuildSettingsScene(scene.sceneReference.ScenePath, true));
                        addedScenePaths.Add(scene.sceneReference.ScenePath);
                    }
                }
            }

            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (!addedScenePaths.Contains(buildScene.path))
                {
                    buildScenes.Add(buildScene);
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("Build settings updated with scenes from StoryData");
        }
    }
#endif
}