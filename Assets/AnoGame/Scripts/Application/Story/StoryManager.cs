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
        private Scene _mainScene;
        private bool _isLoadingScene = false;

        private void Awake()
        {
            _mainScene = SceneManager.GetActiveScene();
            GameManager.Instance.LoadGameData += OnLoadGameData;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadGameData -= OnLoadGameData;
            }
        }

        private void OnLoadGameData(GameData gameData)
        {
            if (gameData == null || gameData.storyProgress == null) return;

            _currentStoryIndex = gameData.storyProgress.currentStoryIndex;
            if (_currentStoryIndex < _storyDataList.Count)
            {
                StoryData currentStory = _storyDataList[_currentStoryIndex];
                currentStory.currentChapterIndex = gameData.storyProgress.currentChapterIndex;
                currentStory.currentSceneIndex = gameData.storyProgress.currentSceneIndex;
                LoadCurrentScene();
            }
        }

        public void UpdateGameData()
        {
            GameData gameData = GameManager.Instance.CurrentGameData;
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

        private void LoadCurrentScene()
        {
            StartCoroutine(LoadSceneCoroutine());
        }

        private IEnumerator LoadSceneCoroutine()
        {
            if (_isLoadingScene)
            {
                Debug.LogWarning("Scene loading is already in progress");
                yield break;
            }

            _isLoadingScene = true;

            yield return UnloadCurrentScenesCoroutine();
            yield return LoadNewSceneCoroutine();

            _isLoadingScene = false;
            ChapterLoaded?.Invoke();
        }

        private IEnumerator LoadNewSceneCoroutine()
        {
            StoryData currentStory = _storyDataList[_currentStoryIndex];
            StoryData.SceneData currentScene = currentStory.GetCurrentScene();
            
            if (currentScene == null)
            {
                Debug.Log("Current story completed or no more scenes available.");
                yield break;
            }

            AsyncOperation loadOperation = null;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(
                    currentScene.sceneReference.scenePath, 
                    LoadSceneMode.Additive
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start scene loading: {ex.Message}");
                yield break;
            }

            if (loadOperation == null)
            {
                Debug.LogError("Failed to start scene loading operation");
                yield break;
            }

            yield return loadOperation;

            Scene newScene = SceneManager.GetSceneByPath(
                currentScene.sceneReference.scenePath
            );
            
            if (newScene.IsValid())
            {
                _loadedStoryScenes.Add(newScene);
                SceneManager.SetActiveScene(newScene);
                yield return SpawnSceneEventsCoroutine(currentScene);
            }
            else
            {
                Debug.LogError($"Failed to load scene: {currentScene.sceneReference.scenePath}");
            }
        }

        private IEnumerator UnloadCurrentScenesCoroutine()
        {
            var scenesToUnload = new List<Scene>(_loadedStoryScenes);
            
            foreach (var scene in scenesToUnload)
            {
                if (!scene.isLoaded || !scene.IsValid()) continue;

                AsyncOperation unloadOperation = null;
                try
                {
                    unloadOperation = SceneManager.UnloadSceneAsync(scene);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to unload scene {scene.path}: {ex.Message}");
                    continue;
                }

                if (unloadOperation != null)
                {
                    yield return unloadOperation;
                }
            }

            ClearSpawnedObjects();
            _loadedStoryScenes.Clear();

            if (_mainScene.IsValid())
            {
                SceneManager.SetActiveScene(_mainScene);
            }
        }

        private IEnumerator SpawnSceneEventsCoroutine(StoryData.SceneData sceneData)
        {
            foreach (var eventData in sceneData.events)
            {
                if (eventData.eventPrefab == null) continue;

                try
                {
                    GameObject spawnedEvent = Instantiate(eventData.eventPrefab);
                    _spawnedObjects.Add(spawnedEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to spawn event: {ex.Message}");
                }

                yield return null;
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

        public List<StoryData> GetStoryList()
        {
            return _storyDataList;
        }

        public bool IsLoadingScene()
        {
            return _isLoadingScene;
        }
    }
}