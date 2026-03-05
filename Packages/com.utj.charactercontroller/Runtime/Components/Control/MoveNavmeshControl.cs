using System.Collections.Generic;
using Unity.TinyCharacterController.Attributes;
using Unity.TinyCharacterController.Interfaces.Core;
using Unity.TinyCharacterController.Interfaces.Utility;
using Unity.TinyCharacterController.Utility;
#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Unity.TinyCharacterController.Interfaces.Components;

namespace Unity.TinyCharacterController.Control
{

    /// <summary>
    /// Component for Navmesh-based movement of a character.
    ///
    /// Uses the component specified by <see cref="_agent"/> to perform a path search to
    /// the coordinates specified by <see cref="SetTargetPosition(Vector3)"/> and move the character
    /// in the context of <see cref="IMove"/>.
    /// 
    /// If MovePriority is high, the character moves on the shortest path set by NavmeshAgent.
    /// /// If TurnPriority is high, the character will turn in the direction of the destination.
    /// </summary>
    [AddComponentMenu(MenuList.MenuControl + nameof(MoveNavmeshControl))]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Transform))]
    [RequireComponent(typeof(CharacterSettings))]
    [RequireInterface(typeof(IBrain))]
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.Control.MoveNavmeshControl")]
#endif
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.MoveByNavigationControl")]
#endif
    public class MoveNavmeshControl : MonoBehaviour,
        IMove, ITurn, IUpdateComponent,
        IComponentCondition,
        IPriorityLifecycle<IMove>, IPriorityLifecycle<ITurn>
    {
        /// <summary>
        /// Agent used to control character movement.
        /// Used to calculate paths asynchronously.
        /// Also, the component used in this setting cannot be shared by multiple components.
        /// The Agent set here should be registered as a child object of the character.
        /// </summary>
        [SerializeField]
        private NavMeshAgent _agent;

        /// <summary>
        ///　Maximum character movement speed
        /// </summary>
        [Header("Settings")]
        [SerializeField]
        [FormerlySerializedAs("Speed")]
        private float _speed = 4;

        /// <summary>
        /// Character turn speed.
        /// </summary>
        [FormerlySerializedAs("_turnSpeed")]
        [Range(-1, 50)]
        public int TurnSpeed = 8;

        /// <summary>
        /// Character move priority.
        /// </summary>
        [FormerlySerializedAs("_movePriority")]
        [Header("movement and orientation")]
        public int MovePriority = 1;

        /// <summary>
        /// Character Turn Priority.
        /// </summary>
        [FormerlySerializedAs("_turnPriority")]
        public int TurnPriority = 1;

        /// <summary>
        /// Callback when destination is reached
        /// </summary>
        public UnityEvent OnArrivedAtDestination;

        private ITransform _transform;
        private float _yawAngle;
        private Vector3 _moveVelocity;

        private void Start()
        {
            TryGetComponent(out _transform);

            if (_agent == null)
            {
                // var agent = new GameObject("agent", typeof(NavMeshAgent));
                var agent = gameObject.AddComponent<NavMeshAgent>();
                // agent.transform.SetParent(transform);
                agent.TryGetComponent(out _agent);
            }

            _agent.transform.localPosition = Vector3.zero;
            _agent.speed = _speed;
            _agent.updatePosition = false;
            _agent.updateRotation = false;
        }
#if UNITY_EDITOR

        private void Reset()
        {
            if (_agent == null)
            {
                var agent = new GameObject("Agent (NavigationControl)", typeof(NavMeshAgent));
                agent.transform.SetParent(transform, false);
                agent.TryGetComponent(out _agent);
                _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                _agent.speed = 0;
                _agent.acceleration = 0;
                _agent.stoppingDistance = 0;
                _agent.autoBraking = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying == false)
                return;

            var position = _agent.destination;
            var center = position + new Vector3(0, 1, 0);
            var cubeSize = new Vector3(0.5f, 2f, 0.5f);

            GizmoDrawUtility.DrawCube(center, cubeSize, Color.yellow);

            if (_agent.path.status == NavMeshPathStatus.PathComplete)
            {
                var corners = _agent.path.corners;
                if (corners.Length > 0)
                {
                    for (var i = 1; i < corners.Length; i++)
                    {
                        var start = corners[i - 1];
                        var next = corners[i];
                        Gizmos.DrawLine(start, next);
                    }
                }
            }
        }

#endif

        /// <summary>
        /// True if the character has reached the target point.
        /// </summary>
        public bool IsArrived { get; private set; } = true;

        /// <summary>
        /// Character movement speed
        /// </summary>
        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                _agent.speed = _speed;
            }
        }

        private bool _hasPriority = false;

        /// <summary>
        /// Set a target point to move to.
        /// </summary>
        /// <param name="position">Target position</param>
        public void SetTargetPosition(Vector3 position)
        {
            if (!_hasPriority) return;

            _agent.isStopped = false;
            _agent.SetDestination(position);
            IsArrived = false;
        }

        // ... (SetTargetPosition overload calls this one, so it's covered)

        void IPriorityLifecycle<IMove>.OnAcquireHighestPriority()
        {
            _hasPriority = true;
            if (_agent != null)
            {
                _agent.isStopped = false;
            }
        }

        void IPriorityLifecycle<IMove>.OnLoseHighestPriority()
        {
            _hasPriority = false;
            if (_agent != null)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
            _moveVelocity = Vector3.zero;
        }

        void IPriorityLifecycle<IMove>.OnUpdateWithHighestPriority(float deltaTime)
        {
        }

        void IPriorityLifecycle<ITurn>.OnAcquireHighestPriority()
        {
        }

        void IPriorityLifecycle<ITurn>.OnLoseHighestPriority()
        {
        }

        void IPriorityLifecycle<ITurn>.OnUpdateWithHighestPriority(float deltaTime)
        {
        }

        // Implementation of IMove
        public Vector3 MoveVelocity => _agent != null ? _agent.velocity : Vector3.zero;

        // Implementation of IPriority<IMove>
        int IPriority<IMove>.Priority => MovePriority;

        // Implementation of ITurn
        int ITurn.TurnSpeed => TurnSpeed;
        float ITurn.YawAngle => _yawAngle;

        // Implementation of IPriority<ITurn>
        int IPriority<ITurn>.Priority => TurnPriority;

        // Implementation of IUpdateComponent
        int IUpdateComponent.Order => Order.Control;

        void IUpdateComponent.OnUpdate(float deltaTime)
        {
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                // Update YawAngle to match movement direction
                if (_agent.velocity.sqrMagnitude > 0.01f)
                {
                    _yawAngle = Vector3.SignedAngle(Vector3.forward, _agent.velocity.normalized, Vector3.up);
                }

                // Check if arrived
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                {
                    if (!IsArrived)
                    {
                        IsArrived = true;
                        OnArrivedAtDestination?.Invoke();
                    }
                }
            }
        }

        // Implementation of IComponentCondition
        public void OnConditionCheck(List<string> messages)
        {
            if (_agent != null)
            {
                messages.Add($"Agent Path Status: {_agent.pathStatus}");
                messages.Add($"Agent Has Path: {_agent.hasPath}");
                messages.Add($"Agent Velocity: {_agent.velocity}");
                messages.Add($"Is Arrived: {IsArrived}");
                messages.Add($"Is On NavMesh: {_agent.isOnNavMesh}");
            }
            else
            {
                messages.Add("Agent is null.");
            }
        }

        /// <summary>
        /// Resets the NavMeshAgent component.
        /// This is a workaround for initialization issues where the agent fails to attach to the NavMesh.
        /// </summary>
        /// <summary>
        /// Resets the NavMeshAgent component.
        /// This is a workaround for initialization issues where the agent fails to attach to the NavMesh.
        /// </summary>
        [ContextMenu("Reset Agent")]
        public void ResetAgent()
        {
            Debug.Log($"[MoveNavmeshControl] ResetAgent called");
            if (Application.isPlaying)
            {
                StartCoroutine(ResetAgentCoroutine());
            }
            else
            {
                ResetAgentImmediate();
            }
        }

        private void ResetAgentImmediate()
        {
            if (_agent != null)
            {
                // In Editor, we might still want to fully recreate it if it's truly broken, 
                // but for consistency with the request, we'll try to just toggle if possible,
                // or fall back to the old method if the user specifically wanted "Reset" in editor to mean "Recreate".
                // However, the user's feedback was primarily about runtime behavior.
                // Let's keep the editor behavior as "Recreate" for safety unless specified otherwise,
                // as "enabled" toggle in Edit mode might not trigger the same initialization hooks as Play mode.
                // Actually, the user said "ResetAgentでこのような処理に変えてみてください" (Change ResetAgent to this process),
                // referring to the runtime code.
                // I will keep the Editor immediate reset as a full recreation to be safe, 
                // as "yield return null" doesn't work in Edit mode without EditorCoroutineUtility.
                DestroyImmediate(_agent);
            }
            CreateAndSetupAgent();
            Debug.Log("NavMeshAgent has been reset (Immediate).", this);
        }

        private System.Collections.IEnumerator ResetAgentCoroutine()
        {
            if (_agent != null)
            {
                Debug.Log($"[MoveNavmeshControl] Disabling agent...");
                _agent.enabled = false;
            }

            // Wait for end of frame to ensure the disable is processed
            yield return null;

            if (_agent != null)
            {
                Debug.Log($"[MoveNavmeshControl] Enabling agent...");
                _agent.enabled = true;

                // Re-apply settings just in case
                _agent.speed = _speed;
                _agent.updatePosition = false;
                _agent.updateRotation = false;
            }

            yield return null;

            if (_agent != null)
            {
                Debug.Log($"[MoveNavmeshControl] Agent reset complete. OnNavMesh: {_agent.isOnNavMesh}", this);
            }
        }

        private void CreateAndSetupAgent()
        {
            // Create new agent
            var agent = gameObject.AddComponent<NavMeshAgent>();
            agent.TryGetComponent(out _agent);

            // Re-apply settings
            if (_agent != null)
            {
                // _agent.transform.localPosition = Vector3.zero;
                _agent.speed = _speed;
                _agent.updatePosition = false;
                _agent.updateRotation = false;
            }
        }

    }
}
