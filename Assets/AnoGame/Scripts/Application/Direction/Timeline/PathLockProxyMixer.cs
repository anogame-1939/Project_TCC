using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    public sealed class PathLockProxyMixer : PlayableBehaviour
    {
        // 状態キャッシュ用
        private bool _locked;
        private TimelineEventLockProxy _lastProxy;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var proxy = playerData as TimelineEventLockProxy;
            if (proxy == null) return;

            // プロキシをキャッシュ（OnPlayableDestroy用）
            _lastProxy = proxy;

            int inputCount = playable.GetInputCount();

            // クリップ混在時の「最も重い」クリップを選ぶ例
            int dominant = -1;
            float maxW = 0f;
            for (int i = 0; i < inputCount; i++)
            {
                float w = playable.GetInputWeight(i);
                if (w > maxW)
                {
                    maxW = w;
                    dominant = i;
                }
            }

            if (dominant < 0 || maxW <= 0f)
            {
                // ロック終了の取り扱い（必要なら）
                if (_locked)
                {
                    proxy.EndLock();
                    _locked = false;
                }
                return;
            }

            // クリップのデータを読む
            var inputPlayable = (ScriptPlayable<PathLockPlayableBehaviour>)playable.GetInput(dominant);
            var bhv = inputPlayable.GetBehaviour();

            // ロック制御（一度打ち）
            if (bhv.lockDuringClip && !_locked)
            {
                proxy.BeginLock();
                _locked = true;
            }

            // 目標 Transform は PlayableAsset 側で ExposedReference を Resolve 済みとして持たせる前提
            if (bhv.target != null)
            {
                proxy.MoveTo(bhv.target, bhv.moveSpeed, bhv.stopDistance);
            }

            // 向きの制御（例）
            switch (bhv.turnMode)
            {
                case PathLockPlayableAsset.TurnMode.Keep:
                    proxy.LookKeep();
                    break;

                case PathLockPlayableAsset.TurnMode.FaceMove:
                    proxy.LookFaceMove();
                    break;

                case PathLockPlayableAsset.TurnMode.FaceTarget:
                    if (bhv.lookAt != null)
                    {
                        proxy.LookAt(bhv.lookAt);
                    }
                    else
                    {
                        // FaceTarget だが lookAt 未設定ならフォールバック
                        proxy.LookFaceMove();
                    }
                    break;
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            // 再生終端でロックを戻しておく
            // Timeline停止時などに呼ばれる
            if (_locked && _lastProxy != null)
            {
                _lastProxy.EndLock();
                _locked = false;
            }
        }
    }
}