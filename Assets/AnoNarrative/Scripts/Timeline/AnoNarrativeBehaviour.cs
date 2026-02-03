using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;

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
                    // Debug.Log($"[AnoNarrativeBehaviour] ProcessFrame: Receiver found ({receiver.name}), triggering delayed start.");
                    StartConversation();
                }
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            // Debug.Log($"[AnoNarrativeBehaviour] OnBehaviourPlay Called. App.isPlaying: {Application.isPlaying}, _isTriggered: {_isTriggered}, ConversationID: {conversationID}, Receiver: {(receiver != null ? receiver.name : "null")}");

            if (_isTriggered || !Application.isPlaying)
            {
                return;
            }

            if (string.IsNullOrEmpty(conversationID))
            {
                Debug.LogWarning("[AnoNarrativeBehaviour] No Conversation ID specified.");
                return;
            }

            if (receiver == null)
            {
                // Debug.LogWarning($"[AnoNarrativeBehaviour] Receiver is null for conversation {conversationID}. Waiting for Mixer injection in ProcessFrame...");
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
                // Debug.Log("[AnoNarrativeBehaviour] Requesting PauseTimeline.");
                PauseTimeline();
            }

            // Debug.Log($"[AnoNarrativeBehaviour] Calling receiver.Play({conversationID}).");
            receiver.Play(conversationID);

            // Wait for conversation to end
            // Debug.Log("[AnoNarrativeBehaviour] Starting MonitorConversationEnd Coroutine.");
            MonitorConversationEnd().Forget();
        }

        private async UniTaskVoid MonitorConversationEnd()
        {
            // Debug.Log("[AnoNarrativeBehaviour] MonitorConversationEnd: Waiting for LastPostLateUpdate...");
            // Wait a frame to ensure active state is updated (if manager starts it)
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            // Debug.Log("[AnoNarrativeBehaviour] MonitorConversationEnd: Waiting for Conversation to end...");

            // Wait until conversation is no longer active
            await UniTask.WaitUntil(() =>
            {
                if (receiver == null)
                {
                    Debug.LogWarning("[AnoNarrativeBehaviour] MonitorConversationEnd: Receiver lost during wait. Aborting.");
                    return true;
                }

                // Seek/Scrub Detection
                // If the timeline time has changed significantly from where we paused it, the user (or logic) has moved the head.
                // In this case, we should cancel the conversation and respect the new time.
                if (pauseTimeline && director != null && director.playableGraph.IsValid())
                {
                    double currentTime = director.time;
                    if (System.Math.Abs(currentTime - _pausedTime) > 0.01f) // Tolerance for float drift
                    {
                        Debug.Log($"[AnoNarrativeBehaviour] Timeline Playhead moved significantly (Time: {currentTime:F2} vs Paused: {_pausedTime:F2}). Cancelling Conversation.");

                        // Stop Dialogue
                        DialogueManager.Instance.StopConversation();

                        // Ensure we don't snap back to old time
                        _pausedTime = currentTime;
                        return true;
                    }
                }

                bool active = receiver.IsConversationActive;
                return !active;
            });

            // Debug.Log($"[AnoNarrativeBehaviour] MonitorConversationEnd: Conversation ended. pauseTimeline: {pauseTimeline}");

            if (pauseTimeline)
            {
                ResumeTimeline();
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // Debug.Log($"[AnoNarrativeBehaviour] OnBehaviourPause Called. Director State: {(director != null ? director.state.ToString() : "null")}");

            // If the timeline is just paused, we should NOT reset the trigger state.
            // Resetting it would cause OnBehaviourPlay to re-trigger the conversation when resumed.
            if (director != null && director.state == PlayState.Paused)
            {
                // HOWEVER, if the user Scrolled/Seeked, the state is also Paused.
                // We need to know if time changed.
                // But MonitorConversationEnd handles the "While Playing" seek.
                // If we are strictly paused, checking changes is hard without tracking per frame.
                // Assuming MonitorConversationEnd covers the active cancellation case.

                // Debug.Log("[AnoNarrativeBehaviour] OnBehaviourPause: Director Paused, skipping reset of _isTriggered.");
                return;
            }

            // Debug.Log("[AnoNarrativeBehaviour] OnBehaviourPause: Resetting _isTriggered to false.");
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
            // Debug.Log($"[AnoNarrativeBehaviour] PauseTimeline. PausedTime: {_pausedTime}, PrevSpeed: {_prevSpeed}");
            director.playableGraph.GetRootPlayable(0).SetSpeed(0);
        }

        private void ResumeTimeline()
        {
            // Debug.Log("[AnoNarrativeBehaviour] ResumeTimeline Called.");
            if (director == null || !director.playableGraph.IsValid())
            {
                Debug.LogWarning("[AnoNarrativeBehaviour] ResumeTimeline Failed: director is null or graph invalid.");
                return;
            }

            // Only restore time if it hasn't drifted via our Seek Logic
            // The Seek logic updates _pausedTime to current time if detected, so we just set it.
            // Actually, if we scrubbed, we are at new time. setting director.time = newTime is redundant but safe.
            // Debug.Log($"[AnoNarrativeBehaviour] Resuming to Time: {_pausedTime}, Speed: {(_prevSpeed <= 0 ? 1 : _prevSpeed)}");
            director.time = _pausedTime;
            director.playableGraph.GetRootPlayable(0).SetSpeed(_prevSpeed <= 0 ? 1 : _prevSpeed);
        }
    }
}
