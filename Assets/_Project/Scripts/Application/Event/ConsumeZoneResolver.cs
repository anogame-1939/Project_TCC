using System.Linq;
using UnityEngine;

namespace AnoGame.AnoFlow
{
    public sealed class ConsumeZoneResolver : IConsumeZoneResolver
    {
        // 必要ならコンストラクタDI可。ここでは user から動的取得。
        public bool TryResolve(string itemId, GameObject user, Vector3 usePos,
                               out IConsumeZone zone, out string reason)
        {
            zone = null;
            reason = null;

            // ① user の子階層からトラッカーを探す（推奨）
            var tracker = user ? user.GetComponentInChildren<ConsumeProximityTracker>() : null;

            // ② 見つかったら既存メソッドで解決（最小改修）
            if (tracker && tracker.TryPickUsableZone(itemId, user, usePos, out zone, out reason))
                return true;

            // ③ フォールバック（任意）：物理検索で近傍ゾーンを直接当てる
            //    - サーバやトラッカー未設置のシーンでも動作可能に
            const float fallbackRadius = 3f; // or 設定/Item別
            var cols = Physics.OverlapSphere(usePos, fallbackRadius);
            var candidates = cols
                .Select(c => c.GetComponentInParent<IConsumeZone>())
                .Where(z => z != null)
                .Distinct();

            string last = null;
            IConsumeZone best = null;
            float bestDist = float.PositiveInfinity;
            foreach (var z in candidates)
            {
                if (z.CanConsume(itemId, user, usePos, out var r))
                {
                    // 近いものを優先
                    var p = (z as Component)?.transform.position ?? usePos;
                    var d = (p - usePos).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; best = z; }
                }
                else last = r;
            }

            if (best != null) { zone = best; return true; }
            reason = last ?? "付近に使用可能なイベントがありません。";
            return false;
        }
    }
}