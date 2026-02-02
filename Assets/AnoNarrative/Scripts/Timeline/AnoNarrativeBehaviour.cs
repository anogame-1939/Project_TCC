using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using AnoGame.AnoNarrative;

namespace AnoGame.AnoNarrative.Timeline
{
    public class AnoNarrativeBehaviour : PlayableBehaviour
    {
        public string conversationID;
        public DialogueController targetController;
        public bool pauseTimeline;
        public PlayableDirector director;
        public DialogueTimelineReceiver receiver;

        private bool _isTriggered;
        private double _pausedTime;
        private double _prevSpeed = 1;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_isTriggered || !Application.isPlaying) return;

            // Priority: Target Controller > ConversationID
            if (targetController != null)
            {
                _isTriggered = true;
                if (pauseTimeline) PauseTimeline();

                targetController.Play();

                // Monitor end via receiver (requires receiver to check global manager)
                MonitorConversationEnd().Forget();
                return;
            }

            if (string.IsNullOrEmpty(conversationID))
            {
                Debug.LogWarning("[AnoNarrativeTimeline] No Conversation ID or Controller specified.");
                return;
            }

            if (receiver == null)
            {
                Debug.LogWarning($"[AnoNarrativeTimeline] Receiver is null for conversation {conversationID}. Check Track Binding.");
                return;
            }

            _isTriggered = true;

            if (pauseTimeline)
            {
                PauseTimeline();
            }

            receiver.Play(conversationID);

            // Wait for conversation to end
            MonitorConversationEnd().Forget();
        }

        private async UniTaskVoid MonitorConversationEnd()
        {
            // Wait a frame to ensure active state is updated (if manager starts it)
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            // Wait until conversation is no longer active
            // Using receiver to check allows receiver to handle "Simulation" (always false) or real check
            await UniTask.WaitUntil(() =>
            {
                if (receiver == null) return true; // Abort wait if receiver lost
                return !receiver.IsConversationActive;
            });

            if (pauseTimeline)
            {
                ResumeTimeline();
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // If we paused the graph, OnBehaviourPause is called.
            // But also when the clip finishes.

            // If the clip finishes normally or is interrupted, we might need to cleanup.
            // But if we are simply PAUSED by our own logic, we don't want to stop conversation.

            // If the timeline is stopped externally while we are active, we should stop dialogue?
            if (Application.isPlaying && director != null && director.state != PlayState.Playing)
            {
                // Timeline stopped or paused manually (not by us?) 
                // Actually this logic is tricky. 
                // Let's stick to simple logic: If the clip scope ends, we don't necessarily stop dialogue unless strictly desired.
                // But if we want to secure "end", maybe we just ensure resuming happened.
            }

            _isTriggered = false;
        }

        private void PauseTimeline()
        {
            if (director == null || !director.playableGraph.IsValid()) return;

            _prevSpeed = director.playableGraph.GetRootPlayable(0).GetSpeed();
            _pausedTime = director.time;
            director.playableGraph.GetRootPlayable(0).SetSpeed(0);
        }

        private void ResumeTimeline()
        {
            if (director == null || !director.playableGraph.IsValid()) return;

            director.time = _pausedTime;
            director.playableGraph.GetRootPlayable(0).SetSpeed(_prevSpeed <= 0 ? 1 : _prevSpeed);
        }
    }
}
