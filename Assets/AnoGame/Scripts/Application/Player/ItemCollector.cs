using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using VContainer;
using AnoGame.Application.Input;
using AnoGame.Messages;

namespace AnoGame.Application.Player
{
    [AddComponentMenu("Inventory/" + nameof(ItemCollector))]
    public class ItemCollector : MonoBehaviour
    {
        [Header("Collection Settings")]
        [SerializeField] private float collectRadius = 2.0f;
        [SerializeField] private LayerMask itemLayer;
        [SerializeField] private float viewAngle = 90.0f;

        [Inject] private IInputActionProvider _inputProvider;

        private InputAction _interactAction;

        private void Awake()
        {
            var playerMap = _inputProvider.GetPlayerActionMap();
            _interactAction = playerMap.FindAction("Interact", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            if (_interactAction != null)
                _interactAction.performed += OnInteract;
        }

        private void OnDisable()
        {
            if (_interactAction != null)
                _interactAction.performed -= OnInteract;
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;

            var items = Physics.OverlapSphere(transform.position, collectRadius, itemLayer);
            var target = FindClosestItemInView(items);
            if (target == null) return;

            var collectable = target.GetComponent<CollectableItem>();
            if (collectable == null) return;

            // ここでは “拾いたい” という要求だけを投げる
            MessageBroker.Default.Publish(
                new TryCollectItemRequest(transform, collectable)
            );
        }

        private Collider FindClosestItemInView(Collider[] items)
        {
            Collider closest = null;
            float min = float.MaxValue;
            foreach (var it in items)
            {
                var dir = (it.transform.position - transform.position).normalized;
                var ang = Vector3.Angle(transform.forward, dir);
                if (ang > viewAngle * 0.5f) continue;

                var d = Vector3.Distance(transform.position, it.transform.position);
                if (d < min) { min = d; closest = it; }
            }
            return closest;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectRadius);

            var right = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;
            var left  = Quaternion.Euler(0,-viewAngle / 2, 0) * transform.forward;
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, right * collectRadius);
            Gizmos.DrawRay(transform.position, left  * collectRadius);
        }
    }
}
