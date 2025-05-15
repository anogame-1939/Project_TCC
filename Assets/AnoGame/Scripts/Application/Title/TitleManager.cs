using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnoGame.Application.Title
{
    public class TitleManager : MonoBehaviour
    {
        [SerializeField] private string titleSceneName;

        /// <summary>
        /// DontDestroyOnLoad で保持されたオブジェクトが配置される内部シーンを取得する。
        /// ※ この方法は、一時的にDontDestroyOnLoad対象のオブジェクトを作成して、その属するシーンを参照するテクニックです。
        /// </summary>
        /// <returns>DontDestroyOnLoad の内部シーン</returns>
        private UnityEngine.SceneManagement.Scene GetDontDestroyOnLoadScene()
        {
            // 一時的なゲームオブジェクトを作成し、DontDestroyOnLoad を付与する
            GameObject temp = new GameObject("Temp");
            DontDestroyOnLoad(temp);

            // 一時オブジェクトが属するシーンが、DontDestroyOnLoad対象のシーンとなる
            UnityEngine.SceneManagement.Scene dontDestroyOnLoadScene = temp.scene;

            // 一時オブジェクトは不要なので削除する
            Destroy(temp);

            return dontDestroyOnLoadScene;
        }

        /// <summary>
        /// タイトル画面に戻る際に DontDestroyOnLoad のオブジェクト群を一括削除し、タイトルシーンをロードする。
        /// </summary>
        public void ReturnToTitle()
        {
            // DontDestroyOnLoad オブジェクトが配置されているシーンを取得
            UnityEngine.SceneManagement.Scene dontDestroyOnLoadScene = GetDontDestroyOnLoadScene();

            // このシーン内の全ルートオブジェクトを取得する
            List<GameObject> rootObjects = new List<GameObject>();
            dontDestroyOnLoadScene.GetRootGameObjects(rootObjects);

            // 取得した各オブジェクトを削除する（必要に応じて、特定のオブジェクトだけ削除する処理に変更可能）
            foreach (GameObject obj in rootObjects)
            {
                Destroy(obj);
            }

            // タイトルシーン (シーン名："TitleScene") をロードする
            SceneManager.LoadScene(titleSceneName);
        }
    }
}