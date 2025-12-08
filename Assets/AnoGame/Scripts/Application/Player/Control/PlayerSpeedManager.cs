using System.Collections.Generic;
using UnityEngine;
using Unity.TinyCharacterController.Control;

namespace AnoGame.Application.Player.Control
{
    [AddComponentMenu("Player/" + nameof(PlayerSpeedManager))]
    [RequireComponent(typeof(MoveControl))]
    public class PlayerSpeedManager : MonoBehaviour
    {
        private MoveControl _moveControl;
        private float _baseSpeed;

        // ID -> Multiplier
        private readonly Dictionary<string, float> _multipliers = new Dictionary<string, float>();

        private void Awake()
        {
            _moveControl = GetComponent<MoveControl>();
            if (_moveControl != null)
            {
                _baseSpeed = _moveControl.MoveSpeed;
            }
        }

        public void RegisterMultiplier(string key, float multiplier)
        {
            if (_multipliers.ContainsKey(key))
            {
                _multipliers[key] = multiplier;
            }
            else
            {
                _multipliers.Add(key, multiplier);
            }
            UpdateSpeed();
        }

        public void UnregisterMultiplier(string key)
        {
            if (_multipliers.ContainsKey(key))
            {
                _multipliers.Remove(key);
                UpdateSpeed();
            }
        }

        private void UpdateSpeed()
        {
            if (_moveControl == null) return;

            float finalMultiplier = 1.0f;
            foreach (var val in _multipliers.Values)
            {
                finalMultiplier *= val;
            }

            _moveControl.MoveSpeed = _baseSpeed * finalMultiplier;
        }
    }
}
