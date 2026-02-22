using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.AnoDialogue.Timeline
{
    /// <summary>
    /// SceneImageTrack の Mixer。
    /// バインディングから DialogueTimelineReceiver を取得し、
    /// 各 SceneImageBehaviour に渡す。
    /// </summary>
    public class SceneImageMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);

            var receiver = playerData as DialogueTimelineReceiver;
            if (receiver == null)
            {
                Debug.LogWarning("[SceneImageMixer] ProcessFrame: playerData (receiver) is NULL! Track binding may be missing.");
                return;
            }

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight > 0f)
                {
                    var inputPlayable = playable.GetInput(i);
                    var behaviour = ((ScriptPlayable<SceneImageBehaviour>)inputPlayable).GetBehaviour();
                    if (behaviour.receiver == null)
                    {
                        Debug.Log($"[SceneImageMixer] Assigning receiver to behaviour[{i}]: sprite={(behaviour.sceneImage != null ? behaviour.sceneImage.name : "NULL")}");
                    }
                    behaviour.receiver = receiver;
                }
            }
        }
    }
}
