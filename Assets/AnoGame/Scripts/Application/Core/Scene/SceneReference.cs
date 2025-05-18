using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Cysharp.Threading.Tasks;

namespace AnoGame.Application.Core.Scene
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
        void HideGameObjectsInScene(SceneReference scene);
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

        public void HideGameObjectsInScene(SceneReference scene)
        {
            // シーン名からシーンを取得
            UnityEngine.SceneManagement.Scene targetScene = SceneManager.GetSceneByName(scene.ScenePath);

            // シーンが存在するかどうかを確認
            if (!targetScene.IsValid())
            {
                Debug.LogError($"Scene not found: {scene.ScenePath}");
                return;
            }

            // シーン内のすべてのルートゲームオブジェクトを取得
            GameObject[] rootObjects = targetScene.GetRootGameObjects();

            // 各ルートゲームオブジェクトを非アクティブにする
            foreach (GameObject rootObject in rootObjects)
            {
                rootObject.SetActive(false);
            }
        }
    }
}