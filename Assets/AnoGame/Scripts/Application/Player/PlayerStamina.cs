using UnityEngine;
using UniRx;
using System;

namespace AnoGame.Application.Player
{
    [AddComponentMenu("Player/" + nameof(PlayerStamina))]
    public class PlayerStamina : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float drainRate = 20f;     // per second
        [SerializeField] private float recoveryRate = 15f;  // per second
        [SerializeField] private float recoveryDelay = 1.0f;

        [Header("State (ReadOnly)")]
        [SerializeField] private float currentStamina;
        [SerializeField] private bool isExhausted;
        [SerializeField] private bool isRecovering;

        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;
        public bool IsExhausted => isExhausted;

        // Reactive property for UI if needed
        private ReactiveProperty<float> _currentStaminaRx;
        public IReadOnlyReactiveProperty<float> CurrentStaminaRx => _currentStaminaRx;

        private float _recoveryTimer;

        private void Awake()
        {
            currentStamina = maxStamina;
            _currentStaminaRx = new ReactiveProperty<float>(currentStamina);
        }

        private void Update()
        {
            if (isRecovering)
            {
                RecoverProcess();
            }
            else
            {
                // Delay before recovery starts if we are not consuming
                if (_recoveryTimer > 0)
                {
                    _recoveryTimer -= Time.deltaTime;
                    if (_recoveryTimer <= 0)
                    {
                        isRecovering = true;
                    }
                }
            }

            _currentStaminaRx.Value = currentStamina;
        }

        public bool CanSprint()
        {
            // Cannot sprint if exhausted until fully recovered? 
            // Or just until we have some stamina?
            // Requirement said "Recover until fully recovered" (implied by "cannot dash until recovery")
            // "スタミナを使い切ったらスタミナが回復するまでダッシュを実行できない" 
            // usually means full recovery or at least leaving exhausted state.
            // Let's assume we stay exhausted until full or a threshold. 
            // Typical implementation: Exhausted -> Wait -> Recover to Max -> Exhausted Cleared.
            return !isExhausted && currentStamina > 0;
        }

        public void Consume()
        {
            // Reset recovery
            isRecovering = false;
            _recoveryTimer = recoveryDelay;

            currentStamina -= drainRate * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true;
            }
        }

        private void RecoverProcess()
        {
            currentStamina += recoveryRate * Time.deltaTime;
            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                isExhausted = false; // Recovered from exhaustion
                isRecovering = false; // Full
            }
        }
    }
}
