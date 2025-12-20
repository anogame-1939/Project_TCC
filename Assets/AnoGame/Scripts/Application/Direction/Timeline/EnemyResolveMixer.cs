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

                var playbleInput = (ScriptPlayable<EnemyResolveBehaviour>)playable.GetInput(i);
                var behaviour = playbleInput.GetBehaviour();

                blendedResolve += behaviour.resolveAmount * weight;
                totalWeight += weight;
            }

            // ブレンドされたリゾルブ値を適用
            // ウェイトの合計が1未満の場合の挙動（補完するか、デフォルト値に戻すか）は
            // プロジェクトの要件に合わせる必要があるが、ここでは単純に適用する。
            if (totalWeight > 0f)
            {
                controller.SetResolve(blendedResolve);
            }
        }
    }
}
