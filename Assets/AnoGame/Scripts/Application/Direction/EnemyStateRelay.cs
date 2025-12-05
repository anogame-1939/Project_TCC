using System;
using AnoGame.Application.Player.Interaction;
using AnoGame.Application.Player.Perception;
using AnoGame.Application.Enemy.AI;
using UniRx;
using UnityEngine;
using VContainer;

namespace AnoGame.Application.Direction
{
    public sealed class EnemyStateRelay : MonoBehaviour
    {
        [SerializeField] EnemyBehaviorCoordinator director;

        [Inject]
        IVisibleTarget targetPlayer;

        private void Start()
        {
            if (targetPlayer != null)
            {
                targetPlayer.IsHiddenObservable
                    .Where(isHidden => isHidden)
                    .Subscribe(_ =>
                    {
                        director.NotifyLost(targetPlayer.GetTransform().position);
                    })
                    .AddTo(this);
            }
        }
    }
}
