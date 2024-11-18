using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    [CreateAssetMenu(fileName = "EffectSettings", menuName = "AnoGame/EffectSettings")]
    public class EffectSettings : ScriptableObject
    {
        [Header("Effect Durations")]
        public float visionReductionDuration = 3f;
        public float stunDuration = 2f;
        public float slowDuration = 5f;
    }
}
