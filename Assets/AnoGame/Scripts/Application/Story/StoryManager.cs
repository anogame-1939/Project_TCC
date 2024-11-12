using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using AnoGame.Infrastructure;
using AnoGame.Data;
using AnoGame.Application;

namespace AnoGame.Application.Story
{
    public class StoryManager : SingletonMonoBehaviour<StoryManager>
    {   
        public event Action ChapterLoaded;
        
        [SerializeField]
        private List<StoryData> _storyDataList;

        private List<GameObject> _spawnedObjects = new List<GameObject>();
        private int _currentStoryIndex = 0;

        private List<Scene> _loadedStoryScenes = new List<Scene>();


        private void Awake()
        {
            // GameManagerのLoadGameDataイベントを購読
            GameManager.Instance.LoadGameData += OnLoadGameData;
            // GameManager.Instance.SaveGameData += OnSaveGameData;
        }

        private void OnDestroy()
        {
            // イベント購読の解除
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadGameData -= OnLoadGameData;
                // GameManager.Instance.SaveGameData -= OnSaveGameData;
            }
        }

        private void OnLoadGameData(GameData gameData)
        {
            if (gameData == null) return;

            // ストーリー進行状況の復元
            if (gameData.storyProgress != null)
            {
                _currentStoryIndex = gameData.storyProgress.currentStoryIndex;
                if (_currentStoryIndex < _storyDataList.Count)
                {
                    StoryData currentStory = _storyDataList[_currentStoryIndex];
                    currentStory.currentChapterIndex = gameData.storyProgress.currentChapterIndex;
                    currentStory.currentSceneIndex = gameData.storyProgress.currentSceneIndex;
                    LoadCurrentScene();
                }
            }
        }

        public void UpdateGameData()
        {
            GameData gameData = GameManager.Instance.CurrentGameData;
            
            // 現在のストーリー進行状況を保存
            if (gameData.storyProgress == null)
            {
                gameData.storyProgress = new StoryProgress();
            }

            gameData.storyProgress.currentStoryIndex = _currentStoryIndex;
            if (_currentStoryIndex < _storyDataList.Count)
            {
                StoryData currentStory = _storyDataList[_currentStoryIndex];
                gameData.storyProgress.currentChapterIndex = currentStory.currentChapterIndex;
                gameData.storyProgress.currentSceneIndex = currentStory.currentSceneIndex;
            }
        }

        public void StartStory()
        {
            if (_storyDataList.Count == 0)
            {
                Debug.LogError("No StoryData available in the StoryManager.");
                return;
            }

            _currentStoryIndex = 0;
            StoryData currentStory = _storyDataList[_currentStoryIndex];
            currentStory.currentChapterIndex = 0;
            currentStory.currentSceneIndex = 0;
            LoadCurrentScene();
        }

        public void MoveToNextScene()
        {
            if (_currentStoryIndex >= _storyDataList.Count)
            {
                Debug.LogError("Invalid story index.");
                return;
            }

            StoryData currentStory = _storyDataList[_currentStoryIndex];
            currentStory.MoveToNextScene();
            LoadCurrentScene();
        }

        public void LoadChapter(int chapterIndex)
        {
            StoryData currentStory = _storyDataList[_currentStoryIndex];
            if (chapterIndex < 0 || chapterIndex >= currentStory.chapters.Count)
            {
                Debug.LogError($"Invalid chapter index: {chapterIndex}");
                return;
            }

            currentStory.currentChapterIndex = chapterIndex;
            currentStory.currentSceneIndex = 0;
            LoadCurrentScene();
        }

        public void LoadChapterScene(int chapterIndex, int sceneIndex)
        {
            StoryData currentStory = _storyDataList[_currentStoryIndex];
            if (chapterIndex < 0 || chapterIndex >= currentStory.chapters.Count)
            {
                Debug.LogError($"Invalid chapter index: {chapterIndex}");
                return;
            }

            if (sceneIndex < 0 || sceneIndex >= currentStory.chapters[chapterIndex].scenes.Count)
            {
                Debug.LogError($"Invalid scene index: {sceneIndex} for chapter: {chapterIndex}");
                return;
            }

            currentStory.currentChapterIndex = chapterIndex;
            currentStory.currentSceneIndex = sceneIndex;
            LoadCurrentScene();
        }

