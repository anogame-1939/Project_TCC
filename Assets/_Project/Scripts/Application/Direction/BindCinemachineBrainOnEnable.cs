using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Cinemachine;

namespace AnoGame.Application.Direction
{

    [RequireComponent(typeof(PlayableDirector))]
    public sealed class BindCinemachineBrainOnEnable : MonoBehaviour
    {
        [Tooltip("明示したい場合だけ設定。未設定なら Camera.main を使用")]
        public Camera brainCamera;

        void OnEnable()
        {
            var director = GetComponent<PlayableDirector>();
            if (!(director.playableAsset is TimelineAsset timeline)) return;

            var cam = brainCamera ? brainCamera : Camera.main;
            if (!cam) return;

            var brain = cam.GetComponent<CinemachineBrain>();
            if (!brain) return;

            // Timeline 内の全トラックを見て、CinemachineBrain を要求するトラックにバインド
            foreach (var output in timeline.outputs)
            {
                // Cinemachine Track は出力ターゲット型が CinemachineBrain
                if (output.outputTargetType == typeof(CinemachineBrain))
                {
                    // sourceObject は TrackAsset。ここに対して GenericBinding をセットする
                    director.SetGenericBinding(output.sourceObject, brain);
                }
            }
        }
    }
}
