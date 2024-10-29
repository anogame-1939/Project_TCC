using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Story
{
    [RequireComponent(typeof(Collider))]
    public class BlockArea : MonoBehaviour
    {
        [SerializeField]
        Transform returnPosition;

        private void Start()
        {
            if (returnPosition == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Return Position が設定されていません！");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("プレイヤーがトリガー内に入りました");
                
                if (returnPosition != null)
                {
                    // 現在のY座標を保持した新しい位置を作成
                    Vector3 newPosition = returnPosition.position;
                    newPosition.y = other.transform.position.y;
                    
                    // プレイヤーの位置と回転を設定
                    other.transform.position = newPosition;
                    other.transform.rotation = returnPosition.rotation;

                    StoryManager.Instance.LoadChapter(1);
                }
                else
                {
                    Debug.LogError($"[{gameObject.name}] Return Position が設定されていません！");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("プレイヤーがトリガーから出ました");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (returnPosition != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(returnPosition.position, 0.5f);
                Gizmos.DrawLine(transform.position, returnPosition.position);
            }
        }

        private void PingObject()
        {
            EditorGUIUtility.PingObject(this.gameObject);
        }
#endif
    }
}