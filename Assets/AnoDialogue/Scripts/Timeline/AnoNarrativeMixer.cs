using UnityEngine;
using UnityEngine.Playables;
using AnoGame.AnoDialogue;

namespace AnoGame.AnoDialogue.Timeline
{
    // Simple mixer that currently passes through logic.
    // Can be used for cross-fading or conflict resolution if multiple clips overlap.
    public class AnoNarrativeMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);

            // Debug binding
            // Debug.Log($"[AnoNarrativeMixer] ProcessFrame. playerData: {(playerData == null ? "null" : playerData.GetType().Name)}");

            // Get the binding from the track
            var receiver = playerData as DialogueTimelineReceiver;

            if (receiver == null)
            {
                // If no binding, we can't play anything safely.
                return;
            }

            // Iterate over all clips on this track
            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight > 0f)
                {
                    var inputPlayable = playable.GetInput(i);
                    var behaviour = ((ScriptPlayable<AnoNarrativeBehaviour>)inputPlayable).GetBehaviour();

                    // Pass the receiver to the behaviour
                    behaviour.receiver = receiver;
                }
            }
        }
    }
}
