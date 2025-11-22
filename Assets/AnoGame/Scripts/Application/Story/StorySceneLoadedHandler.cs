using UnityEngine;
using UnityEngine.Events;
using AnoGame.Application.Story;

namespace AnoGame.Application.Story
{
    public class StorySceneLoadedHandler : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onSceneLoaded;

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
            onSceneLoaded?.Invoke();
        }
    }
}
