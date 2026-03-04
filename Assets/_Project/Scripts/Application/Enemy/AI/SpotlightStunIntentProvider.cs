using UnityEngine;
using UniRx;
using AnoGame.Application.Message;
using System.Collections.Generic;

namespace AnoGame.Application.Enemy.AI
{
    public class SpotlightStunIntentProvider : MonoBehaviour, IIntentProvider
    {
        [SerializeField] private int priority = 200; // Higher than chase/investigate

        private class ActiveSpotlightData
        {
            public Vector3 Position;
            public float Radius;
            public string LightId;
        }

        private List<ActiveSpotlightData> _activeSpotlights = new List<ActiveSpotlightData>();

        private bool _isStunned = false;

        public int Priority => priority;

        private void Start()
        {
            MessageBroker.Default.Receive<SpotlightActivationMessage>()
                .Subscribe(msg =>
                {
                    // Remove existing if same ID to avoid duplicates (or update it)
                    _activeSpotlights.RemoveAll(x => x.LightId == msg.LightId);

                    var data = new ActiveSpotlightData
                    {
                        Position = msg.Position,
                        Radius = msg.Radius,
                        LightId = msg.LightId
                    };
                    _activeSpotlights.Add(data);
                    Debug.Log($"[SpotlightStun] Spotlight Activated: {msg.LightId}");
                })
                .AddTo(this);

            MessageBroker.Default.Receive<SpotlightDeactivationMessage>()
                .Subscribe(msg =>
                {
                    int removedCount = _activeSpotlights.RemoveAll(x => x.LightId == msg.LightId);
                    if (removedCount > 0)
                    {
                        Debug.Log($"[SpotlightStun] Spotlight Deactivated: {msg.LightId}");
                    }
                })
                .AddTo(this);
        }

        private void Update()
        {
            // Persistent logic: No longer checking for expiration time.
            // Explicit deactivation is required.

            // Check if currently stunned
            bool shouldBeStunned = false;
            foreach (var light in _activeSpotlights)
            {
                float dist = Vector3.Distance(transform.position, light.Position);
                if (dist <= light.Radius)
                {
                    shouldBeStunned = true;
                    if (!_isStunned)
                    {
                        Debug.Log($"[SpotlightStun] Enemy Entered Spotlight! ID: {light.LightId}, Distance: {dist:F2}");
                    }
                    break;
                }
            }

            _isStunned = shouldBeStunned;

            if (_isStunned)
            {
                Debug.Log("[SpotlightStun] Enemy is stunned.");
            }
        }

        public bool IsActive()
        {
            return _isStunned;
        }

        public bool TryGetGoal(out MoveGoal goal)
        {
            if (!IsActive())
            {
                goal = default;
                return false;
            }

            // Stop in place
            goal = MoveGoal.FromPosition(transform.position);
            return true;
        }
    }
}
