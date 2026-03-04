using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnoGame.Application.Gimmicks;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackColor(1f, 0.9f, 0.4f)]
    [TrackBindingType(typeof(StreetlightFxBlink))]
    [TrackClipType(typeof(StreetlightFxBlinkClip))]
    [TrackClipType(typeof(StreetlightFxCurveClip))]
    public class StreetlightFxBlinkTrack : TrackAsset
    {
        [SerializeField]
        private string bindingId;   // シーンの StreetlightFxBlink と一致させる
        public string BindingId => bindingId;

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            // 複数クリップが重なったとき用にミキサーを用意しておく
            return ScriptPlayable<StreetlightFxBlinkMixer>.Create(graph, inputCount);
        }
    }
}
