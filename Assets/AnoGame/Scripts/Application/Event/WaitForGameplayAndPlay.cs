using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using AnoGame.Application;

namespace AnoGame
{
    public class WaitForGameplayAndPlay : MonoBehaviour
    {
        [SerializeField] private PlayableDirector playableDirector;

        public void Play()
        {
            StartCoroutine(WaitForGameplayCoroutine());
        }

        private IEnumerator WaitForGameplayCoroutine()
        {
            yield return new WaitForSeconds(5f);
            // GameStateManager.Instance が null の場合を考慮して待つ
            while (GameStateManager.Instance == null)
            {
                yield return null;
            }

            // Gameplay 状態になるまで待機
            while (GameStateManager.Instance.CurrentState != GameState.Gameplay)
            {
                Debug.Log("Waiting for Gameplay state...");
                yield return null;
            }

            // Gameplay 状態になったら Play 実行
            if (playableDirector != null)
            {
                playableDirector.Play();
            }
            else
            {
                Debug.LogWarning("PlayableDirector が設定されていません。");
            }
        }
    }
}
