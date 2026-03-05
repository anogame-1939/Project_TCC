using System.Collections.Generic;
using UnityEngine;
using AnoGame.Application.Interfaces;

namespace AnoGame.Application.Managers
{
    /// <summary>
    /// エネルギー管理マネージャー
    /// 制御盤の鍵入手後に有効化され、各オブジェクトへの電力供給を制御する
    /// 一度に一つのオブジェクトのみactive(Open)にできる
    /// </summary>
    public class EnergyManager : MonoBehaviour
    {
        public static EnergyManager Instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool _isSystemActive = false;
        [SerializeField] private string _currentActiveDeviceName;

        private IEnergyConsumer _currentActiveDevice;
        [SerializeField] private List<UnityEngine.Object> _debugRegisteredDevices = new List<UnityEngine.Object>();
        private List<IEnergyConsumer> _registeredDevices = new List<IEnergyConsumer>();

        public bool IsSystemActive
        {
            get => _isSystemActive;
            set
            {
                if (_isSystemActive == value) return;
                _isSystemActive = value;
                Debug.Log($"[EnergyManager] System Active: {_isSystemActive}");
                if (!_isSystemActive)
                {
                    CutAllPower();
                }
            }
        }

        /// <summary>
        /// システムを有効化する
        /// </summary>
        public void Activate()
        {
            IsSystemActive = true;
        }

        /// <summary>
        /// システムを無効化する
        /// </summary>
        public void Deactivate()
        {
            IsSystemActive = false;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _debugRegisteredDevices.Clear();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// デバイスを登録
        /// </summary>
        public void Register(IEnergyConsumer device)
        {
            if (!_registeredDevices.Contains(device))
            {
                _registeredDevices.Add(device);
                if (device is UnityEngine.Object obj)
                {
                    if (!_debugRegisteredDevices.Contains(obj))
                    {
                        _debugRegisteredDevices.Add(obj);
                    }
                }
            }
        }

        /// <summary>
        /// デバイスの登録解除
        /// </summary>
        public void Unregister(IEnergyConsumer device)
        {
            if (_registeredDevices.Contains(device))
            {
                _registeredDevices.Remove(device);
                if (device is UnityEngine.Object obj)
                {
                    if (_debugRegisteredDevices.Contains(obj))
                    {
                        _debugRegisteredDevices.Remove(obj);
                    }
                }
            }
            if (_currentActiveDevice == device)
            {
                _currentActiveDevice = null;
                _currentActiveDeviceName = "None";
            }
        }

        /// <summary>
        /// 特定のデバイスへの電力供給を要求する（Open）
        /// 他のデバイスはCut（Close）される
        /// </summary>
        public void RequestPower(IEnergyConsumer device)
        {
            if (!_isSystemActive)
            {
                Debug.LogWarning("[EnergyManager] System is inactive. Cannot supply power.");
                return;
            }

            if (_currentActiveDevice == device)
            {
                // 既にActiveなら何もしない、あるいは再アクティブ化？
                // 仕様：「Openされた1個のみが存在でき、あらたに別のオブジェクトがOpenされた場合、最初の1個はCloseされ」
                return;
            }

            // 前のデバイスをオフ
            if (_currentActiveDevice != null)
            {
                _currentActiveDevice.OnEnergyCut();
            }

            // 新しいデバイスをオン
            _currentActiveDevice = device;
            _currentActiveDeviceName = device.DeviceName;

            if (_currentActiveDevice != null)
            {
                _currentActiveDevice.OnEnergySupplied();
                Debug.Log($"[EnergyManager] Power supplied to: {_currentActiveDeviceName}");
            }
        }

        private void CutAllPower()
        {
            if (_currentActiveDevice != null)
            {
                _currentActiveDevice.OnEnergyCut();
                _currentActiveDevice = null;
                _currentActiveDeviceName = "None";
            }
        }
    }
}
