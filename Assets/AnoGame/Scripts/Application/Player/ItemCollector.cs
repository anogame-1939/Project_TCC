using UnityEngine;
using UnityEngine.InputSystem;
using AnoGame.Data;
using System.Collections.Generic;
using System.Linq;

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
        private GameManager _gameManager;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput != null)
            {
                // Interactアクションの参照を取得
                _interactAction = _playerInput.actions["Interact"];
            }

            _gameManager = GameManager.Instance;
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
            var itemData = collectableItem.ItemData;
            InventoryItem newItem = new InventoryItem
            {
                itemName = itemData.ItemName,
                quantity = collectableItem.Quantity,
                description = itemData.Description,
                // itemImage = collectableItem.ItemImage
            };

            // まず古いアイテムをInventoryから削除
            InventoryItem existingItem = inventory.Find(item => item.itemName == newItem.itemName);
            if (existingItem != null)
            {
                inventory.Remove(existingItem);
                // 数量を加算
                newItem.quantity += existingItem.quantity;
            }

            // 新しいアイテムを追加
            inventory.Add(newItem);
            Debug.Log($"Collected: {newItem.itemName} x{newItem.quantity}");

            // GameManagerの状態を更新
            AddItemToInventory2(newItem);
        }

        private void AddItemToInventory2(InventoryItem newItem)
        {
            var currentGameData = _gameManager.CurrentGameData;
            if (currentGameData == null) return;

            // 同じitemNameのアイテムを削除
            var existingItem = currentGameData.inventory.FirstOrDefault(x => x.itemName == newItem.itemName);
            if (existingItem != null)
            {
                currentGameData.inventory.Remove(existingItem);
                newItem.quantity += existingItem.quantity;
            }

            // 新しいアイテムを追加
            currentGameData.inventory.Add(newItem);

            // GameManagerに更新を通知
            _gameManager.UpdateGameState(currentGameData);
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