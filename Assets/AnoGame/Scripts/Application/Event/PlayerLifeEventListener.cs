using UnityEngine;
using UnityEngine.Events;
using UniRx;
using AnoGame.Domain.Event;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// Listens for Player life events (Miss, Death) and triggers UnityEvents.
    /// This allows designers to hook up effects and game over logic in the Inspector.
    /// </summary>
    public class PlayerLifeEventListener : MonoBehaviour
    {
        [Header("Events")]
        [Tooltip("Triggered when player takes damage but survives.")]
        public UnityEvent OnMiss;

        [Tooltip("Triggered when player dies (lives <= 0).")]
        public UnityEvent OnDeath;

        private void Start()
        {
            // Subscribe to PlayerMissEvent
            MessageBroker.Default.Receive<PlayerMissEvent>()
                .Subscribe(_ => OnMiss?.Invoke())
                .AddTo(this);

            // Subscribe to PlayerDeathEvent
            MessageBroker.Default.Receive<PlayerDeathEvent>()
                .Subscribe(_ => OnDeath?.Invoke())
                .AddTo(this);
        }
    }
}
