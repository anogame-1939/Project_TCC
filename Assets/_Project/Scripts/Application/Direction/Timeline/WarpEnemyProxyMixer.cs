using UnityEngine;
using UnityEngine.Playables;
using AnoGame.Application.Enemy.Control;

namespace AnoGame.Application.Direction.Timeline
{
    public sealed class WarpEnemyProxyMixer : PlayableBehaviour
    {
        // バインド先が切り替わった時だけ Handler を取り直す
        private TimelineEnemyEventLockProxy _lastProxy;
        private EnemyLocomotionHandler _cachedHandler;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // Debug.Log("playerdata " + playerData);
            var proxy = playerData as TimelineEnemyEventLockProxy;
            if (proxy == null)
            {
                // Debug.LogWarning("[WarpEnemyProxyMixer] Proxy is null");
                return;
            }

            // Proxyが変わったら Handler を再取得
            if (!ReferenceEquals(proxy, _lastProxy))
            {
                Debug.Log($"[WarpEnemyProxyMixer] Proxy changed. New proxy: {proxy.name}");
                _lastProxy = proxy;
                _cachedHandler = null;
            }

            // キャッシュがない場合は取得を試みる (Enemyが後からSpawnする場合などに対応)
            if (_cachedHandler == null)
            {
                var enemyCtrl = proxy.EnemyCtrl; // ← 公開したプロパティを利用
                if (enemyCtrl != null)
                {
                    // 同じGO or 親から Handler を取得
                    _cachedHandler = enemyCtrl.GetComponent<EnemyLocomotionHandler>() ?? enemyCtrl.GetComponentInParent<EnemyLocomotionHandler>();
                    if (_cachedHandler != null)
                    {
                        Debug.Log($"[WarpEnemyProxyMixer] Cached Handler: {_cachedHandler != null}");
                    }
                }
            }

            if (_cachedHandler == null)
            {
                // Debug.LogWarning("[WarpEnemyProxyMixer] Cached Handler is null");
                return;
            }

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                var weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var sp = (ScriptPlayable<WarpPlayableBehaviour>)playable.GetInput(i);
                var bhv = sp.GetBehaviour();
                if (bhv.fired) continue;

                Debug.Log($"[WarpEnemyProxyMixer] Firing warp. Weight: {weight}");

                // 目標座標
                Vector3 pos = (bhv.useTarget && bhv.target != null)
                    ? bhv.target.position
                    : bhv.worldPosition;

                // 向き
                switch (bhv.facing)
                {
                    case WarpPlayableAsset.WarpFacing.Keep:
                        Debug.Log($"[WarpEnemyProxyMixer] Warping to {pos} (Keep Facing)");
                        _cachedHandler.Warp(pos); // 位置のみ（向き維持）
                        break;

                    case WarpPlayableAsset.WarpFacing.FaceDirection:
                        Debug.Log($"[WarpEnemyProxyMixer] Warping to {pos} (Face Direction: {bhv.worldFacingDirection})");
                        _cachedHandler.Warp(pos, bhv.worldFacingDirection); // 位置＋方向ベクトル
                        break;

                    case WarpPlayableAsset.WarpFacing.FaceTarget:
                        Vector3 dir = Vector3.zero;
                        if (bhv.lookAt != null)
                        {
                            dir = bhv.lookAt.position - pos;
                            dir.y = 0f;
                        }
                        Debug.Log($"[WarpEnemyProxyMixer] Warping to {pos} (Face Target: {bhv.lookAt?.name}, Dir: {dir})");
                        _cachedHandler.Warp(pos, dir);
                        break;
                }

                bhv.fired = true;
            }
        }
    }
}
