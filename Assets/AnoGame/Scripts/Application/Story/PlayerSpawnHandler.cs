using UnityEngine;

namespace AnoGame.Application.Story
{
    public class PlayerSpawnHandler : MonoBehaviour
    {   
        public void SpawnAtStartPoint()
        {
            PlayerSpawnManager.Instance.SpawnPlayerAtStart();
        }
    }
}