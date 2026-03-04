using UnityEngine;
using UnityEngine.AI;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField]
    Transform targetPoint;

    private NavMeshAgent navMeshAgent;
    private Vector3 lastTargetPosition;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        // NavMesh上にエージェントが存在するか確認
        if (!navMeshAgent.isOnNavMesh)
        {
            Debug.LogError("NavMesh Agent is not on the NavMesh. Please make sure the agent is placed correctly.");
            return;
        }

        // 初期位置を記録
        lastTargetPosition = targetPoint.position;
        navMeshAgent.SetDestination(targetPoint.position);
    }

    void Update()
    {
        // 目標地点が変わった場合のみSetDestinationを呼び出す
        if (targetPoint.position != lastTargetPosition)
        {
            navMeshAgent.SetDestination(targetPoint.position);
            lastTargetPosition = targetPoint.position;
        }
    }

    void OnDrawGizmos()
    {
        if (navMeshAgent && navMeshAgent.enabled) {
            Gizmos.color = Color.red;
            var prefPos = transform.position;
    
            foreach (var pos in navMeshAgent.path.corners) {
                Gizmos.DrawLine(prefPos, pos);
                prefPos = pos;
            }
        }
    }
}
