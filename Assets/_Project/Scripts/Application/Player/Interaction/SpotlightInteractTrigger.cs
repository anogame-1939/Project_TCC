using UnityEngine;
using UniRx;
using AnoGame.Application.Message;
using AnoGame.Application.Interfaces;
using AnoGame.Application.Managers;

namespace AnoGame.Application.Player.Interaction
{
    public class SpotlightInteractTrigger : MonoBehaviour, IEnergyConsumer
    {
        [Header("Spotlight Settings")]
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private float radius = 5f;
        [Tooltip("Active duration in seconds. Set to negative value for infinite duration.")]
        [SerializeField] private float duration = 5f;
        [SerializeField] private string lightId;
        [SerializeField] private string _deviceName; // Changed from private property to serialized field
        [SerializeField] private bool useTransformAsPosition = true;

        // IEnergyConsumer Implementation
        public string DeviceName => string.IsNullOrEmpty(_deviceName) ? gameObject.name : _deviceName;

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
            if (string.IsNullOrEmpty(_deviceName))
            {
                _deviceName = gameObject.name;
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
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.Register(this);
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
            PublishActivation();
        }

        public void OnEnergyCut()
        {
            PublishDeactivation();
        }

        public void PublishActivation()
        {
            var pos = useTransformAsPosition ? transform.position : targetPosition;

            MessageBroker.Default.Publish(new SpotlightActivationMessage(
                pos,
                radius,
                duration,
                lightId
            ));

            Debug.Log($"[SpotlightInteractTrigger] Published activation for ID: {lightId}, Name: {DeviceName}");
        }

        public void PublishDeactivation()
        {
            MessageBroker.Default.Publish(new SpotlightDeactivationMessage(lightId));
            Debug.Log($"[SpotlightInteractTrigger] Published deactivation for ID: {lightId}, Name: {DeviceName}");
        }

        private void OnDrawGizmosSelected()
        {
            var pos = useTransformAsPosition ? transform.position : targetPosition;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos, radius);
        }
    }
}
