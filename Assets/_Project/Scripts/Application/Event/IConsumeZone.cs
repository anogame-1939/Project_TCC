using UnityEngine;

namespace AnoGame.AnoFlow
{
    public interface IConsumeZone
    {
        bool CanConsume(string itemId, GameObject user, Vector3 usePos, out string reason);
        bool TryStart(string itemId, GameObject user, Vector3 usePos); // 実行まで行いたい時用（任意）
        string GetDebugName();
    }
}