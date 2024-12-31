using System.Linq;
using UnityEngine;
using UnityEditor;
using AnoGame.Application;
using AnoGame.Application.Utils;
using AnoGame.Application.Story;
using AnoGame.Application.Enemy;
using AnoGame.Domain.Data.Models;
using AnoGame.Infrastructure.Persistence;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(GameManager2))]
    public class GameManagerEditor2 : UnityEditor.Editor
    {
        private bool showDebugOptions = true;
        private bool showStoryOptions = true;
        private bool showStartOptions = true;
        private bool showPositionData = true;
        private Vector2 scrollPosition;
        private GameData currentGameData;
        private StoryManager storyManager;
        private const string SAVE_FILE_NAME = "savedata.json";

        // 追加: スタート方法の選択肢
        private enum StartPointType
        {
            StartPoint,
            RetryPoint
        }
        private StartPointType selectedStartType = StartPointType.StartPoint;

        private void OnEnable()
        {
            storyManager = FindFirstObjectByType<StoryManager>();
            LoadCurrentGameData();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawDebugOptions();
                DrawGameStartOptions();
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

        private void DrawGameStartOptions()
        {
        showStartOptions = EditorGUILayout.Foldout(showStartOptions, "Game Start Options", true, EditorStyles.foldoutHeader);
        
        if (showStartOptions)
        {
            EditorGUI.indentLevel++;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (storyManager != null)
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

                                // ボタンを横に2つ並べる
                                if (GUILayout.Button("Start Point", GUILayout.Width(100)))
                                {
                                    StartFromChapter(storyIndex, chapterIndex, false);
                                    PlayerSpawnManager.Instance.OnChapterLoaded(false);
                                    EnemySpawnManager.Instance.OnChapterLoaded(false);
                                }
                                if (GUILayout.Button("Retry Point", GUILayout.Width(100)))
                                {
                                    StartFromChapter(storyIndex, chapterIndex, true);
                                    PlayerSpawnManager.Instance.OnChapterLoaded(true);
                                    EnemySpawnManager.Instance.OnChapterLoaded(true);
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
                else 
                {
                    EditorGUILayout.HelpBox("StoryManager not found in the scene", MessageType.Error);
                }
            }

            EditorGUI.indentLevel--;
        }
        }

        private void DrawCurrentGameData()
        {
            if (currentGameData != null)
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(300)))
                {
                    scrollPosition = scrollView.scrollPosition;

                    EditorGUILayout.LabelField("Current Save Data:", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    
                    // 基本情報
                    EditorGUILayout.LabelField($"Player Name: {currentGameData.PlayerName}");
                    EditorGUILayout.LabelField($"Score: {currentGameData.Score}");
                    
                    // ストーリー進行状況
                    if (currentGameData.StoryProgress != null)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Story Progress:", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Story Index: {currentGameData.StoryProgress.CurrentStoryIndex}");
                        EditorGUILayout.LabelField($"Chapter Index: {currentGameData.StoryProgress.CurrentChapterIndex}");
                        EditorGUILayout.LabelField($"Scene Index: {currentGameData.StoryProgress.CurrentSceneIndex}");
                    }

                    // 位置情報
                    if (currentGameData.PlayerPosition != null)
                    {
                        EditorGUILayout.Space(5);
                        showPositionData = EditorGUILayout.Foldout(showPositionData, "Position Data", true);
                        if (showPositionData)
                        {
                            EditorGUI.indentLevel++;

                            var position = currentGameData.PlayerPosition.Position.ToVector3();
                            EditorGUILayout.Vector3Field("Current Position", position);

                            var rotation = currentGameData.PlayerPosition.Rotation;
                            EditorGUILayout.Vector4Field("Current Rotation", 
                                new Vector4(rotation.X, rotation.Y, rotation.Z, rotation.W));

                            EditorGUILayout.LabelField($"Current Map ID: {currentGameData.PlayerPosition.CurrentMapId}");
                            EditorGUILayout.LabelField($"Current Area ID: {currentGameData.PlayerPosition.CurrentAreaId}");

                            if (currentGameData.PlayerPosition.LastCheckpointPosition.HasValue)
                            {
                                EditorGUILayout.Vector3Field("Last Checkpoint Position", 
                                    currentGameData.PlayerPosition.LastCheckpointPosition.Value.ToVector3());
                            }

                            if (currentGameData.PlayerPosition.RespawnPosition.HasValue)
                            {
                                EditorGUILayout.Vector3Field("Respawn Position", 
                                    currentGameData.PlayerPosition.RespawnPosition.Value.ToVector3());
                            }

                            EditorGUI.indentLevel--;
                        }
                    }

                    // インベントリ情報
                    if (currentGameData.Inventory != null && currentGameData.Inventory.Items.Any())
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Inventory:", EditorStyles.boldLabel);
                        foreach (var item in currentGameData.Inventory.Items)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"- {item.ItemName} x{item.Quantity}");
                            if (!string.IsNullOrEmpty(item.Description))
                            {
                                EditorGUILayout.LabelField($"({item.Description})", EditorStyles.miniLabel);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    // クリア済みイベント情報
                    if (currentGameData.EventHistory != null && currentGameData.EventHistory.ClearedEvents.Any())
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Cleared Events:", EditorStyles.boldLabel);
                        foreach (var eventId in currentGameData.EventHistory.ClearedEvents)
                        {
                            EditorGUILayout.LabelField($"- {eventId}");
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
            var gameManager = target as GameManager2;
            if (gameManager != null)
            {
                var jsonManager = new AsyncJsonDataManager();
                currentGameData = await jsonManager.LoadDataAsync<GameData>(SAVE_FILE_NAME);
                Repaint();
            }
        }

        private async void ResetSaveData()
        {
            var gameManager = target as GameManager2;
            if (gameManager != null)
            {
                var jsonManager = new AsyncJsonDataManager();
                await jsonManager.SaveDataAsync(SAVE_FILE_NAME, (GameData)null);
                currentGameData = null;
                Repaint();
            }
        }

        private void StartFromChapter(int storyIndex, int chapterIndex, bool useRetryPoint)
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
                storyManager.SwitchToChapterInStory(storyIndex, chapterIndex, useRetryPoint);
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