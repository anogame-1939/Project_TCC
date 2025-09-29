using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using PixelCrushers.DialogueSystem;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackClipType(typeof(DialogueConversationClip))]
    public class DialogueConversationTrack : TrackAsset
    {
        [Header("New Clip Defaults")]
        [Range(0.05f, 5f)]
        public double newClipLengthScale = 1.0 / 3.0;   // 既定長の3分の1
        [Min(0.01f)]
        public double minNewClipSeconds = 0.25;         // あまりに短いのを防ぐ下限

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            // 入力を単純に通すだけの空ミキサー
            return ScriptPlayable<DialogueConversationMixer>.Create(graph, inputCount);
        }

        protected override void OnCreateClip(TimelineClip clip)
        {
            base.OnCreateClip(clip);
            clip.duration = System.Math.Max(minNewClipSeconds, clip.duration * newClipLengthScale);
            // 任意：初期名
            clip.displayName = "会話";
        }
    }

    // 空ミキサー（必須ではないが型を分けると安全）
    public sealed class DialogueConversationMixer : PlayableBehaviour { }
}
