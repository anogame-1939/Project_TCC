using UnityEngine;
using UnityEngine.InputSystem;
using Unity.TinyCharacterController.Utility;

namespace AnoGame
{
    [AddComponentMenu("Test/" + nameof(ItemCollector))]
    public class ItemCollector : MonoBehaviour
    {
        [SerializeField]
        public float collectRadius = 2.0f;
        public LayerMask itemLayer;

        [SerializeField]
        public float viewAngle = 90.0f;
        private PlayerInput _playerInput;

        private void OnInteract(InputAction.CallbackContext context)
        {
            CollectItem();
        }

        public void CollectItem()
        {
            Debug.Log("CollectItem");
            Collider[] items = Physics.OverlapSphere(transform.position, collectRadius, itemLayer);
            Collider closestItem = null;
            float closestDistance = float.MaxValue;

            foreach (Collider item in items)
            {
                Debug.Log("CollectItem... ");
                Vector3 directionToItem = (item.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToItem);

Debug.Log($"angle... {angle}-{viewAngle}");
                if (angle <= viewAngle / 2)
                {
                    
                    float distance = Vector3.Distance(transform.position, item.transform.position);

                    Debug.Log($"distance... {distance}-{closestDistance}");
                    if (distance < closestDistance)
                    {
                        
                        closestDistance = distance;
                        closestItem = item;
                    }
                }
            }

            if (closestItem != null)
            {
                // アイテムを回収する処理をここに書きます
                Debug.Log($"Collected: {closestItem.name}");
                // 例：アイテムを非表示にする
                closestItem.gameObject.SetActive(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // コレクション範囲を表示するためのデバッグ用
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectRadius);
        }


    }
}