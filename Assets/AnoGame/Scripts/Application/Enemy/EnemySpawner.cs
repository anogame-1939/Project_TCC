using UnityEngine;
using AnoGame.Application.Story;

namespace AnoGame.Application.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField]
        private GameObject enemyPrefab;

        private void Start()
        {
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.ChapterLoaded += OnChapterLoaded;

                // すでにロードが完了している場合は即座に実行
                if (!StoryManager.Instance.IsLoadingScene())
                {
                    OnChapterLoaded(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.ChapterLoaded -= OnChapterLoaded;
            }
        }

        private void OnChapterLoaded(bool useRetryPoint)
        {
            Spawn();
        }

        private void Spawn()
        {
            if (enemyPrefab != null)
            {
                // Instantiate(enemyPrefab, transform.position, transform.rotation);
                EnemySpawnManager.Instance.Spawn(enemyPrefab, transform.position, transform.rotation);
            }
        }
    }
}