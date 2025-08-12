using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace AnoGame.Application.Audio
{
    public class AudioController : MonoBehaviour
    {
        AudioSource _audioSource;

        private float _maxVolume;

        [SerializeField]
        private float _fadeInDuration = 3.0f;

        [SerializeField]
        private float _fadeOutDuration = 5.0f;

        void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            _maxVolume = _audioSource.volume;
            _audioSource.volume = 0;
        }

        public void Play()
        {
            StartCoroutine(PlayFadeIn());
        }

        public void Stop()
        {
            StartCoroutine(PlayFadeOut());
        }

        private IEnumerator PlayFadeIn()
        {
            _audioSource.volume = 0;
            _audioSource.Play();

            float elapsedTime = 0;
            while (elapsedTime < _fadeInDuration)
            {
                _audioSource.volume = Mathf.Lerp(0, _maxVolume, elapsedTime / _fadeInDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            _audioSource.volume = _maxVolume;
        }

        private IEnumerator PlayFadeOut()
        {
            float elapsedTime = 0;
            while (elapsedTime < _fadeOutDuration)
            {
                _audioSource.volume = Mathf.Lerp(_maxVolume, 0, elapsedTime / _fadeOutDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            _audioSource.volume = 0;
            _audioSource.Stop();
        }

    }
}
