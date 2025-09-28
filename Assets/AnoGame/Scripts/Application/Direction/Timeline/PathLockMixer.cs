using UnityEngine.Playables;
using AnoGame.Application.Player.Control; // EventLockControl の名前空間

namespace AnoGame.Application.Direction.Timeline
{
    public class PathLockMixer : PlayableBehaviour
    {
        EventLockControl _ctrl;
        bool _locked;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _ctrl = playerData as EventLockControl;
            if (_ctrl == null) return;

            int inputCount = playable.GetInputCount();
            var resolver = playable.GetGraph().GetResolver();

            // ロック要否 & メイン（最も重い）クリップを決定
            bool anyActive = false;
            bool needLock = false;

            double maxW = 0;
            ScriptPlayable<PathLockPlayableBehaviour> mainPlayable = default;
            PathLockPlayableBehaviour mainB = null;

            for (int i = 0; i < inputCount; i++)
            {
                var ip = (ScriptPlayable<PathLockPlayableBehaviour>)playable.GetInput(i);
                var w = playable.GetInputWeight(i);
                if (w <= 0) continue;

                anyActive = true;
                var b = ip.GetBehaviour();
                if (b.lockDuringClip) needLock = true;

                if (w > maxW) { maxW = w; mainPlayable = ip; mainB = b; }
            }

            // ロック管理（再生中のどれか1つでも lockDuringClip ならオン）
            if (needLock && !_locked) { _ctrl.BeginLock(); _locked = true; }
            if (!needLock && _locked) { _ctrl.EndLock(); _locked = false; }

            if (mainB == null) return;

            // 向き設定（最も重いクリップに従う）
            switch (mainB.turnMode)
            {
                case PathLockPlayableAsset.TurnMode.Keep: _ctrl.LookKeep(); break;
                case PathLockPlayableAsset.TurnMode.FaceMove: _ctrl.LookFaceMove(); break;
                case PathLockPlayableAsset.TurnMode.FaceTarget:
                    {
                        var t = mainB.lookAt;
                        if (t != null) _ctrl.LookAt(t);
                        else _ctrl.LookFaceMove();
                    }
                    break;
            }

            // 目的地 Transform を解決
            var targetTf = mainB.target;
            if (targetTf == null) return;

            int id = targetTf.GetInstanceID();

            // ★重要：このクリップでまだ MoveToPoint を発行していない、または目的地が変わった時だけ一度だけ発行
            if (!mainB.issuedOnce || mainB.targetInstanceId != id)
            {
                _ctrl.MoveToPoint(targetTf.position, mainB.moveSpeed, mainB.stopDistance);
                mainB.issuedOnce = true;
                mainB.targetInstanceId = id;
            }

            // 以降は「到着するまで」再発行しない（到着判定は EventLockControl 側の stopDistance に任せる）
            // クリップ切替時は別インスタンスの Behaviour になるため、新しいクリップで再び一度だけ発行される。
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_locked && _ctrl != null) _ctrl.EndLock();
            _locked = false;
        }
    }
}