using UnityEngine;

namespace AnoGame.Application.Message
{
    public struct SpotlightActivationMessage
    {
        public Vector3 Position;
        public float Radius;
        [System.Obsolete("Duration is deprecated. Spotlights are now persistent until deactivated.")]
        public float Duration;
        public string LightId;

        public SpotlightActivationMessage(Vector3 position, float radius, float duration, string lightId)
        {
            Position = position;
            Radius = radius;
            Duration = duration;
            LightId = lightId;
        }
    }
}
