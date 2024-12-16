using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AnoGame.Infrastructure.Scene;
using UnityEngine.SceneManagement;
using System.Linq;

namespace AnoGame.Application.Lore
{
    public class LoreSceneManager : MonoBehaviour
    {
        [SerializeField]
        private List<SceneReference> _persistentScenes = new List<SceneReference>();

        private ISceneLoader _sceneLoader;
        private List<string> _loadedScenePaths = new List<string>();

        private void Awake()
        {
            _sceneLoader = new SceneLoader();
            LoadPersistentScenesAsync().Forget();
        }

        private async UniTaskVoid LoadPersistentScenesAsync()
        {
            foreach (var scene in _persistentScenes)
            {
                Debug.Log("読み込んだで");
                if (string.IsNullOrEmpty(scene.ScenePath)) continue;
                Debug.Log("読み込んだで1");
                if (_loadedScenePaths.Contains(scene.ScenePath)) continue;

                Debug.Log("読み込んだで2");

                await _sceneLoader.LoadSceneAsync(scene, LoadSceneMode.Additive);
                _loadedScenePaths.Add(scene.ScenePath);

                Debug.Log("読み込んだで3");
            }
        }

        private void OnDestroy()
        {
            UnloadPersistentScenesAsync().Forget();
        }

        private async UniTaskVoid UnloadPersistentScenesAsync()
        {
            foreach (var scenePath in _loadedScenePaths)
            {
                // _persistentScenesから対応するSceneReferenceを探す
                var sceneRef = _persistentScenes.FirstOrDefault(s => s.ScenePath == scenePath);
                if (sceneRef != null)
                {
                    await _sceneLoader.UnloadSceneAsync(sceneRef);
                }
            }
            _loadedScenePaths.Clear();
        }

        // 外部からシーンの状態を確認できるようにするメソッド
        public bool IsSceneLoaded(SceneReference scene)
        {
            return _loadedScenePaths.Contains(scene.ScenePath);
        }

        public List<string> GetLoadedScenePaths()
        {
            return new List<string>(_loadedScenePaths);
        }

        // 必要に応じて個別のシーンをロード/アンロードするメソッド
        public async UniTask LoadSceneAsync(SceneReference scene)
        {
            if (string.IsNullOrEmpty(scene.ScenePath)) return;
            if (_loadedScenePaths.Contains(scene.ScenePath)) return;

            await _sceneLoader.LoadSceneAsync(scene, LoadSceneMode.Additive);
            _loadedScenePaths.Add(scene.ScenePath);
        }

        public async UniTask UnloadSceneAsync(SceneReference scene)
        {
            if (string.IsNullOrEmpty(scene.ScenePath)) return;
            if (!_loadedScenePaths.Contains(scene.ScenePath)) return;

            await _sceneLoader.UnloadSceneAsync(scene);
            _loadedScenePaths.Remove(scene.ScenePath);
        }
    }
}