using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    public enum ParticleAction
    {
        None,
        PlayAtThreshold,
        ForcePlay,
        ForceStop
    }

    [Serializable]
    public sealed class EnemyResolveBehaviour : PlayableBehaviour
    {
        [Header("Resolve Control")]
        public bool resolveControl = true;
        [Range(0f, 1f)] public float startAmount = 0f;
        [Range(0f, 1f)] public float endAmount = 1f;
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        [ColorUsage(true, true)] public Color outlineColor = Color.red;

        [Header("Particle Control")]
        public bool particleControl = false;
        public ParticleAction particleAction = ParticleAction.PlayAtThreshold;
        public bool stopAtClipEnd = true;
        [ColorUsage(true, true)] public Color particleColor = Color.white;
        [Range(0f, 1f)] public float playThreshold = 0.2f;
        [Range(0f, 1f)] public float stopThreshold = 0.8f;
    }
}
