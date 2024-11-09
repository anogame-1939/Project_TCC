using UnityEngine;
using UnityEditor;
using AnoGame.Application;
using AnoGame.Application.Story;
using AnoGame.Application.SaveData;
using AnoGame.Data;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : UnityEditor.Editor
    {
        private bool showDebugOptions = false;
        private bool showStoryOptions = false;
        private Vector2 scrollPosition;
        private GameData currentGameData;
        private StoryManager storyManager;
        private GameDataRepository _repository;

        private void OnEnable()
        {
            storyManager = FindAnyObjectByType<StoryManager>();
            _repository = new GameDataRepository();
            LoadCurrentGameData();
        }

        // ... 他のメソッドは変更なし ...

        private async void LoadCurrentGameData()
        {
            var gameManager = target as GameManager;
            if (gameManager != null)
            {
                currentGameData = await _repository.LoadDataAsync();
                Repaint();
            }
        }

        private async void ResetSaveData()
        {
            var gameManager = target as GameManager;
            if (gameManager != null)
            {
                await _repository.SaveDataAsync(null);
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
                var repository = new GameDataRepository();
                repository.SaveDataAsync(null).Wait();
                Debug.Log("Save data has been reset");
            }
        }
    }
}