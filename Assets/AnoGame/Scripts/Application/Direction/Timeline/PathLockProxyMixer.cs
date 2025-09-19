using UnityEngine;
using UnityEngine.Playables;

public sealed class PathLockProxyMixer : PlayableBehaviour
{
    // 状態キャッシュ用
    private bool _locked;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var proxy = playerData as TimelineEventLockProxy;
        if (proxy == null) return;

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

        /*
        // 向きの制御（例）
        switch (bhv.faceMode)
        {
            case FaceMode.FaceMove:
                proxy.FaceTowards(bhv.moveDirection); // moveDirection は適宜算出 or Asset 側で保持
                break;
            case FaceMode.FaceTarget:
                if (bhv.lookAt != null)
                {
                    var dir = (bhv.lookAt.position - bhv.target.position).normalized;
                    proxy.FaceTowards(dir);
                }
                break;
            case FaceMode.Keep:
            default:
                break;
        }
        */
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        // 再生終端でロックを戻しておく
        // （必要であれば、最後に見ていた proxy を覚えておき、EndLock を確実に打つ）
    }
}
