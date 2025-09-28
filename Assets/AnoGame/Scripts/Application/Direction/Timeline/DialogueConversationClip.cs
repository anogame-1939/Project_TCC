using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    public class DialogueConversationClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("Start Conversation by... (either)")]
        public ExposedReference<DialogueSystemTrigger> trigger;
        public string conversationTitle;

        [Header("Optional")]
        public ExposedReference<Transform> actor;
        public ExposedReference<Transform> conversant;

        [Tooltip("会話中はタイムラインをその場で停止")]
        public bool pauseDuringConversation = true;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<DialogueConversationBehaviour>.Create(graph);
            var b = playable.GetBehaviour();

            var resolver = graph.GetResolver(); // = 再生中の PlayableDirector
            b.director    = resolver as PlayableDirector;
            b.trigger     = trigger.Resolve(resolver);
            b.conversationTitle = conversationTitle;
            b.actor       = actor.Resolve(resolver);
            b.conversant  = conversant.Resolve(resolver);
            b.pauseDuringConversation = pauseDuringConversation;
            return playable;
        }
    }


}