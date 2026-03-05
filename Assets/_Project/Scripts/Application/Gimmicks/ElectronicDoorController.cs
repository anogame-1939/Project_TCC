using UnityEngine;
using AnoGame.Application.Interfaces;
using AnoGame.Application.Managers;
using DG.Tweening;

namespace AnoGame.Application.Gimmicks
{
    /// <summary>
    /// 電子ドアの制御クラス
    /// EnergyManagerによって制御され、電力供給時に開き、遮断時に閉じる
    /// DOTweenで門の回転とコントロールパネルのEmission色を制御する
    /// </summary>
    public class ElectronicDoorController : MonoBehaviour, IEnergyConsumer
    {
        [Header("Settings")]
        [SerializeField] private string _doorId;
        [SerializeField] private bool _startOpen = false;

        [Header("Gate Doors")]
        [SerializeField] private Transform _gatePivotLeft;
        [SerializeField] private Transform _gatePivotRight;
        [SerializeField] private float _openAngle = 90f;
        [SerializeField] private float _duration = 1.0f;
        [SerializeField] private Ease _easeType = Ease.InOutQuad;

        [Header("Control Panel Emission")]
        [SerializeField] private Renderer _panelRenderer;
        [SerializeField] private int _emissionMaterialIndex = 0;
        [SerializeField] private Color _closedEmissionColor = Color.red;
        [SerializeField] private Color _openEmissionColor = Color.blue;
        [SerializeField] private float _emissionIntensity = 1f;

        [Header("Debug")]
        [SerializeField] private bool _isOpen;

        private Sequence _currentSequence;
        private MaterialPropertyBlock _propertyBlock;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public string DeviceName => $"ElectronicDoor_{_doorId}";

        /// <summary>
        /// UnityEventなどから呼び出すためのメソッド
        /// </summary>
        public void RequestActivation()
        {
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.RequestPower(this);
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_doorId))
            {
                _doorId = gameObject.name;
            }

            // Automatically register to EnergyManager in Editor
            var manager = FindAnyObjectByType<EnergyManager>();
            if (manager != null)
            {
                manager.Register(this);
            }
        }

        private void Start()
        {
            _propertyBlock = new MaterialPropertyBlock();

            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.Register(this);
            }

            // 初期状態を即座に設定（アニメーションなし）
            if (_startOpen)
            {
                SetGateImmediate(true);
            }
            else
            {
                SetGateImmediate(false);
            }
        }

        private void OnDestroy()
        {
            _currentSequence?.Kill();

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
            PlayGateAnimation(true);
        }

        public void OnEnergyCut()
        {
            if (!_isOpen) return;

            _isOpen = false;
            Debug.Log($"[ElectronicDoor] Closed: {_doorId}");
            PlayGateAnimation(false);
        }

        /// <summary>
        /// 門の開閉アニメーションとEmission色変更を実行
        /// </summary>
        private void PlayGateAnimation(bool open)
        {
            _currentSequence?.Kill();
            _currentSequence = DOTween.Sequence();

            float targetAngle = open ? _openAngle : 0f;
            Color targetColor = open ? _openEmissionColor : _closedEmissionColor;
            Color hdrColor = targetColor * _emissionIntensity;

            // 左門の回転（Y軸正方向に開く）
            if (_gatePivotLeft != null)
            {
                _currentSequence.Join(
                    _gatePivotLeft.DOLocalRotate(new Vector3(0f, targetAngle, 0f), _duration)
                        .SetEase(_easeType)
                );
            }

            // 右門の回転（Y軸負方向に開く＝反対方向）
            if (_gatePivotRight != null)
            {
                _currentSequence.Join(
                    _gatePivotRight.DOLocalRotate(new Vector3(0f, -targetAngle, 0f), _duration)
                        .SetEase(_easeType)
                );
            }

            // コントロールパネルのEmission色変更
            if (_panelRenderer != null)
            {
                Color currentColor = GetCurrentEmissionColor();
                _currentSequence.Join(
                    DOVirtual.Color(currentColor, hdrColor, _duration, color =>
                    {
                        _panelRenderer.GetPropertyBlock(_propertyBlock, _emissionMaterialIndex);
                        _propertyBlock.SetColor(EmissionColorId, color);
                        _panelRenderer.SetPropertyBlock(_propertyBlock, _emissionMaterialIndex);
                    }).SetEase(_easeType)
                );
            }
        }

        /// <summary>
        /// 初期状態を即座に設定する（Start時用）
        /// </summary>
        private void SetGateImmediate(bool open)
        {
            _isOpen = open;
            float angle = open ? _openAngle : 0f;
            Color targetColor = open ? _openEmissionColor : _closedEmissionColor;
            Color hdrColor = targetColor * _emissionIntensity;

            if (_gatePivotLeft != null)
            {
                _gatePivotLeft.localEulerAngles = new Vector3(0f, angle, 0f);
            }

            if (_gatePivotRight != null)
            {
                _gatePivotRight.localEulerAngles = new Vector3(0f, -angle, 0f);
            }

            if (_panelRenderer != null)
            {
                _panelRenderer.GetPropertyBlock(_propertyBlock, _emissionMaterialIndex);
                _propertyBlock.SetColor(EmissionColorId, hdrColor);
                _panelRenderer.SetPropertyBlock(_propertyBlock, _emissionMaterialIndex);
            }
        }

        /// <summary>
        /// 現在のEmission色を取得
        /// </summary>
        private Color GetCurrentEmissionColor()
        {
            if (_panelRenderer == null) return _closedEmissionColor;

            _panelRenderer.GetPropertyBlock(_propertyBlock, _emissionMaterialIndex);
            return _propertyBlock.GetColor(EmissionColorId);
        }
    }
}
