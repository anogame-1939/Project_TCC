using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Timeline.CameraShake
{
    [System.Serializable]
    public class CameraShakeBehaviour : PlayableBehaviour
    {
        [Range(0f, 20f)] public float intensity = 1f;
        [Range(0f, 20f)] public float frequency = 1f;
    }
}
