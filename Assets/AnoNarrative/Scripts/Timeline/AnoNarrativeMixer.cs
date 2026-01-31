using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.AnoNarrative.Timeline
{
    // Simple mixer that currently passes through logic.
    // Can be used for cross-fading or conflict resolution if multiple clips overlap.
    public class AnoNarrativeMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);
            // Could handle blending here if necessary, but conversations don't really blend.
        }
    }
}
