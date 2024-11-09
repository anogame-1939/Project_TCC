using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Cysharp.Threading.Tasks;

namespace AnoGame.Infrastructure.Scene
{
    [System.Serializable]
    public class SceneReference
    {
        [SerializeField] private string scenePath;
        public string ScenePath => scenePath;

#if UNITY_EDITOR
        [SerializeField] private UnityEngine.Object sceneAsset;
#endif
    }

    public interface ISceneLoader
    {
        UniTask LoadSceneAsync(SceneReference scene, LoadSceneMode mode = LoadSceneMode.Single);
        UniTask UnloadSceneAsync(SceneReference scene);
    }

    public class SceneLoader : ISceneLoader
    {
        public async UniTask LoadSceneAsync(SceneReference scene, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrEmpty(scene.ScenePath))
            {
                Debug.LogError("Scene path is empty");
                return;
            }

            try
            {
                await SceneManager.LoadSceneAsync(scene.ScenePath, mode);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load scene: {e.Message}");
            }
        }

        public async UniTask UnloadSceneAsync(SceneReference scene)
        {
            if (string.IsNullOrEmpty(scene.ScenePath))
            {
                Debug.LogError("Scene path is empty");
                return;
            }

            try
            {
                await SceneManager.UnloadSceneAsync(scene.ScenePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to unload scene: {e.Message}");
            }
        }
    }
}