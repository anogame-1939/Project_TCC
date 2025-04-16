using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

namespace AnoGame.Application.Title
{
    public class TitleSceneLoader : MonoBehaviour
    {
        // インスペクターで次のシーン名とEventSystemプレハブを指定できるようにします
        [SerializeField] private string nextSceneName;
        [SerializeField] private GameObject eventSystemPrefab;

        // シーンロード開始時に非同期処理を開始します
        public void LoadNextScene()
        {
            StartCoroutine(LoadNextSceneCor());
        }

        // 非同期でシーンをロードし、完了後にEventSystemの初期化と現在のシーンをアンロードするコルーチン
        private IEnumerator LoadNextSceneCor()
        {
            // 現在のシーン（タイトルシーン）を取得
            Scene currentScene = SceneManager.GetActiveScene();

            // 指定されたシーンをAdditiveモードで非同期ロード
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);

            // ロードが完了するまで待機
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // ロード完了後、追加ロードしたシーンを取得し、アクティブに設定
            Scene newScene = SceneManager.GetSceneByName(nextSceneName);
            SceneManager.SetActiveScene(newScene);



            // 現在のシーン（タイトルシーン）をアンロード
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(currentScene);

            // アンロードが完了するまで待機
            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            // 新シーン上に存在するEventSystemを全て削除
            foreach (var ev in FindObjectsOfType<EventSystem>())
            {
                // 現在アクティブな新シーンに属している場合のみ対象とする
                if (ev.gameObject.scene == newScene)
                {
                    Destroy(ev.gameObject);
                }
            }

            // シリアライズフィールドに設定されたEventSystemプレハブを生成
            if (eventSystemPrefab != null)
            {
                Instantiate(eventSystemPrefab);
            }
            else
            {
                Debug.LogWarning("EventSystemPrefabが設定されていません。");
            }
        }
    }
}
