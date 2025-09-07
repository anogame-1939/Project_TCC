using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Splines;
using AnoGame.Application.Player.Control; // EventLockControl の名前空間

[TrackColor(0.3f, 0.8f, 1f)]
[TrackClipType(typeof(PathLockPlayableAsset))]
[TrackBindingType(typeof(EventLockControl))] // ← バインド先は EventLockControl
public class PathLockTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        => ScriptPlayable<PathLockMixer>.Create(graph, inputCount);
}

public class PathLockMixer : PlayableBehaviour
{
    EventLockControl _ctrl;
    bool _locked;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        _ctrl = playerData as EventLockControl;
        if (_ctrl == null) return;

        int inputCount = playable.GetInputCount();
        var resolver   = playable.GetGraph().GetResolver();

        // 重なりブレンドに対応：各クリップの t を重み付き平均
        float accumT = 0f, accumW = 0f;
        SplineContainer chosenPath = null;
        Transform lookAt = null;
        PathLockPlayableAsset.TurnMode turnMode = PathLockPlayableAsset.TurnMode.FaceMove;
        float moveSpeed = 0f, stopDist = 0.05f;
        bool anyActive = false;
        bool needLock = false;

        double maxW = 0;
        for (int i = 0; i < inputCount; i++)
        {
            var inputPlayable = (ScriptPlayable<PathLockPlayableBehaviour>)playable.GetInput(i);
            var weight = playable.GetInputWeight(i);
            if (weight <= 0) continue;

            var b = inputPlayable.GetBehaviour();
            var path = b.path.Resolve(resolver);
            if (!path || path.Spline == null) continue;

            // 0..1 のクリップ内正規化時間
            double dur = Mathf.Max(0.0001f, (float)inputPlayable.GetDuration());
            float t01 = Mathf.Clamp01((float)(inputPlayable.GetTime() / dur));
            float eased = b.easing != null ? b.easing.Evaluate(t01) : t01;
            float t = Mathf.Lerp(b.from, b.to, eased);

            accumT += t * (float)weight;
            accumW += (float)weight;
            anyActive = true;
            if (b.lockDuringClip) needLock = true;

            // 最優先（最も重い）クリップのメタを採用
            if (weight > maxW) {
                maxW = weight;
                chosenPath = path;
                moveSpeed = b.moveSpeed;
                stopDist  = b.stopDistance;
                turnMode  = b.turnMode;
                lookAt    = b.lookAt.Resolve(resolver);
            }
        }

        // ロック管理
        if (needLock && !_locked) { _ctrl.BeginLock(); _locked = true; }
        if (!needLock && _locked) { _ctrl.EndLock();   _locked = false; }

        // 位置目標の更新
        if (anyActive && accumW > 0f && chosenPath != null)
        {
            float finalT = accumT / accumW;
            var pos = (Vector3)chosenPath.EvaluatePosition(finalT);

            // 進行方向の向き設定
            switch (turnMode)
            {
                case PathLockPlayableAsset.TurnMode.Keep:
                    _ctrl.LookKeep();
                    break;
                case PathLockPlayableAsset.TurnMode.FaceMove:
                    _ctrl.LookFaceMove();
                    break;
                case PathLockPlayableAsset.TurnMode.FaceTarget:
                    if (lookAt != null) _ctrl.LookAt(lookAt);
                    else _ctrl.LookFaceMove();
                    break;
            }

            // 目標点へ移動（EventLockControl の ToPoint を使用）
            _ctrl.MoveToPoint(pos, moveSpeed, stopDist);
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (_locked && _ctrl != null) _ctrl.EndLock();
        _locked = false;
    }
}
