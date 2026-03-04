using UnityEngine;
using UnityEngine.Playables;
using Unity.TinyCharacterController.Interfaces.Components; // IWarp

namespace AnoGame.Application.Direction.Timeline
{
    public sealed class WarpProxyMixer : PlayableBehaviour
    {
        // バインド先が切り替わった時だけ IWarp を取り直す
        private TimelineEventLockProxy _lastProxy;
        private IWarp _cachedWarp;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var proxy = playerData as TimelineEventLockProxy;
            if (proxy == null) return;

            // Proxyが変わったら IWarp を再取得
            if (!ReferenceEquals(proxy, _lastProxy))
            {
                _lastProxy = proxy;
                _cachedWarp = null;

                var playerCtrl = proxy._playerCtrl; // ← 既存フィールドをそのまま利用
                if (playerCtrl != null)
                {
                    // 同じGO or 親から IWarp を取得（Brain）
                    _cachedWarp = playerCtrl.GetComponent<IWarp>() ?? playerCtrl.GetComponentInParent<IWarp>();
                }
            }

            if (_cachedWarp == null) return;

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                var weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var sp  = (ScriptPlayable<WarpPlayableBehaviour>)playable.GetInput(i);
                var bhv = sp.GetBehaviour();
                if (bhv.fired) continue;

                // 目標座標
                Vector3 pos = (bhv.useTarget && bhv.target != null)
                    ? bhv.target.position
                    : bhv.worldPosition;

                // 向き
                switch (bhv.facing)
                {
                    case WarpPlayableAsset.WarpFacing.Keep:
                        _cachedWarp.Warp(pos); // 位置のみ（向き維持）
                        break;

                    case WarpPlayableAsset.WarpFacing.FaceDirection:
                        _cachedWarp.Warp(pos, bhv.worldFacingDirection); // 位置＋方向ベクトル
                        break;

                    case WarpPlayableAsset.WarpFacing.FaceTarget:
                        Vector3 dir = Vector3.zero;
                        if (bhv.lookAt != null)
                        {
                            dir = bhv.lookAt.position - pos;
                            dir.y = 0f;
                        }
                        _cachedWarp.Warp(pos, dir);
                        break;
                }

                bhv.fired = true;
            }
        }
    }
}
