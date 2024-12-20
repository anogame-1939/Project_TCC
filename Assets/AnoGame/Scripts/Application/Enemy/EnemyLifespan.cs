using System.Collections;
using UnityEngine;

namespace AnoGame.Application.Enemy
{
// 新しいコンポーネント
    public class EnemyLifespan : MonoBehaviour
    {
        [SerializeField] private float minLifespan = 5f;
        [SerializeField] private float maxLifespan = 30f;
        
        private void OnEnable()
        {
            StartCoroutine(DestroyAfterDelay());
        }
        
        private IEnumerator DestroyAfterDelay()
        {
            float delay = Random.Range(minLifespan, maxLifespan);
            yield return new WaitForSeconds(delay);
            
            // 必要に応じてアニメーションや効果音を再生
            // gameObject.SetActive(false) または Destroy(gameObject)
            gameObject.SetActive(false);
        }
    }
}