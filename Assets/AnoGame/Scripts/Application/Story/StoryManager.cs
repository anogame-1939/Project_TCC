using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using AnoGame.Application.Core;
using AnoGame.Application.Data;
using AnoGame.Domain.Data.Models;
using AnoGame.Application.Core.Scene;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace AnoGame.Application.Story
{
    public class StoryManager : SingletonMonoBehaviour<StoryManager>
    {
        [SerializeField]
        string[] ignoreScenes;
        public event Action<bool> StoryLoaded;
        public event Action<bool> ChapterLoaded;
        
        [SerializeField]
        private List<StoryData> _storyDataList;

        private List<GameObject> _spawnedObjects = new List<GameObject>();
        private int _currentStoryIndex = 0;
        private int _currentChapterIndex = 0;

        private ISceneLoader _sceneLoader;
        private List<UnityEngine.SceneManagement.Scene> _loadedStoryScenes = new List<UnityEngine.SceneManagement.Scene>();
        private SceneReference _mainMapScene;
        
        // インスペクターでメインシーン名を設定
        [SerializeField]
        private string mainSceneName;
        private UnityEngine.SceneManagement.Scene _mainScene;
        public UnityEngine.SceneManagement.Scene MainScene => _mainScene;
        
        private bool _isLoadingScene = false;

        private void Awake()
        {
            _sceneLoader = new SceneLoader();
            // GetActiveScene() は使わず、mainSceneName からシーンを取得する
            _mainScene = SceneManager.GetSceneByName(mainSceneName);
            GameManager2.Instance.LoadGameData += OnLoadGameData;
        }

        private void Start()
        {
            // ゲームデータがロード済みの場合、ロード済みのデータを使用してゲームを再開
            if (GameManager2.Instance.DataLoaded)
            {
                OnLoadGameData(GameManager2.Instance.CurrentGameData);
            }
        }

        private void OnDestroy()
        {
            if (GameManager2.Instance != null)
            {
                GameManager2.Instance.LoadGameData -= OnLoadGameData;
            }
        }

        private async void OnLoadGameData(GameData gameData)
        {
            if (gameData == null || gameData.StoryProgress == null) return;

            // ストーリ進捗状況をロード
            _currentStoryIndex = gameData.StoryProgress.CurrentStoryIndex;
            _currentChapterIndex = gameData.StoryProgress.CurrentChapterIndex;

            StoryData storyData = _storyDataList[_currentStoryIndex];
            if (_mainMapScene != storyData.mainMapScene)
            {
                SceneReference tmpMainScene;
                if (_mainMapScene != null)
                {
                    tmpMainScene = _mainMapScene;
                    Debug.Log($"{_mainMapScene.ScenePath} をアンロード");

                    // _sceneLoader.HideAllGameObjects(tmpMainScene);
                    
                    _mainMapScene = storyData.mainMapScene;
                    await LoadScenesAsync(storyData.mainMapScene);
                    await _sceneLoader.UnloadSceneAsync(tmpMainScene);
                }
                else
                {
                    _mainMapScene = storyData.mainMapScene;
                    await LoadScenesAsync(storyData.mainMapScene);
                }
            }

            LoadCurrentScene();
        }

        private async UniTask LoadScenesAsync(SceneReference mainMapScene)
        {
            await _sceneLoader.LoadSceneAsync(mainMapScene, LoadSceneMode.Additive);
        }

        public void UpdateGameData()
        {
            GameData gameData = GameManager2.Instance.CurrentGameData;
            if (gameData.StoryProgress == null)
            {
            }
            GameManager2.Instance.CurrentGameData.UpdateStoryProgress(new StoryProgress(_currentStoryIndex, _currentChapterIndex));
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
            LoadCurrentScene(false); // 次のシーンは常にスタートポイントから
        }

        public async void LoadStory(int storyIndex, bool useRetryPoint = false)
        {
            _currentStoryIndex = storyIndex;
            _currentChapterIndex = 0;

            StoryData storyData = _storyDataList[_currentStoryIndex];
            if (_mainMapScene != storyData.mainMapScene)
            {
                SceneReference tmpMainScene;
                if (_mainMapScene != null)
                {
                    tmpMainScene = _mainMapScene;
                    Debug.Log($"{_mainMapScene.ScenePath} をアンロード2");

                    // _sceneLoader.HideAllGameObjects(tmpMainScene);
                    
                    _mainMapScene = storyData.mainMapScene;
                    await LoadScenesAsync(storyData.mainMapScene);
                    await _sceneLoader.UnloadSceneAsync(tmpMainScene);
                }
                else
                {
                    _mainMapScene = storyData.mainMapScene;
                    await LoadScenesAsync(storyData.mainMapScene);
                }
            }
            StoryLoaded?.Invoke(useRetryPoint);

            LoadCurrentScene(useRetryPoint);
        }

        public void LoadChapter(int chapterIndex, bool useRetryPoint = false)
        {
            _currentChapterIndex = chapterIndex;
            StoryData currentStory = _storyDataList[_currentStoryIndex];
            if (chapterIndex < 0 || chapterIndex >= currentStory.chapters.Count)
            {
                Debug.LogError($"Invalid chapter index: {chapterIndex}");
                return;
            }
            LoadCurrentScene(useRetryPoint);
        }

        public void LoadChapter2(int chapterIndex, bool useRetryPoint = false)
        {
            _currentChapterIndex = chapterIndex;
            StoryData currentStory = _storyDataList[_currentStoryIndex];
            if (chapterIndex < 0 || chapterIndex >= currentStory.chapters.Count)
            {
                Debug.LogError($"Invalid chapter index: {chapterIndex}");
                return;
            }
            LoadCurrentScene(useRetryPoint);
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

        public void RetyrCurrentScene()
        {
            LoadCurrentScene(true);
        }

        private void LoadCurrentScene(bool useRetryPoint = false)
        {
            Debug.Log($"LoadCurrentScene:{useRetryPoint}");
            StartCoroutine(LoadSceneCoroutine(useRetryPoint));
        }

        private IEnumerator LoadSceneCoroutine(bool useRetryPoint)
        {
            // 変更後：インスペクターで設定した mainSceneName からシーンを取得して待機
            UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneByName(mainSceneName);
            while (!scene.isLoaded)
            {
                Debug.Log($"メインシーン {mainSceneName} の読み込みが完了するまで待機中...");
                yield return null;
                scene = SceneManager.GetSceneByName(mainSceneName);
            }
            // 正常に読み込まれたら _mainScene を更新
            _mainScene = scene;

            if (_isLoadingScene)
            {
                Debug.LogWarning("Scene loading is already in progress");
                yield break;
            }

            _isLoadingScene = true;

            yield return UnloadCurrentScenesCoroutine();
            yield return LoadNewSceneCoroutine();

            _isLoadingScene = false;
            
            // チャプターのロード完了イベントを通知
            ChapterLoaded?.Invoke(useRetryPoint);
        }

        private IEnumerator LoadNewSceneCoroutine()
        {
            StoryData currentStory = _storyDataList[_currentStoryIndex];
            StoryData.SceneData currentScene = currentStory.chapters[_currentChapterIndex].scenes[0];
            
            if (currentScene == null)
            {
                Debug.Log("Current story completed or no more scenes available.");
                yield break;
            }

            AsyncOperation loadOperation = null;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(
                    currentScene.sceneReference.ScenePath, 
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

            UnityEngine.SceneManagement.Scene newScene = SceneManager.GetSceneByPath(
                currentScene.sceneReference.ScenePath
            );
            
            if (newScene.IsValid())
            {
                _loadedStoryScenes.Add(newScene);
                SceneManager.SetActiveScene(newScene);
                yield return SpawnSceneEventsCoroutine(currentScene);
            }
            else
            {
                Debug.LogError($"Failed to load scene: {currentScene.sceneReference.ScenePath}");
            }
        }

        private IEnumerator UnloadCurrentScenesCoroutine()
        {
            var scenesToUnload = new List<UnityEngine.SceneManagement.Scene>(_loadedStoryScenes);
            Debug.Log($"Starting to unload {scenesToUnload.Count} story scenes");
            
            foreach (var scene in scenesToUnload)
            {
                if (ignoreScenes.Contains(scene.name))
                {
                    continue;
                }
                if (!scene.isLoaded || !scene.IsValid())
                    {
                        Debug.Log($"Skipping scene {scene.path}: not loaded or invalid");
                        continue;
                    }

                AsyncOperation unloadOperation = null;
                try
                {
                    Debug.Log($"Unloading scene: {scene.path}");
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
                    Debug.Log($"Successfully unloaded scene: {scene.path}");
                }
            }

            ClearSpawnedObjects();
            _loadedStoryScenes.Clear();

            if (_mainScene.IsValid())
            {
                Debug.Log($"Setting active scene back to main scene: {_mainScene.path}");
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

        public void SwitchToChapterInStory(int storyIndex, int chapterIndex, bool useRetryPoint = false)
        {
            // ストーリーとチャプターの状態を更新
            _currentStoryIndex = storyIndex;
            _currentChapterIndex = chapterIndex;
            UpdateGameData();

            // シーンのロードを一度だけ実行
            LoadCurrentScene(useRetryPoint);

            ChapterLoaded?.Invoke(useRetryPoint);
        }

        private void SwitchToStory(int storyIndex)
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
        }

        private void LoadChapter(int chapterIndex)
        {
            StoryData currentStory = _storyDataList[_currentStoryIndex];
            if (chapterIndex < 0 || chapterIndex >= currentStory.chapters.Count)
            {
                Debug.LogError($"Invalid chapter index: {chapterIndex}");
                return;
            }

            currentStory.currentChapterIndex = chapterIndex;
            currentStory.currentSceneIndex = 0;
        }

        public StoryProgress GetCurrentProgress()
        {
            return new StoryProgress
            (
                _currentStoryIndex,
                _storyDataList[_currentStoryIndex].currentChapterIndex
            );
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
