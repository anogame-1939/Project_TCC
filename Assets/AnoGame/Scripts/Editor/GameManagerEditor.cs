using UnityEngine;
using UnityEditor;
using AnoGame.Application;
using AnoGame.Application.Story;
using AnoGame.Data;
using AnoGame.Infrastructure.Persistence;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : UnityEditor.Editor
    {
      private bool showDebugOptions = true;
        private bool showStoryOptions = true;
        private Vector2 scrollPosition;
        private GameData currentGameData;
        private StoryManager storyManager;
        private const string SAVE_FILE_NAME = "savedata.json";

        private void OnEnable()
        {
            storyManager = FindObjectOfType<StoryManager>();
            LoadCurrentGameData();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawDebugOptions();
                DrawStoryOptions();
            }
        }

        private void DrawDebugOptions()
        {
            showDebugOptions = EditorGUILayout.Foldout(showDebugOptions, "Debug Options", true, EditorStyles.foldoutHeader);
    
            if (showDebugOptions)
            {
                EditorGUI.indentLevel++;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button("Reset Save Data"))
                    {
                        if (EditorUtility.DisplayDialog("Reset Save Data",
                            "Are you sure you want to reset all save data? This cannot be undone.",
                            "Reset", "Cancel"))
                        {
                            ResetSaveData();
                        }
                    }

                    if (GUILayout.Button("Load Current Save Data"))
                    {
                        LoadCurrentGameData();
                    }

                    DrawCurrentGameData();
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawStoryOptions()
        {
            // 初期状態で開いた状態にし、foldoutの矢印を右向きにする
            showStoryOptions = EditorGUILayout.Foldout(showStoryOptions, "Story Debug Options", true, EditorStyles.foldoutHeader);
            
            if (showStoryOptions && storyManager != null)
            {
                EditorGUI.indentLevel++;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var stories = storyManager.GetStoryList();
                    if (stories != null && stories.Count > 0)
                    {
                        for (int storyIndex = 0; storyIndex < stories.Count; storyIndex++)
                        {
                            var story = stories[storyIndex];
                            EditorGUILayout.LabelField($"Story: {story.storyName}", EditorStyles.boldLabel);

                            EditorGUI.indentLevel++;
                            for (int chapterIndex = 0; chapterIndex < story.chapters.Count; chapterIndex++)
                            {
                                var chapter = story.chapters[chapterIndex];
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField($"Chapter {chapterIndex}: {chapter.chapterName}");
                                if (GUILayout.Button("Start from here", GUILayout.Width(100)))
                                {
                                    StartFromChapter(storyIndex, chapterIndex);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                            EditorGUILayout.Space(5);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No stories found in StoryManager", MessageType.Warning);
                    }
                }

                EditorGUI.indentLevel--;
            }
            else if (storyManager == null)
            {
                EditorGUILayout.HelpBox("StoryManager not found in the scene", MessageType.Error);
            }
        }

        private void DrawCurrentGameData()
        {
            if (currentGameData != null)
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(200)))
                {
                    scrollPosition = scrollView.scrollPosition;

                    EditorGUILayout.LabelField("Current Save Data:", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.LabelField($"Player Name: {currentGameData.playerName}");
                    EditorGUILayout.LabelField($"Score: {currentGameData.score}");
                    
                    if (currentGameData.storyProgress != null)
                    {
                        EditorGUILayout.LabelField("Story Progress:", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Story Index: {currentGameData.storyProgress.currentStoryIndex}");
                        EditorGUILayout.LabelField($"Chapter Index: {currentGameData.storyProgress.currentChapterIndex}");
                        EditorGUILayout.LabelField($"Scene Index: {currentGameData.storyProgress.currentSceneIndex}");
                    }

                    if (currentGameData.inventory != null && currentGameData.inventory.Count > 0)
                    {
                        EditorGUILayout.LabelField("Inventory:", EditorStyles.boldLabel);
                        foreach (var item in currentGameData.inventory)
                        {
                            EditorGUILayout.LabelField($"- {item.itemName} x{item.quantity}");
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No save data loaded", MessageType.Info);
            }
        }

        private async void LoadCurrentGameData()
        {
            var gameManager = target as GameManager;
            if (gameManager != null)
            {
                var jsonManager = new AsyncJsonDataManager();
                currentGameData = await jsonManager.LoadDataAsync<GameData>(SAVE_FILE_NAME);
                Repaint();
            }
        }

        private async void ResetSaveData()
        {
            var gameManager = target as GameManager;
            if (gameManager != null)
            {
                var jsonManager = new AsyncJsonDataManager();
                await jsonManager.SaveDataAsync(SAVE_FILE_NAME, (GameData)null);
                currentGameData = null;
                Repaint();
            }
        }

        private void StartFromChapter(int storyIndex, int chapterIndex)
        {
            if (!UnityEngine.Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Error",
                    "Please enter play mode to start from a specific chapter",
                    "OK");
                return;
            }

            if (storyManager != null)
            {
                storyManager.SwitchToStory(storyIndex);
                storyManager.LoadChapter(chapterIndex);
            }
        }

        [MenuItem("Tools/AnoGame/Reset Save Data")]
        private static void ResetSaveDataMenuItem()
        {
            if (EditorUtility.DisplayDialog("Reset Save Data",
                "Are you sure you want to reset all save data? This cannot be undone.",
                "Reset", "Cancel"))
            {
                var jsonManager = new AsyncJsonDataManager();
                jsonManager.SaveDataAsync(SAVE_FILE_NAME, (GameData)null).Wait();
                Debug.Log("Save data has been reset");
            }
        }
    }
}