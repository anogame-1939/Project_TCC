using UnityEngine;
using UnityEngine.Splines;

namespace AnoGame.Application.Enemy.AI
{
    public class PatrolRouteRegistry : MonoBehaviour, IPatrolRouteRegistry
    {
        [SerializeField] private SplineContainer splineContainer;

        public SplineContainer GetPatrolRoute() => splineContainer;
    }
}
