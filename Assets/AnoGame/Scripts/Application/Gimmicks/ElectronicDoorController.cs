using UnityEngine;
using AnoGame.Application.Interfaces;
using AnoGame.Application.Managers;
using Cysharp.Threading.Tasks;

namespace AnoGame.Application.Gimmicks
{
    /// <summary>
    /// 電子ドアの制御クラス
    /// EnergyManagerによって制御され、電力供給時に開き、遮断時に閉じる
    /// </summary>
    public class ElectronicDoorController : MonoBehaviour, IEnergyConsumer
    {
        [Header("Settings")]
        [SerializeField] private string _doorId;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _openTriggerName = "Open";
        [SerializeField] private string _closeTriggerName = "Close";
        [SerializeField] private bool _startOpen = false;

        [Header("Debug")]
        [SerializeField] private bool _isOpen;

        public string DeviceName => $"ElectronicDoor_{_doorId}";

        private void Start()
        {
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.Register(this);
            }

            if (_startOpen)
            {
                OnEnergySupplied();
            }
            else
            {
                OnEnergyCut();
            }
        }

        private void OnDestroy()
        {
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.Unregister(this);
            }
        }

        public void OnEnergySupplied()
        {
            if (_isOpen) return;

            _isOpen = true;
            Debug.Log($"[ElectronicDoor] Opened: {_doorId}");

            if (_animator != null)
            {
                _animator.SetTrigger(_openTriggerName);
            }
            // アニメーターがない場合の簡易処理（必要なら）
        }

        public void OnEnergyCut()
        {
            if (!_isOpen) return;

            _isOpen = false;
            Debug.Log($"[ElectronicDoor] Closed: {_doorId}");

            if (_animator != null)
            {
                _animator.SetTrigger(_closeTriggerName);
            }
        }
    }
}
