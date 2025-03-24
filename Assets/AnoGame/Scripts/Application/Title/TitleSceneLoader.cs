using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

namespace AnoGame.Application.Title
{
    public class TitleSceneLoader : MonoBehaviour
    {
        // インスペクターで次のシーン名を指定できるようにします
        [SerializeField] private string nextSceneName;

        // Startが呼ばれる際に非同期処理を開始します
        public void LoadNextScene()
        {
            StartCoroutine(LoadNextSceneCor());
        }

        // 非同期でシーンをロードし、完了後に現在のシーンをアンロードするコルーチン
        private IEnumerator LoadNextSceneCor()
        {
            // 現在のシーンを取得
            Scene currentScene = SceneManager.GetActiveScene();

            // 指定されたシーンをAdditiveモードで非同期ロード
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);

            // ロードが完了するまで待機
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // ロード完了後、追加ロードしたシーンをアクティブに設定
            Scene newScene = SceneManager.GetSceneByName(nextSceneName);
            SceneManager.SetActiveScene(newScene);

            // 現在のシーンをアンロード
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(currentScene);

            // アンロードが完了するまで待機
            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            var playerInput = FindAnyObjectByType<PlayerInput>();
            playerInput.enabled = false;

            yield return null;

            playerInput.enabled = true;

        }
    }
}
