using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline    
{
    [TrackClipType(typeof(FadePlayableAsset))]
    [TrackBindingType(typeof(FadeHandler))]
    public class FadeTrack : TrackAsset
    {
        // ここは最小実装でOK（必要ならカスタムのミキサーを追加）
        protected override void OnCreateClip(TimelineClip clip)
        {
            base.OnCreateClip(clip);
            clip.displayName = "Fade";
        }
    }
}
