using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using PixelCrushers.DialogueSystem;
using App = UnityEngine.Application;

namespace AnoGame.Application.Direction.Timeline
{
    public class DialogueConversationBehaviour : PlayableBehaviour
    {
        [HideInInspector] public PlayableDirector director;
        [HideInInspector] public DialogueSystemTrigger triggerOverride; // クリップ個別の上書き
        [HideInInspector] public Transform actor, conversant;
        [HideInInspector] public bool pauseDuringConversation = true;

        DialogueSystemTrigger _trackBoundTrigger; // ← トラックにバインドされた Trigger
        bool _fired;
        double _pausedTime;
        double _prevSpeed = 1;

        // Track のバインドは playerData で渡ってくる
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playerData is DialogueSystemTrigger t) _trackBoundTrigger = t;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_fired || !App.isPlaying) return;
            _fired = true;

            if (pauseDuringConversation) PauseTimeline();

            // 1) クリップの上書きがあればそれを使う
            // 2) 無ければトラックのバインド Trigger を使う
            var trigger = triggerOverride != null ? triggerOverride : _trackBoundTrigger;

            if (trigger != null)
            {
                trigger.OnUse(); // ← Trigger 側の設定どおりに起動（Title 指定不要）
            }
            else
            {
                Debug.LogWarning("[DialogueConversation] Trigger が未設定です。トラックに DialogueSystemTrigger を割り当てるか、クリップの Override を指定してください。");
            }

            // 終了待ち → レジューム
            ResumeWhenConversationEnds().Forget();
        }

        async UniTaskVoid ResumeWhenConversationEnds()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await UniTask.WaitUntil(() => !DialogueManager.IsConversationActive);
            if (pauseDuringConversation) ResumeTimeline();
        }

        void PauseTimeline()
        {
            if (director == null || !director.playableGraph.IsValid()) return;
            var root = director.playableGraph.GetRootPlayable(0);
            _prevSpeed = root.GetSpeed();
            _pausedTime = director.time;
            root.SetSpeed(0);
        }

        void ResumeTimeline()
        {
            if (director == null || !director.playableGraph.IsValid()) return;
            director.time = _pausedTime;
            var root = director.playableGraph.GetRootPlayable(0);
            root.SetSpeed(_prevSpeed <= 0 ? 1 : _prevSpeed);
        }

public override void OnBehaviourPause(Playable playable, FrameData info)
{
    // 再生中にこのクリップの評価が終わったタイミングで会話を強制停止
    if (App.isPlaying && DialogueManager.IsConversationActive)
    {
        DialogueManager.StopConversation();

        // 会話中にタイムラインを止めていた場合は強制的に再開しておく
        if (pauseDuringConversation)
        {
            ResumeTimeline();
        }
    }

    // ループ/シーク再入に対応
    _fired = false;
}

    }
}