        public int GetCurrentChapterIndex()
        {
            return _storyDataList[_currentStoryIndex].currentChapterIndex;
        }

        public int GetCurrentSceneIndex()
        {
            return _storyDataList[_currentStoryIndex].currentSceneIndex;
        }

        public StoryData.ChapterData GetCurrentChapter()
        {
            StoryData currentStory = _storyDataList[_currentStoryIndex];
            if (currentStory.currentChapterIndex < currentStory.chapters.Count)
            {
                return currentStory.chapters[currentStory.currentChapterIndex];
            }
            return null;
        }

        private void LoadCurrentScene()
        {
            StartCoroutine(LoadSceneCoroutine());
        }

        private IEnumerator LoadSceneCoroutine()
        {
            // 前のストーリーシーンをすべてアンロード
            foreach (var scene in _loadedStoryScenes)
            {
                if (scene.isLoaded) // シーンがまだロードされているか確認
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
            _loadedStoryScenes.Clear(); // リストをクリア

            ClearSpawnedObjects();

            StoryData currentStory = _storyDataList[_currentStoryIndex];
            StoryData.SceneData currentScene = currentStory.GetCurrentScene();
            if (currentScene != null)
            {
                // 新しいシーンをロード
                yield return SceneManager.LoadSceneAsync(currentScene.sceneReference.scenePath, LoadSceneMode.Additive);
                
                // 新しくロードしたシーンを取得して保存
                Scene newScene = SceneManager.GetSceneByPath(currentScene.sceneReference.scenePath);
                _loadedStoryScenes.Add(newScene);
                
                // アクティブシーンとして設定
                SceneManager.SetActiveScene(newScene);

                SpawnSceneEvents(currentScene);
            }
            else
            {
                Debug.Log("Current story completed or no more scenes available.");
            }
            ChapterLoaded?.Invoke();
        }

        // オプション: シーンがアンロードされたことを確認するユーティリティメソッド
        private bool IsSceneLoaded(Scene scene)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i) == scene)
                    return true;
            }
            return false;
        }

        // オプション: 明示的にすべてのストーリーシーンをアンロードするメソッド
        public void UnloadAllStoryScenes()
        {
            StartCoroutine(UnloadAllStoryScenesCoroutine());
        }

        private IEnumerator UnloadAllStoryScenesCoroutine()
        {
            foreach (var scene in _loadedStoryScenes)
            {
                if (scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
            _loadedStoryScenes.Clear();
        }
        private void SpawnSceneEvents(StoryData.SceneData sceneData)
        {
            foreach (var eventData in sceneData.events)
            {
                if (eventData.eventPrefab != null)
                {
                    GameObject spawnedEvent = Instantiate(eventData.eventPrefab);
                    _spawnedObjects.Add(spawnedEvent);
                }
            }
        }

        private void ClearSpawnedObjects()
        {
            foreach (var obj in _spawnedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            _spawnedObjects.Clear();
        }

        // 将来的に複数のストーリーを切り替える場合に使用できるメソッド
        public void SwitchToStory(int storyIndex)
        {
            if (storyIndex < 0 || storyIndex >= _storyDataList.Count)
            {
                Debug.LogError($"Invalid story index: {storyIndex}");
                return;
            }

            _currentStoryIndex = storyIndex;
            StoryData newStory = _storyDataList[_currentStoryIndex];
            newStory.currentChapterIndex = 0;
            newStory.currentSceneIndex = 0;
            LoadCurrentScene();
        }

        public StoryProgress GetCurrentProgress()
        {
            return new StoryProgress
            {
                currentStoryIndex = _currentStoryIndex,
                currentChapterIndex = _storyDataList[_currentStoryIndex].currentChapterIndex,
                currentSceneIndex = _storyDataList[_currentStoryIndex].currentSceneIndex
            };
        }

        // エディタ拡張用のメソッド
        public List<StoryData> GetStoryList()
        {
            return _storyDataList;
        }
    }
}