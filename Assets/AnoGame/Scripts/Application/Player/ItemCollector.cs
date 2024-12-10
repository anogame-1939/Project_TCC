using UnityEngine;
using UnityEngine.InputSystem;
using AnoGame.Data;
using System.Collections.Generic;
using System.Linq;
using VContainer;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame.Application.Player
{
    [AddComponentMenu("Inventory/" + nameof(ItemCollector))]
    public class ItemCollector : MonoBehaviour
    {
        [Inject] private IEventService _eventService;
        [Inject] private IKeyItemService _keyItemService;

        [Inject]
        public void Construct(
            IEventService eventService,
            IKeyItemService keyItemService)
        {
            _eventService = eventService;
            _keyItemService = keyItemService;
        }

        [Header("Collection Settings")]
        [SerializeField] private float collectRadius = 2.0f;
        [SerializeField] private LayerMask itemLayer;
        [SerializeField] private float viewAngle = 90.0f;

        [Header("Inventory Settings")]
        [SerializeField] private int maxInventorySize = 20;

        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private GameManager _gameManager;

        // インベントリデータへの直接アクセスを制限し、GameManagerを通じて管理
        private List<InventoryItem> Inventory => _gameManager?.CurrentGameData?.inventory;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput != null)
            {
                _interactAction = _playerInput.actions["Interact"];
            }

            _gameManager = GameManager.Instance;
            if (_gameManager == null)
            {
                Debug.LogError($"{nameof(GameManager)} not found!");
                enabled = false;
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
            if (Inventory == null || Inventory.Count >= maxInventorySize)
            {
                Debug.LogWarning("Inventory is full or not initialized!");
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
            if (Inventory == null) return;

            var itemData = collectableItem.ItemData;
            InventoryItem newItem = new InventoryItem
            {
                itemName = itemData.ItemName,
                quantity = collectableItem.Quantity,
                description = itemData.Description
            };

            // キーアイテムの場合、イベントをトリガー
            if (_keyItemService.IsKeyItem(newItem.itemName))
            {
                _eventService.TriggerKeyItemEvent(newItem.itemName);
            }

            // 既存のインベントリ処理
            var existingItem = Inventory.FirstOrDefault(item => item.itemName == newItem.itemName);
            if (existingItem != null)
            {
                existingItem.quantity += newItem.quantity;
            }
            else
            {
                Inventory.Add(newItem);
            }

            // ゲームデータの更新
            // _gameDataService.UpdateGameData(_gameDataService.CurrentGameData);
        }

        public IReadOnlyList<InventoryItem> GetInventory()
        {
            return Inventory?.AsReadOnly();
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