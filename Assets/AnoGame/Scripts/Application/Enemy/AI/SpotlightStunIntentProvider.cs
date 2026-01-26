using UnityEngine;
using UniRx;
using AnoGame.Application.Message;

namespace AnoGame.Application.Enemy.AI
{
    public class SpotlightStunIntentProvider : MonoBehaviour, IIntentProvider
    {
        [SerializeField] private int priority = 200; // Higher than chase/investigate
        
        private bool _active = false;
        private float _expireAt = 0f;

        public int Priority => priority;

        private void Start()
        {
            MessageBroker.Default.Receive<SpotlightActivationMessage>()
                .Subscribe(msg =>
                {
                    float dist = Vector3.Distance(transform.position, msg.Position);
                    if (dist <= msg.Radius)
                    {
                        Activate(msg.Duration);
                    }
                })
                .AddTo(this);
        }

        public bool IsActive()
        {
            if (!_active) return false;
            if (Time.time >= _expireAt)
            {
                _active = false;
                return false;
            }
            return true;
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

        private void Activate(float duration)
        {
            _expireAt = Time.time + duration;
            _active = true;
            Debug.Log($"[SpotlightStun] Stunned for {duration} seconds.");
        }
    }
}
