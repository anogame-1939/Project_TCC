using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using PixelCrushers.DialogueSystem;

namespace AnoGame.Application.Direction.Timeline
{
    public class DialogueConversationBehaviour : PlayableBehaviour
    {
        [HideInInspector] public PlayableDirector director;
        [HideInInspector] public DialogueSystemTrigger trigger;
        [HideInInspector] public string conversationTitle;
        [HideInInspector] public Transform actor, conversant;
        [HideInInspector] public bool pauseDuringConversation = true;

        bool _fired;
        double _pausedTime;
        double _prevSpeed = 1;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_fired || !UnityEngine.Application.isPlaying) return;
            _fired = true;

            if (pauseDuringConversation) PauseTimeline();

            // 会話開始（Trigger優先、無ければタイトル直指定）
            if (trigger != null)
            {
                trigger.OnUse();                       // 「On Use」で起動する設定にしておく
            }
            else if (!string.IsNullOrEmpty(conversationTitle))
            {
                DialogueManager.StartConversation(conversationTitle, actor, conversant);
            }

            // 終了待ち → レジューム
            ResumeWhenConversationEnds().Forget();
        }

        async UniTaskVoid ResumeWhenConversationEnds()
        {
            // 会話開始直後のフレームを跨いでから監視
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await UniTask.WaitUntil(() => !DialogueManager.IsConversationActive);
            if (pauseDuringConversation) ResumeTimeline();
        }

        void PauseTimeline()
        {
            if (director == null || !director.playableGraph.IsValid()) return;
            var root = director.playableGraph.GetRootPlayable(0);
            _prevSpeed = root.GetSpeed();
            _pausedTime = director.time;   // 念のため保存
            root.SetSpeed(0);              // その場で静止（Pause/Play より位置ズレが少ない）
        }

        void ResumeTimeline()
        {
            if (director == null || !director.playableGraph.IsValid()) return;
            director.time = _pausedTime;   // 万一ずれても元位置から
            var root = director.playableGraph.GetRootPlayable(0);
            root.SetSpeed(_prevSpeed <= 0 ? 1 : _prevSpeed);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // ループやシークで再入できるように
            _fired = false;
        }
    }
}