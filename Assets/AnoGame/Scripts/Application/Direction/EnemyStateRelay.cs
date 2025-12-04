// AnoGame.Application.Direction
using System;
using AnoGame.Application.Player.Interaction;
using AnoGame.Application.Player.Perception;
using UniRx;
using UnityEngine;

namespace AnoGame.Application.Direction
{
    public sealed class EnemyStateRelay : MonoBehaviour
    {
        [SerializeField] EnemyBehaviorCoordinator director;
        [SerializeField] Player.Controller.PlayerStealthController targetPlayer;

        private void Start()
        {
            if (targetPlayer == null)
            {
                // Try to find player if not assigned
                targetPlayer = FindObjectOfType<Player.Controller.PlayerStealthController>();
            }

            if (targetPlayer != null)
            {
                targetPlayer.ObserveEveryValueChanged(p => p.IsHidden)
                    .Where(isHidden => isHidden)
                    .Subscribe(_ =>
                    {
                        director.NotifyLost(targetPlayer.transform.position);
                    })
                    .AddTo(this);
            }
        }
    }
}
