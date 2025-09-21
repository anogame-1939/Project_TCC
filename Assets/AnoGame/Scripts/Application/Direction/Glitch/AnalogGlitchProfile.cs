// Assets/AnoGame/Application/Glitch/AnalogGlitchProfile.cs
using UnityEngine;

namespace AnoGame.Application.Direction.Glitch
{
    [CreateAssetMenu(menuName = "AnoGame/Glitch/Analog Profile")]
    public sealed class AnalogGlitchProfile : ScriptableObject
    {
        public bool enabled = true;
        [Range(0, 1)] public float scanLineJitter = 0f;
        [Range(0, 1)] public float verticalJump = 0f;
        [Range(0, 1)] public float horizontalShake = 0f;
        [Range(0, 1)] public float colorDrift = 0f;
    }
}