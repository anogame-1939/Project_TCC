using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using AnoGame.Core;

namespace AnoGame.Event
{
    public class StoryManager : SingletonMonoBehaviour<StoryManager>
    {   
        [SerializeField]
        private List<StoryData> _storyDataList;

        private List<GameObject> _spawnedObjects = new List<GameObject>();
        private int _currentStoryIndex = 0;

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
            if (SceneManager.sceneCount > 1)
            {
                yield return SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
            }

            ClearSpawnedObjects();

            StoryData currentStory = _storyDataList[_currentStoryIndex];
            StoryData.SceneData currentScene = currentStory.GetCurrentScene();
            if (currentScene != null)
            {
                yield return SceneManager.LoadSceneAsync(currentScene.sceneReference.scenePath, LoadSceneMode.Additive);

                Scene newScene = SceneManager.GetSceneByPath(currentScene.sceneReference.scenePath);
                SceneManager.SetActiveScene(newScene);

                SpawnSceneEvents(currentScene);
            }
            else
            {
                Debug.Log("Current story completed or no more scenes available.");
            }
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
    }
}