using UnityEngine;

namespace AnoGame.Application.Audio
{
    /// <summary>
    /// プレイヤーとの距離に応じて心音（AudioSource）の音量とピッチ（テンポ）を制御するコンポーネント
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class HeartbeatController : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Transform playerTransform;

        [Header("Distance Settings")]
        [Tooltip("音が聞こえ始める最大距離")]
        [SerializeField] private float maxDistance = 20.0f;
        [Tooltip("音量が最大になる最小距離")]
        [SerializeField] private float minDistance = 2.0f;

        [Header("Audio Settings")]
        [Tooltip("最大音量")]
        [SerializeField, Range(0f, 1f)] private float maxVolume = 1.0f;
        [Tooltip("最も遠い時のピッチ（遅い）")]
        [SerializeField] private float minPitch = 1.0f;
        [Tooltip("最も近い時のピッチ（速い）")]
        [SerializeField] private float maxPitch = 2.0f;

        [Header("Fade Settings")]
        [SerializeField] private float stopFadeDuration = 1.0f;

        private AudioSource _audioSource;
        private bool _isRunning = false;
        private Coroutine _stopFadeCoroutine;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var go = GameObject.FindWithTag(playerTag);
                if (go != null) playerTransform = go.transform;
            }
        }

        /// <summary>
        /// 心音制御を開始する
        /// </summary>
        [ContextMenu("Play")]
        public void Play()
        {
            if (_stopFadeCoroutine != null) StopCoroutine(_stopFadeCoroutine);
            _isRunning = true;
            _audioSource.volume = 0f; // Reset volume to 0 to fade in naturally by Update
            // Start playing immediately if not playing, though volume will be updated in Update
            if (!_audioSource.isPlaying) _audioSource.Play();
        }

        /// <summary>
        /// 心音制御を停止する（フェードアウト付き・設定値を使用）
        /// </summary>
        [ContextMenu("Stop")]
        public void Stop()
        {
            Stop(stopFadeDuration);
        }

        /// <summary>
        /// 心音制御を停止する（フェードアウト時間指定）
        /// UnityEvent（Dynamic Float）対応
        /// </summary>
        /// <param name="duration"></param>
        public void Stop(float duration)
        {
            if (!_isRunning) return; // Already stopped or stopping
            _isRunning = false;

            if (gameObject.activeInHierarchy)
            {
                if (_stopFadeCoroutine != null) StopCoroutine(_stopFadeCoroutine);
                _stopFadeCoroutine = StartCoroutine(FadeOutAndStop(duration));
            }
            else
            {
                _audioSource.Stop();
                _audioSource.volume = 0f;
            }
        }

        /// <summary>
        /// 即時停止
        /// </summary>
        [ContextMenu("Stop Immediate")]
        public void StopImmediate()
        {
            if (_stopFadeCoroutine != null) StopCoroutine(_stopFadeCoroutine);
            _isRunning = false;

            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.volume = 0f;
            }
        }

        private System.Collections.IEnumerator FadeOutAndStop(float duration)
        {
            float startVol = _audioSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _audioSource.volume = Mathf.Lerp(startVol, 0f, t);
                yield return null;
            }

            _audioSource.volume = 0f;
            _audioSource.Stop();
        }

        private void Update()
        {
            if (playerTransform == null || _audioSource == null) return;

            // 停止中（フェードアウト含む）はUpdateでの音量制御を行わない
            if (!_isRunning) return;

            float distance = Vector3.Distance(transform.position, playerTransform.position);

            // 範囲外ならミュート（または停止）
            if (distance > maxDistance)
            {
                _audioSource.volume = 0f;
                return;
            }

            // minDistance ～ maxDistance の間で 0.0 ～ 1.0 を計算 (近いほど 1.0)
            // Mathf.InverseLerp(a, b, v) は vがaなら0, bなら1。
            // ここでは 近い(min) -> 1, 遠い(max) -> 0 にしたいので
            // 1 - InverseLerp(min, max, dist)
            float t = 1.0f - Mathf.InverseLerp(minDistance, maxDistance, distance);

            // 音量: t に応じて 0 ～ maxVolume
            _audioSource.volume = t * maxVolume;

            // ピッチ: t に応じて minPitch ～ maxPitch
            _audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, t);

            // 必要なら再生状態管理（聞こえる範囲に入ったらPlayする等）
            // ここでは常にループ再生されている前提で音量のみ操作
            if (!_audioSource.isPlaying && _audioSource.volume > 0.01f)
            {
                _audioSource.Play();
            }
        }
    }
}
