using UnityEngine;

namespace AnoGame.Application.Story
{
    public class StoryHandler : MonoBehaviour
    {
        public void LoadStory(int storyIndex)
        {
            StoryManager.Instance.LoadStory(storyIndex);
            StoryManager.Instance.UpdateGameData();
        }

        public void LoadChapter(int chapterIndex)
        {
            StoryManager.Instance.LoadChapter(chapterIndex);
            StoryManager.Instance.UpdateGameData();
        }
    }
}
