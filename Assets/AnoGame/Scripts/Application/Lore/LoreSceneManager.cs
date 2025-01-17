using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AnoGame.Application.Core.Scene;
using UnityEngine.SceneManagement;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
                if (string.IsNullOrEmpty(scene.ScenePath)) continue;
                if (_loadedScenePaths.Contains(scene.ScenePath)) continue;
                await _sceneLoader.LoadSceneAsync(scene, LoadSceneMode.Additive);
                _loadedScenePaths.Add(scene.ScenePath);
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateScenePaths();
            UpdateBuildSettings();
        }

        private void UpdateScenePaths()
        {
            // SerializedObjectを使用してシリアライズされたデータにアクセス
            var serializedObject = new SerializedObject(this);
            var scenesProperty = serializedObject.FindProperty("_persistentScenes");

            for (int i = 0; i < scenesProperty.arraySize; i++)
            {
                var sceneReferenceProperty = scenesProperty.GetArrayElementAtIndex(i);
                var sceneAssetProperty = sceneReferenceProperty.FindPropertyRelative("sceneAsset");
                var scenePathProperty = sceneReferenceProperty.FindPropertyRelative("scenePath");

                if (sceneAssetProperty.objectReferenceValue != null)
                {
                    var scenePath = AssetDatabase.GetAssetPath(sceneAssetProperty.objectReferenceValue);
                    scenePathProperty.stringValue = scenePath;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateBuildSettings()
        {
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            HashSet<string> addedScenePaths = new HashSet<string>();

            // LoreSceneManagerのシーンを追加
            foreach (var scene in _persistentScenes)
            {
                if (!string.IsNullOrEmpty(scene.ScenePath) && !addedScenePaths.Contains(scene.ScenePath))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(scene.ScenePath, true));
                    addedScenePaths.Add(scene.ScenePath);
                }
            }

            // 既存のビルド設定のシーンを保持
            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (!addedScenePaths.Contains(buildScene.path))
                {
                    buildScenes.Add(buildScene);
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("Build settings updated with scenes from LoreSceneManager");
        }

        [CustomEditor(typeof(LoreSceneManager))]
        public class LoreSceneManagerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                var manager = (LoreSceneManager)target;

                EditorGUILayout.Space();
                if (GUILayout.Button("Update Scene Paths"))
                {
                    manager.UpdateScenePaths();
                }

                if (GUILayout.Button("Update Build Settings"))
                {
                    manager.UpdateBuildSettings();
                }
            }
        }
#endif
    }
}