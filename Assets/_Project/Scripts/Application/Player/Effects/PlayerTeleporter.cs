
// Assets/AnoGame/Scripts/Application/Player/Effects/PlayerTeleporter.cs
using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    public class PlayerTeleporter : MonoBehaviour
    {
        [SerializeField] private Transform[] teleportPoints;

        public void TeleportToRandom()
        {
            if (teleportPoints.Length == 0) return;
            int randomIndex = Random.Range(0, teleportPoints.Length);
            transform.position = teleportPoints[randomIndex].position;
        }
    }
}