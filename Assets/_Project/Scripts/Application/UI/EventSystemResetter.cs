using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnoGame.Application.UI
{
    public class EventSystemResetter : MonoBehaviour
    {
        [Tooltip("シーンロード時に再起動したい EventSystem の GameObject を指定してください")]
        [SerializeField] private GameObject eventSystemObject;

        private void Awake()
        {
            // シーンロード完了時のコールバックを登録
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            // 念のため無効化時にコールバックを解除
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // シーンロードが走るたびに呼ばれる
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // コルーチンで一度非アクティブにしてから再度アクティブにする
            StartCoroutine(ResetEventSystemCoroutine());
        }

        private IEnumerator ResetEventSystemCoroutine()
        {
            if (eventSystemObject == null)
            {
                Debug.LogWarning("[EventSystemResetter] EventSystem オブジェクトが設定されていません。Inspector でアサインしてください。");
                yield break;
            }
            yield return new WaitForSeconds(1f);

            // 一度非アクティブにする
            eventSystemObject.SetActive(false);

            // 1秒待つ（必要に応じて WaitForSecondsRealtime に変えても可）
            yield return new WaitForSeconds(1f);

            // 改めてアクティブにする
            eventSystemObject.SetActive(true);
        }
    }

}