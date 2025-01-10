using System.Linq;
using UnityEngine;
using UnityEditor;
using AnoGame.Application;
using AnoGame.Application.Utils;
using AnoGame.Application.Story;
using AnoGame.Application.Enemy;
using AnoGame.Domain.Data.Models;
using AnoGame.Infrastructure.Persistence;
using Cysharp.Threading.Tasks;

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
        private GameData _currentGameData;
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
            // LoadCurrentGameData();
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

                    if (GUILayout.Button("Save Data"))
                    {
                        // 保存
                        GameManager2.Instance.SaveData();
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
                                    // EnemySpawnManager.Instance.OnChapterLoaded(false);
                                    
                                }
                                if (GUILayout.Button("Retry Point", GUILayout.Width(100)))
                                {
                                    StartFromChapter(storyIndex, chapterIndex, true);
                                    PlayerSpawnManager.Instance.OnChapterLoaded(true);
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
            if (_currentGameData != null)
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(300)))
                {
                    scrollPosition = scrollView.scrollPosition;

                    EditorGUILayout.LabelField("Current Save Data:", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    
                    // 基本情報
                    EditorGUILayout.LabelField($"Player Name: {_currentGameData.PlayerName}");
                    EditorGUILayout.LabelField($"Score: {_currentGameData.Score}");
                    
                    // ストーリー進行状況
                    if (_currentGameData.StoryProgress != null)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Story Progress:", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Story Index: {_currentGameData.StoryProgress.CurrentStoryIndex}");
                        EditorGUILayout.LabelField($"Chapter Index: {_currentGameData.StoryProgress.CurrentChapterIndex}");
                        EditorGUILayout.LabelField($"Scene Index: {_currentGameData.StoryProgress.CurrentSceneIndex}");
                    }

                    // 位置情報
                    if (_currentGameData.PlayerPosition != null)
                    {
                        EditorGUILayout.Space(5);
                        showPositionData = EditorGUILayout.Foldout(showPositionData, "Position Data", true);
                        if (showPositionData)
                        {
                            EditorGUI.indentLevel++;

                            var position = _currentGameData.PlayerPosition.Position.ToVector3();
                            EditorGUILayout.Vector3Field("Current Position", position);

                            var rotation = _currentGameData.PlayerPosition.Rotation;
                            EditorGUILayout.Vector4Field("Current Rotation", 
                                new Vector4(rotation.X, rotation.Y, rotation.Z, rotation.W));

                            EditorGUILayout.LabelField($"Current Map ID: {_currentGameData.PlayerPosition.CurrentMapId}");
                            EditorGUILayout.LabelField($"Current Area ID: {_currentGameData.PlayerPosition.CurrentAreaId}");

                            if (_currentGameData.PlayerPosition.LastCheckpointPosition.HasValue)
                            {
                                EditorGUILayout.Vector3Field("Last Checkpoint Position", 
                                    _currentGameData.PlayerPosition.LastCheckpointPosition.Value.ToVector3());
                            }

                            if (_currentGameData.PlayerPosition.RespawnPosition.HasValue)
                            {
                                EditorGUILayout.Vector3Field("Respawn Position", 
                                    _currentGameData.PlayerPosition.RespawnPosition.Value.ToVector3());
                            }

                            EditorGUI.indentLevel--;
                        }
                    }

                    // インベントリ情報
                    if (_currentGameData.Inventory != null && _currentGameData.Inventory.Items.Any())
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Inventory:", EditorStyles.boldLabel);
                        foreach (var item in _currentGameData.Inventory.Items)
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
                    if (_currentGameData.EventHistory != null && _currentGameData.EventHistory.ClearedEvents.Any())
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Cleared Events:", EditorStyles.boldLabel);
                        foreach (var eventId in _currentGameData.EventHistory.ClearedEvents)
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


        private void LoadCurrentGameData()
        {
            var gameManager = GameManager2.Instance;
            if (gameManager != null)
            {
                _currentGameData = gameManager.CurrentGameData;
                Repaint();
            }
        }

        private async void ResetSaveData()
        {
            var gameManager = GameManager2.Instance;
            if (gameManager != null)
            {
                gameManager.ResetDataAsync();
                // await gameManager.SaveCurrentGameState();
                // currentGameData = gameManager.CurrentGameData;
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
                // storyManager.UpdateGameData();
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