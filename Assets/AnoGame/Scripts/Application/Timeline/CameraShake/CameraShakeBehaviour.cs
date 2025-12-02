using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Timeline.CameraShake
{
    [System.Serializable]
    public class CameraShakeBehaviour : PlayableBehaviour
    {
        [Range(0f, 500f)] public float intensity = 1f;
        [Range(0f, 500f)] public float frequency = 1f;
        public Vector3 offset;
    }
}
