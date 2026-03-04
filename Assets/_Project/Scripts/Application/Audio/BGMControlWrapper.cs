using System.Collections;
using UnityEngine;

namespace AnoGame.Application.Audio
{
    /// <summary>
    /// AudioSourceをラップし、再生・停止（フェード付き）を制御するコンポーネント
    /// EnemyBehaviorCoordinatorのイベントから呼び出すことを想定
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BGMControlWrapper : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float fadeDuration = 1.0f;
        [SerializeField] private float targetVolume = 1.0f;

        private Coroutine _fadeCoroutine;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                // 音量は初期設定に合わせておく（再生中ならtargetVolumeか、あるいは0か）
                // ここでは勝手に再生しないので何もしないが、playOnAwakeの場合は注意
            }
        }

        /// <summary>
        /// 再生開始（フェードインなし、即時音量セット）
        /// </summary>
        public void Play()
        {
            if (audioSource == null) return;

            // フェード停止中ならキャンセル
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            audioSource.volume = targetVolume;
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        /// <summary>
        /// 停止（フェードアウト付き）
        /// </summary>
        public void Stop()
        {
            if (audioSource == null || !audioSource.isPlaying) return;

            if (gameObject.activeInHierarchy)
            {
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeOutAndStop());
            }
            else
            {
                audioSource.Stop();
                audioSource.volume = targetVolume; // 次回用に戻すかどうかは仕様次第だが一応戻すか、Playで戻してるのでOK
            }
        }

        /// <summary>
        /// 即時停止
        /// </summary>
        public void StopImmediate()
        {
            if (audioSource == null) return;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            audioSource.Stop();
        }

        private IEnumerator FadeOutAndStop()
        {
            float startVol = audioSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                audioSource.volume = Mathf.Lerp(startVol, 0f, t);
                yield return null;
            }

            audioSource.volume = 0f;
            audioSource.Stop();
            audioSource.volume = targetVolume; // Reset for next Play
            _fadeCoroutine = null;
        }
    }
}
