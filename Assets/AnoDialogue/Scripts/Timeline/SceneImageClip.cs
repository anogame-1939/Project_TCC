using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.AnoDialogue.Timeline
{
    /// <summary>
    /// シーン画像を表示するための Timeline クリップ。
    /// クリップの期間中、指定されたシーン画像が表示され、
    /// 終了時に自動で非表示になる。
    /// </summary>
    public class SceneImageClip : PlayableAsset, ITimelineClipAsset
    {
        public Sprite sceneImage;
        public string dialogueStyleName;
        public AnoGame.AnoDialogue.Data.DialogueStyle styleDatabase;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<SceneImageBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.sceneImage = sceneImage;
            behaviour.dialogueStyleName = dialogueStyleName;

            Debug.Log($"[SceneImageClip] CreatePlayable: sprite={(sceneImage != null ? sceneImage.name : "NULL")}, style={dialogueStyleName}");

            return playable;
        }
    }
}
