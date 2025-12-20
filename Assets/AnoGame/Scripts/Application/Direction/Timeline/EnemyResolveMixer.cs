using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    public sealed class EnemyResolveMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var proxy = playerData as TimelineEnemyResolveProxy;
            if (proxy == null) return;

            var controller = proxy.ResolveController;
            if (controller == null) return;

            int inputCount = playable.GetInputCount();
            float totalWeight = 0f;
            float blendedResolve = 0f;

            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var playableInput = (ScriptPlayable<EnemyResolveBehaviour>)playable.GetInput(i);
                var behaviour = playableInput.GetBehaviour();

                // クリップの進行度（0〜1）を算出
                double duration = playableInput.GetDuration();
                double time = playableInput.GetTime();
                float t = (duration > 0) ? (float)(time / duration) : 1f;
                t = Mathf.Clamp01(t);

                // カーブを適用して補完
                float curveT = behaviour.curve != null ? behaviour.curve.Evaluate(t) : t;
                float currentAmount = Mathf.Lerp(behaviour.startAmount, behaviour.endAmount, curveT);

                blendedResolve += currentAmount * weight;
                totalWeight += weight;

                // エフェクト制御 (ウェイトが最大のものを優先するなどのロジックも考えられるが、ここでは単純に各クリップからHandleを呼ぶ)
                // SpriteResolveController 側で複数の呼び出しに対する整合性を取る（最後に呼ばれたものが勝つ、または再生中フラグで管理）
                controller.HandleEffect(t, behaviour.playEffect, behaviour.playThreshold, behaviour.stopThreshold);
            }

            if (totalWeight > 0f)
            {
                controller.SetResolve(blendedResolve);
            }
        }
    }
}
