// Assets/AnoGame/Application/Glitch/AnalogGlitchProfile.cs
using UnityEngine;

namespace AnoGame.Application.Direction.Glitch
{
    [CreateAssetMenu(menuName = "AnoGame/Glitch/Digital Profile")]
    public sealed class DigitalGlitchProfile : ScriptableObject
    {
        public bool enabled = true;
        [Range(0, 1)] public float intensity = 0.2f;
    }
}