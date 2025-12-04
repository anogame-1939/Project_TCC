using UnityEngine.Splines;

namespace AnoGame.Application.Enemy.AI
{
    public interface IPatrolRouteRegistry
    {
        SplineContainer GetPatrolRoute();
    }
}
