using System;
using UnityEngine;
using UnityEngine.InputSystem;
using AnoGame.Data;
using AnoGame.Application.Inventory;
using System.Collections.Generic;
using VContainer;
using AnoGame.Application.Input;  // IInputActionProvider の名前空間

namespace AnoGame.Application.Player
{
    [AddComponentMenu("Inventory/" + nameof(ItemCollector))]
    public class ItemCollector : MonoBehaviour
    {
        [Header("Collection Settings")]
        [SerializeField] private float collectRadius = 2.0f;
        [SerializeField] private LayerMask itemLayer;
        [SerializeField] private float viewAngle = 90.0f;

        [Inject] private InventoryManager _inventoryManager;
        [Inject] private IInputActionProvider _inputProvider;

        private InputAction _interactAction;

        private void Awake()
        {
            // (1) まず Player マップを有効化しておく
            // _inputProvider.SwitchToPlayer();

            // (2) Player マップを取得し、"Interact" アクションをキャッシュ
            var playerMap = _inputProvider.GetPlayerActionMap();
            _interactAction = playerMap.FindAction("Interact", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            // (3) Interact アクションの登録
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
            // キーを押した瞬間に CollectItem を呼ぶ
            if (context.performed)
            {
                CollectItem();
            }
        }

        public void CollectItem()
        {
            // インベントリがいっぱいなら何もしない
            if (_inventoryManager.IsInventoryFull())
            {
                Debug.LogWarning("Inventory is full!");
                return;
            }

            // 周囲にあるアイテムを検出
            Collider[] items = Physics.OverlapSphere(transform.position, collectRadius, itemLayer);
            Collider closestItem = FindClosestItemInViewAngle(items);

            if (closestItem != null)
            {
                CollectableItem collectableItem = closestItem.GetComponent<CollectableItem>();
                if (collectableItem != null && _inventoryManager.AddItem(collectableItem))
                {
                    collectableItem.OnCollected();
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

        public IReadOnlyList<Domain.Data.Models.InventoryItem> GetInventory()
        {
            return _inventoryManager.GetInventory();
        }

        private void OnDrawGizmosSelected()
        {
            // Collection 範囲の可視化
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectRadius);

            // 視野角の可視化
            Vector3 rightDirection = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;
            Vector3 leftDirection = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, rightDirection * collectRadius);
            Gizmos.DrawRay(transform.position, leftDirection * collectRadius);
        }
    }
}
