using UnityEngine;
using UnityEngine.InputSystem;
using AnoGame.Data;
using System.Collections.Generic;

namespace AnoGame.Application.Player
{
    [AddComponentMenu("Inventory/" + nameof(ItemCollector))]
    public class ItemCollector : MonoBehaviour
    {
        [Header("Collection Settings")]
        [SerializeField] private float collectRadius = 2.0f;
        [SerializeField] private LayerMask itemLayer;
        [SerializeField] private float viewAngle = 90.0f;

        [Header("Inventory Settings")]
        [SerializeField] private List<InventoryItem> inventory = new List<InventoryItem>();
        [SerializeField] private int maxInventorySize = 20;

        private PlayerInput _playerInput;
        private InputAction _interactAction;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput != null)
            {
                // Interactアクションの参照を取得
                _interactAction = _playerInput.actions["Interact"];
            }
        }

        private void OnEnable()
        {
            if (_interactAction != null)
            {
                _interactAction.performed += OnInteract;
            }
        }

        private void OnDisable()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteract;
            }
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                CollectItem();
            }
        }

        public void CollectItem()
        {
            if (inventory.Count >= maxInventorySize)
            {
                Debug.LogWarning("Inventory is full!");
                return;
            }

            Collider[] items = Physics.OverlapSphere(transform.position, collectRadius, itemLayer);
            Collider closestItem = FindClosestItemInViewAngle(items);

            if (closestItem != null)
            {
                CollectableItem collectableItem = closestItem.GetComponent<CollectableItem>();
                if (collectableItem != null)
                {
                    AddItemToInventory(collectableItem);
                    closestItem.gameObject.SetActive(false);
                }
            }
        }

        private Collider FindClosestItemInViewAngle(Collider[] items)
        {
            Collider closestItem = null;
            float closestDistance = float.MaxValue;

            foreach (Collider item in items)
            {
                Vector3 directionToItem = (item.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToItem);

                if (angle <= viewAngle / 2)
                {
                    float distance = Vector3.Distance(transform.position, item.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestItem = item;
                    }
                }
            }

            return closestItem;
        }

        private void AddItemToInventory(CollectableItem collectableItem)
        {
            InventoryItem newItem = new InventoryItem
            {
                itemName = collectableItem.ItemName,
                quantity = collectableItem.Quantity,
                description = collectableItem.Description,
                itemImage = collectableItem.ItemImage
            };

            // 既存のアイテムがある場合は数量を加算
            InventoryItem existingItem = inventory.Find(item => item.itemName == newItem.itemName);
            if (existingItem != null)
            {
                existingItem.quantity += newItem.quantity;
            }
            else
            {
                inventory.Add(newItem);
            }

            Debug.Log($"Collected: {newItem.itemName} x{newItem.quantity}");
        }

        public List<InventoryItem> GetInventory()
        {
            return inventory;
        }

        private void OnDrawGizmosSelected()
        {
            // Collection range visualization
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectRadius);

            // View angle visualization
            Vector3 rightDirection = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;
            Vector3 leftDirection = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, rightDirection * collectRadius);
            Gizmos.DrawRay(transform.position, leftDirection * collectRadius);
        }
    }
}