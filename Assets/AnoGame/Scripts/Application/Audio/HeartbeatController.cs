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

        private AudioSource _audioSource;

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

        private void Update()
        {
            if (playerTransform == null || _audioSource == null) return;

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
