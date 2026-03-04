using System.Collections;
using UnityEngine;

namespace AnoGame.Application.Audio
{
    /// <summary>
    /// ・初回は 0 秒から再生
    /// ・loopEnd に到達したら、もう一方の AudioSource を loopStart に合わせて Play しつつ、
    ///   fadeMs で A→B のクロスフェード（片方下げ・片方上げ）
    /// ・以降は loopStart～loopEnd をクロスフェードでループ
    /// ・PlayScheduled は使わず、コルーチンで音量制御のみ
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioLooperSimple : MonoBehaviour
    {
        [Header("ループ範囲 (0〜1)")]
        [Range(0f, 1f)] public float loopStart = 0f;
        [Range(0f, 1f)] public float loopEnd   = 1f;

        [Header("フェード (ms)")]
        [Tooltip("クロスフェード長（片方下げ・片方上げ）。0で即時切替。")]
        [Range(0f, 5000f)] public float fadeMs = 8f;

        [Header("挙動")]
        [Tooltip("Time.timeScale の影響を受けないフェードにするか")]
        public bool useUnscaledTime = true;

        [Tooltip("ログ出力（デバッグ用）")]
        public bool verboseLog = false;

        // 実体
        private AudioSource _a; // 現在のメイン（再生中）
        private AudioSource _b; // 次に鳴らす側（待機〜再生）
        private AudioClip   _clip;

        // ループ端点（サンプル）
        private int _loopStartSamples;
        private int _loopEndSamples;

        // フェード中フラグ（二重起動防止）
        private bool _isFading;

#if UNITY_EDITOR
        [Header("Editor テスト (ビルド除外)")]
        [Range(0f,1f)] public float testSeekNormalized = 0f;

        [ContextMenu("Editor/Seek To (Normalized)")]
        private void EditorSeek()
        {
            if (!_clip) return;
            int smp = Mathf.RoundToInt(Mathf.Clamp01(testSeekNormalized) * _clip.samples);
            _a.Stop(); _b.Stop();
            _a.timeSamples = Mathf.Clamp(smp, 0, _clip.samples - 1);
            _a.volume = 1f; _b.volume = 0f;
            _a.Play();
            if (verboseLog) Debug.Log($"[AudioLooperSimple] Seek A to {smp} samples");
        }

        [ContextMenu("Editor/Jump To Loop Start")]
        private void EditorJumpLoopStart()
        {
            if (!_clip) return;
            _a.Stop(); _b.Stop();
            _a.timeSamples = _loopStartSamples;
            _a.volume = 1f; _b.volume = 0f;
            _a.Play();
            if (verboseLog) Debug.Log($"[AudioLooperSimple] Jump A to loopStart {_loopStartSamples}");
        }

        [ContextMenu("Editor/Jump To Loop End")]
        private void EditorJumpLoopEnd()
        {
            if (!_clip) return;
            _a.Stop(); _b.Stop();
            _a.timeSamples = _loopEndSamples;
            _a.volume = 1f; _b.volume = 0f;
            _a.Play();
            if (verboseLog) Debug.Log($"[AudioLooperSimple] Jump A to loopEnd {_loopEndSamples}");
        }
#endif

        void Awake()
        {
            // 既存の AudioSource を A として使い、B を動的追加
            _a = GetComponent<AudioSource>();
            _a.loop = false; _a.playOnAwake = false;

            _b = gameObject.AddComponent<AudioSource>();
            CopySettings(_a, _b);
            _b.loop = false; _b.playOnAwake = false;

            // 初期ゲイン
            _a.volume = 1f;
            _b.volume = 0f;
        }

        void Start()
        {
            _clip = _a.clip;
            if (!_clip)
            {
                Debug.LogWarning("[AudioLooperSimple] AudioClip が未設定です。");
                enabled = false; return;
            }

            RecalcLoopPoints();

            // 初回は 0 秒から A を再生
            _a.timeSamples = 0;
            _a.Play();
        }

        void Update()
        {
            if (!_clip || !_a || _isFading) return;

            // A が再生中で、ループ終端に達したらフェード開始
            // ※少し手前で仕込みたいので、fade 分の“余裕”を加味
            int cur = _a.timeSamples;
            int fadeSamples = Mathf.RoundToInt((_clip.frequency * fadeMs) / 1000f);
            int trigger = Mathf.Max(_loopStartSamples, _loopEndSamples - Mathf.Max(1, fadeSamples));

            if (cur >= trigger)
            {
                if (verboseLog) Debug.Log($"[AudioLooperSimple] CrossFade start at samples={cur}");
                StartCoroutine(CrossFadeAndSwap());
            }
        }

        private IEnumerator CrossFadeAndSwap()
        {
            _isFading = true;

            // B を loopStart にセットして再生開始
            _b.clip = _clip;
            _b.timeSamples = _loopStartSamples;
            _b.Play();

            float dur = Mathf.Max(0f, fadeMs / 1000f);
            if (dur <= 0f)
            {
                // 即時切替
                _a.volume = 0f;
                _b.volume = 1f;

                // A を止め、参照入れ替え
                _a.Stop();
                SwapAB();
                _isFading = false;
                yield break;
            }

            // フェード（線形：必要なら等電力に変更可）
            float t = 0f;
            while (t < 1f)
            {
                t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / dur;
                float k = Mathf.Clamp01(t);

                // 線形フェード：A 1→0、B 0→1
                _a.volume = 1f - k;
                _b.volume = k;

                yield return null;
            }

            // フェード完了後の整理
            _a.volume = 0f;
            _b.volume = 1f;

            // A を止めて入れ替え
            _a.Stop();
            SwapAB();

            _isFading = false;
        }

        private void SwapAB()
        {
            // A<->B を入れ替えて、常に「A が現行・B が待機」の形に保つ
            var tmp = _a; _a = _b; _b = tmp;

            // 待機側は常に無音・停止状態に戻しておく（次の仕込みに備える）
            _b.volume = 0f;
            // _b.Stop はフェード中に呼んでいるが、保険として明示
            if (_b.isPlaying && _b != _a) _b.Stop();
        }

        private void OnValidate()
        {
            if (_a && _a.clip)
            {
                loopStart = Mathf.Clamp01(loopStart);
                loopEnd   = Mathf.Clamp01(loopEnd);
                if (loopEnd < loopStart) loopEnd = loopStart;
                RecalcLoopPoints();
            }
        }

        private void RecalcLoopPoints()
        {
            int total = _a.clip.samples;

            _loopStartSamples = Mathf.RoundToInt(loopStart * total);
            _loopEndSamples   = Mathf.RoundToInt(loopEnd   * total);

            if (_loopEndSamples <= _loopStartSamples)
                _loopEndSamples = Mathf.Min(total - 1, _loopStartSamples + 1);
        }

        private static void CopySettings(AudioSource src, AudioSource dst)
        {
            if (!src || !dst) return;
            dst.outputAudioMixerGroup = src.outputAudioMixerGroup;
            dst.mute = src.mute;
            dst.bypassEffects = src.bypassEffects;
            dst.bypassListenerEffects = src.bypassListenerEffects;
            dst.bypassReverbZones = src.bypassReverbZones;
            dst.priority = src.priority;
            dst.pitch = src.pitch;
            dst.panStereo = src.panStereo;
            dst.spatialBlend = src.spatialBlend;
            dst.reverbZoneMix = src.reverbZoneMix;
            dst.dopplerLevel = src.dopplerLevel;
            dst.spread = src.spread;
            dst.minDistance = src.minDistance;
            dst.maxDistance = src.maxDistance;
            dst.rolloffMode = src.rolloffMode;
        }
    }
}
