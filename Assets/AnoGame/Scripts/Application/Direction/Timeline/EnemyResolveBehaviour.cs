using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    [Serializable]
    public sealed class EnemyResolveBehaviour : PlayableBehaviour
    {
        [Range(0f, 1f)]
        public float startAmount = 0f;

        [Range(0f, 1f)]
        public float endAmount = 1f;

        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Particle Control")]
        public bool playEffect = false;
        [Range(0f, 1f)] public float playThreshold = 0.2f;
        [Range(0f, 1f)] public float stopThreshold = 0.8f;
    }
}
