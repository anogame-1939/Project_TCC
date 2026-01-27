using UnityEngine;

namespace AnoGame.Application.Message
{
    public struct SpotlightDeactivationMessage
    {
        public string LightId;

        public SpotlightDeactivationMessage(string lightId)
        {
            LightId = lightId;
        }
    }
}
