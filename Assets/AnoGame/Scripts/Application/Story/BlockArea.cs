using UnityEngine;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Story
{
    [RequireComponent(typeof(Collider))]
    public class BlockArea : MonoBehaviour
    {
        [SerializeField]
        Transform returnPosition;

        [SerializeField]
        private float returnDuration = 1.0f; // 移動にかかる時間

        [SerializeField]
        private Ease returnEase = Ease.InOutQuad; // イージングの種類

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
                CharacterBrain characterBrain = other.GetComponent<CharacterBrain>();
                characterBrain.enabled = false;

                // Animatorを探して取得
                Animator animator = FindAnimatorInHierarchy(other.gameObject);
                if (animator != null)
                {
                    Debug.Log($"Found Animator on {animator.gameObject.name}");
                    // ここでAnimatorに対する処理を追加できます
                }
                
                if (returnPosition != null)
                {
                    // 以下、既存のコード
                    Vector3 newPosition = returnPosition.position;
                    newPosition.y = other.transform.position.y;
                    
                    other.transform.DOMove(newPosition, returnDuration)
                        .SetEase(returnEase)
                        .OnComplete(() => 
                        {
                            characterBrain.enabled = true;
                        });
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

        private Animator FindAnimatorInHierarchy(GameObject targetObject)
        {
            // 直接アタッチされているAnimatorを確認
            Animator animator = targetObject.GetComponent<Animator>();
            if (animator != null)
            {
                return animator;
            }

            // 子オブジェクトを再帰的に検索
            Animator[] childAnimators = targetObject.GetComponentsInChildren<Animator>();
            if (childAnimators != null && childAnimators.Length > 0)
            {
                return childAnimators[0]; // 最初に見つかったAnimatorを返す
            }

            return null;
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