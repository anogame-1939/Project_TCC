using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    [System.Serializable]
    public class WarpPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        public enum WarpFacing { Keep, FaceDirection, FaceTarget }

        [Header("Destination")]
        public ExposedReference<Transform> target;   // 目的地(Transform) or
        public Vector3 worldPosition;                // 直指定
        public bool useTarget = true;

        [Header("Facing")]
        public WarpFacing facing = WarpFacing.Keep;
        public Vector3 worldFacingDirection = Vector3.zero;          // FaceDirection用
        public ExposedReference<Transform> lookAt;                   // FaceTarget用

        [Header("Lock (optional)")]
        public bool beginLockBeforeWarp = false;  // Warp直前にBeginLockしたい場合
        public bool endLockAfterWarp = false;     // Warp直後にEndLockしたい場合

        public ClipCaps clipCaps => ClipCaps.None; // 1ショット用

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<WarpPlayableBehaviour>.Create(graph);
            var b = playable.GetBehaviour();

            var resolver = graph.GetResolver();
            b.target = target.Resolve(resolver);
            b.useTarget = useTarget;
            b.worldPosition = worldPosition;

            b.facing = facing;
            b.worldFacingDirection = worldFacingDirection;
            b.lookAt = lookAt.Resolve(resolver);

            b.beginLockBeforeWarp = beginLockBeforeWarp;
            b.endLockAfterWarp = endLockAfterWarp;

            return playable;
        }
    }

    public class WarpPlayableBehaviour : PlayableBehaviour
    {
        // Resolved
        public Transform target;
        public bool useTarget;
        public Vector3 worldPosition;

        public WarpPlayableAsset.WarpFacing facing;
        public Vector3 worldFacingDirection;
        public Transform lookAt;

        public bool beginLockBeforeWarp;
        public bool endLockAfterWarp;

        [System.NonSerialized] public bool fired;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            Debug.Log("[WarpPlayableBehaviour] OnBehaviourPlay - Resetting fired flag");
            fired = false; // クリップ頭に入るたびリセット
        }
    }
}
