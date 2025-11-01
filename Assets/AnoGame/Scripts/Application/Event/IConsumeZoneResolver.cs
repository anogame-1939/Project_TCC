using UnityEngine;

namespace AnoGame.Application.Event
{
    public interface IConsumeZoneResolver
    {
        bool TryResolve(string itemId, GameObject user, Vector3 usePos,
                        out IConsumeZone zone, out string reason);
    }
}