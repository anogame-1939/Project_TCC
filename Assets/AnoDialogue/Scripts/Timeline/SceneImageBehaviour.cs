using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.AnoDialogue.Timeline
{
    /// <summary>
    /// SceneImageClip の再生ロジック。
    /// クリップ開始時にシーン画像を表示し、終了時に非表示にする。
    /// </summary>
    public class SceneImageBehaviour : PlayableBehaviour
    {
        public Sprite sceneImage;
        public string dialogueStyleName;
        public DialogueTimelineReceiver receiver;

        private bool _isShowing;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_isShowing || !Application.isPlaying) return;

            if (receiver == null) return;

            if (sceneImage == null) return;

            _isShowing = true;
            receiver.ShowSceneImage(dialogueStyleName, sceneImage);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // Mixer 経由で receiver が後から設定される場合のフォールバック
            if (!_isShowing && receiver != null && Application.isPlaying && info.weight > 0)
            {
                if (sceneImage != null)
                {
                    _isShowing = true;
                    receiver.ShowSceneImage(dialogueStyleName, sceneImage);
                }
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!_isShowing) return;

            // Timeline が一時停止しただけの場合はリセットしない
            // クリップが実際に終了した場合のみ非表示にする
            if (receiver != null && Application.isPlaying)
            {
                // effectiveWeight が 0 の場合、クリップが範囲外（終了）
                var progress = (float)(playable.GetTime() / playable.GetDuration());
                if (progress >= 0.99f || info.effectiveWeight <= 0f)
                {
                    _isShowing = false;
                    receiver.HideSceneImage(dialogueStyleName);
                }
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_isShowing && receiver != null && Application.isPlaying)
            {
                _isShowing = false;
                receiver.HideSceneImage(dialogueStyleName);
            }
        }
    }
}
