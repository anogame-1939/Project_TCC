using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    [Serializable]
    public sealed class EnemyResolveBehaviour : PlayableBehaviour
    {
        [Range(0f, 1f)]
        public float resolveAmount;
    }
}
