using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.AnoDialogue.Timeline
{
    /// <summary>
    /// シーン画像を制御する Timeline トラック。
    /// DialogueTimelineReceiver にバインドし、クロスシーン対応。
    /// クリップの長さがシーン画像の表示期間を表す。
    /// </summary>
    [TrackClipType(typeof(SceneImageClip))]
    [TrackBindingType(typeof(DialogueTimelineReceiver))]
    public class SceneImageTrack : TrackAsset
    {
        [Header("Clip Defaults")]
        public double defaultClipDuration = 5.0;

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<SceneImageMixer>.Create(graph, inputCount);
        }

        protected override void OnCreateClip(TimelineClip clip)
        {
            base.OnCreateClip(clip);
            clip.duration = defaultClipDuration;
            clip.displayName = "Scene Image";
        }
    }
}
