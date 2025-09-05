using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;

public class SplinePatrolAgent : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    private NavMeshAgent agent;
    private int currentIndex = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (splineContainer != null && splineContainer.Spline != null)
        {
            MoveToKnot(currentIndex);
        }
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentIndex = (currentIndex + 1) % splineContainer.Spline.Count;
            MoveToKnot(currentIndex);
        }
    }

    private void MoveToKnot(int index)
    {
        var spline = splineContainer.Spline;
        var knot = spline[index];
        Vector3 worldPos = knot.Position;
        agent.SetDestination(worldPos);
    }
}
