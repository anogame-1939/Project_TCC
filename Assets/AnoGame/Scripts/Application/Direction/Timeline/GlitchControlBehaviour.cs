using AnoGame.Application.Direction.Glitch;
using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    [System.Serializable]
    public sealed class GlitchControlBehaviour : PlayableBehaviour
    {
        [Header("Toggle")]
        public bool   touchAnalog   = false;
        public bool   analogEnabled = true;
        public bool   touchDigital  = false;
        public bool   digitalEnabled= true;

        [Header("Profiles (optional)")]
        public AnalogGlitchProfile  analogProfile;
        public DigitalGlitchProfile digitalProfile;

public override void OnBehaviourPlay(Playable playable, FrameData info)
{
    Debug.Log("[GLITCH_CLIP] OnBehaviourPlay");
}
public void Apply(IGlitchController ctrl)
{
    Debug.Log($"[GLITCH_CLIP] Apply touchA={touchAnalog} touchD={touchDigital} " +
              $"A={analogEnabled} D={digitalEnabled} " +
              $"AP={analogProfile} " +
              $"DP={digitalProfile}");
    if (touchAnalog)  ctrl.SetAnalogEnabled(analogEnabled);
    if (touchDigital) ctrl.SetDigitalEnabled(digitalEnabled);
    if (analogProfile)  ctrl.ApplyAnalogProfile(analogProfile);
    if (digitalProfile) ctrl.ApplyDigitalProfile(digitalProfile);
        }
    }
}
