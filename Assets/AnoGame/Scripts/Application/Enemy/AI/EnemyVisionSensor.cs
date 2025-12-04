using UnityEngine;
using AnoGame.Application.Direction;

namespace AnoGame.Application.Enemy.AI
{
    public class EnemyVisionSensor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float viewDistance = 10f;
        [SerializeField, Range(0, 360)] private float viewAngle = 90f;
        [SerializeField] private Transform eyeTransform;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("References")]
        [SerializeField] private EnemyBehaviorCoordinator director;

        private void Awake()
        {
            if (eyeTransform == null) eyeTransform = transform;
            if (director == null) director = GetComponentInParent<EnemyBehaviorCoordinator>();
        }

        private void FixedUpdate()
        {
            DetectPlayer();
        }

        private void DetectPlayer()
        {
            // Optimization: Use OverlapSphereNonAlloc if performance becomes an issue
            Collider[] targets = Physics.OverlapSphere(eyeTransform.position, viewDistance, targetLayer);

            foreach (var target in targets)
            {
                Vector3 dirToTarget = (target.transform.position - eyeTransform.position).normalized;
                if (Vector3.Angle(eyeTransform.forward, dirToTarget) < viewAngle / 2)
                {
                    float distToTarget = Vector3.Distance(eyeTransform.position, target.transform.position);
                    if (!Physics.Raycast(eyeTransform.position, dirToTarget, distToTarget, obstacleLayer))
                    {
                        // Check if target is hidden
                        if (target.TryGetComponent<IVisibleTarget>(out var visibleTarget) && visibleTarget.IsHidden)
                        {
                            continue;
                        }

                        // Player found!
                        if (director != null)
                        {
                            director.NotifyFound();
                        }
                        return; // Found, no need to check others
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (eyeTransform == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(eyeTransform.position, viewDistance);

            Vector3 leftRay = Quaternion.Euler(0, -viewAngle / 2, 0) * eyeTransform.forward;
            Vector3 rightRay = Quaternion.Euler(0, viewAngle / 2, 0) * eyeTransform.forward;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(eyeTransform.position, leftRay * viewDistance);
            Gizmos.DrawRay(eyeTransform.position, rightRay * viewDistance);
        }
    }
}
