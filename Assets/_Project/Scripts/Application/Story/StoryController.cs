using UnityEngine;

namespace AnoGame.Application.Story
{
    public class StoryController : MonoBehaviour
    {
        [SerializeField]
        bool _runAtStart = false;
        [SerializeField]
        int _storyIndex = 0;
        [SerializeField]
        int _chapterIndex = 0;

        void Start()
        {
            if (_runAtStart)
            {
                LoadChapter();
            }
        }

        public void LoadStory()
        {
            StoryManager.Instance.LoadStory(_storyIndex);
            StoryManager.Instance.UpdateGameData();
        }

        public void LoadChapter()
        {
            StoryManager.Instance.LoadChapter(_chapterIndex);
            StoryManager.Instance.UpdateGameData();
        }
    }
}
