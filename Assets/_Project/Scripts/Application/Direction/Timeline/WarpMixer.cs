using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    public sealed class WarpMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var ctrl = playerData as AnoGame.Application.Player.Control.EventLockControl;
            if (ctrl == null) return;

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                var weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var sp = (ScriptPlayable<WarpPlayableBehaviour>)playable.GetInput(i);
                var bhv = sp.GetBehaviour();

                if (bhv.fired) continue;

                // 必要ならロック開始
                if (bhv.beginLockBeforeWarp) ctrl.BeginLock();

                // 位置の決定
                Vector3 pos = bhv.useTarget && bhv.target != null
                    ? bhv.target.position
                    : bhv.worldPosition;

                // 向きの決定
                switch (bhv.facing)
                {
                    case WarpPlayableAsset.WarpFacing.Keep:
                        ctrl.TryWarp(pos);       // 現在の向きを維持
                        break;

                    case WarpPlayableAsset.WarpFacing.FaceDirection:
                        ctrl.TryWarp(pos, bhv.worldFacingDirection);
                        break;

                    case WarpPlayableAsset.WarpFacing.FaceTarget:
                        {
                            Vector3 dir = Vector3.zero;
                            if (bhv.lookAt != null)
                            {
                                dir = (bhv.lookAt.position - pos);
                                dir.y = 0f;
                            }
                            ctrl.TryWarp(pos, dir);
                        }
                        break;
                }

                // 必要ならロック終了（位置だけ瞬間移動して解放）
                if (bhv.endLockAfterWarp) ctrl.EndLock(resetState: true);

                bhv.fired = true; // このクリップでは一度きり
            }
        }
    }
}
