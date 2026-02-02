using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using AnoGame.AnoNarrative;

namespace AnoGame.AnoNarrative.Timeline
{
    public class AnoNarrativeBehaviour : PlayableBehaviour
    {
        public string conversationID;
        // public DialogueController targetController; // Removed
        public bool pauseTimeline;
        public PlayableDirector director;
        public DialogueTimelineReceiver receiver;

        private bool _isTriggered;
        private double _pausedTime;
        private double _prevSpeed = 1;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // Fallback for late-binding or lost binding recovery
            if (!_isTriggered && receiver != null && Application.isPlaying)
            {
                // Ensure we are actually playing (weight check might be needed if mixer blends, but usually OnBehaviourPlay handles entry)
                // But if OnBehaviourPlay failed due to null receiver, we retry here.
                if (info.weight > 0)
                {
                    Debug.Log($"[AnoNarrativeBehaviour] ProcessFrame: Receiver found ({receiver.name}), triggering delayed start.");
                    StartConversation();
                }
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            Debug.Log($"[AnoNarrativeBehaviour] OnBehaviourPlay Called. App.isPlaying: {Application.isPlaying}, _isTriggered: {_isTriggered}, ConversationID: {conversationID}, Receiver: {(receiver != null ? receiver.name : "null")}");

            if (_isTriggered || !Application.isPlaying)
            {
                // Debug.Log($"[AnoNarrativeBehaviour] OnBehaviourPlay Skipped. _isTriggered: {_isTriggered}, App.isPlaying: {Application.isPlaying}");
                return;
            }

            if (string.IsNullOrEmpty(conversationID))
            {
                Debug.LogWarning("[AnoNarrativeBehaviour] No Conversation ID specified.");
                return;
            }

            if (receiver == null)
            {
                Debug.LogWarning($"[AnoNarrativeBehaviour] Receiver is null for conversation {conversationID}. Waiting for Mixer injection in ProcessFrame...");
                // Do NOT return failure, just don't trigger yet. ProcessFrame will pick it up properly once Mixer runs.
                return;
            }

            StartConversation();
        }

        private void StartConversation()
        {
            if (_isTriggered) return;

            _isTriggered = true;

            if (pauseTimeline)
            {
                Debug.Log("[AnoNarrativeBehaviour] Requesting PauseTimeline.");
                PauseTimeline();
            }

            Debug.Log($"[AnoNarrativeBehaviour] Calling receiver.Play({conversationID}).");
            receiver.Play(conversationID);

            // Wait for conversation to end
            Debug.Log("[AnoNarrativeBehaviour] Starting MonitorConversationEnd Coroutine.");
            MonitorConversationEnd().Forget();
        }

        private async UniTaskVoid MonitorConversationEnd()
        {
            Debug.Log("[AnoNarrativeBehaviour] MonitorConversationEnd: Waiting for LastPostLateUpdate...");
            // Wait a frame to ensure active state is updated (if manager starts it)
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            Debug.Log("[AnoNarrativeBehaviour] MonitorConversationEnd: Waiting for Conversation to end...");
            // Wait until conversation is no longer active
            // Using receiver to check allows receiver to handle "Simulation" (always false) or real check
            await UniTask.WaitUntil(() =>
            {
                if (receiver == null)
                {
                    Debug.LogWarning("[AnoNarrativeBehaviour] MonitorConversationEnd: Receiver lost during wait. Aborting.");
                    return true; // Abort wait if receiver lost
                }
                bool active = receiver.IsConversationActive;
                return !active;
            });

            Debug.Log($"[AnoNarrativeBehaviour] MonitorConversationEnd: Conversation ended. pauseTimeline: {pauseTimeline}");

            if (pauseTimeline)
            {
                ResumeTimeline();
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            Debug.Log($"[AnoNarrativeBehaviour] OnBehaviourPause Called. Director State: {(director != null ? director.state.ToString() : "null")}");
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

            // If the timeline is just paused, we should NOT reset the trigger state.
            // Resetting it would cause OnBehaviourPlay to re-trigger the conversation when resumed.
            if (director != null && director.state == PlayState.Paused)
            {
                Debug.Log("[AnoNarrativeBehaviour] OnBehaviourPause: Director Paused, skipping reset of _isTriggered.");
                return;
            }

            Debug.Log("[AnoNarrativeBehaviour] OnBehaviourPause: Resetting _isTriggered to false.");
            _isTriggered = false;
        }

        private void PauseTimeline()
        {
            if (director == null || !director.playableGraph.IsValid())
            {
                Debug.LogWarning("[AnoNarrativeBehaviour] PauseTimeline Failed: director is null or graph invalid.");
                return;
            }

            _prevSpeed = director.playableGraph.GetRootPlayable(0).GetSpeed();
            _pausedTime = director.time;
            Debug.Log($"[AnoNarrativeBehaviour] PauseTimeline. PausedTime: {_pausedTime}, PrevSpeed: {_prevSpeed}");
            director.playableGraph.GetRootPlayable(0).SetSpeed(0);
        }

        private void ResumeTimeline()
        {
            Debug.Log("[AnoNarrativeBehaviour] ResumeTimeline Called.");
            if (director == null || !director.playableGraph.IsValid())
            {
                Debug.LogWarning("[AnoNarrativeBehaviour] ResumeTimeline Failed: director is null or graph invalid.");
                return;
            }

            Debug.Log($"[AnoNarrativeBehaviour] Resuming to Time: {_pausedTime}, Speed: {(_prevSpeed <= 0 ? 1 : _prevSpeed)}");
            director.time = _pausedTime;
            director.playableGraph.GetRootPlayable(0).SetSpeed(_prevSpeed <= 0 ? 1 : _prevSpeed);
        }
    }
}
