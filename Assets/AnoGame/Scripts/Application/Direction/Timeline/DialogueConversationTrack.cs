using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using PixelCrushers.DialogueSystem;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackClipType(typeof(DialogueConversationClip))]
    [TrackBindingType(typeof(DialogueSystemTrigger))] // ← ここを Trigger に
    public class DialogueConversationTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            // 入力を単純に通すだけの空ミキサー
            return ScriptPlayable<DialogueConversationMixer>.Create(graph, inputCount);
        }
    }

    // 空ミキサー（必須ではないが型を分けると安全）
    public sealed class DialogueConversationMixer : PlayableBehaviour { }
}
