using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    public class DialogueConversationClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("Override (optional)")]
        public ExposedReference<DialogueSystemTrigger> triggerOverride; // ← 任意
        public ExposedReference<Transform> actor;        // 任意（使わないなら無視される）
        public ExposedReference<Transform> conversant;   // 任意
        [Tooltip("会話中はタイムラインをその場で停止")]
        public bool pauseDuringConversation = true;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<DialogueConversationBehaviour>.Create(graph);
            var b = playable.GetBehaviour();

            var resolver = graph.GetResolver();                 // 自身の PlayableDirector
            b.director            = resolver as PlayableDirector;
            b.triggerOverride     = triggerOverride.Resolve(resolver);
            b.actor               = actor.Resolve(resolver);
            b.conversant          = conversant.Resolve(resolver);
            b.pauseDuringConversation = pauseDuringConversation;
            return playable;
        }
    }
}
