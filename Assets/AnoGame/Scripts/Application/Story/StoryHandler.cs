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

        public void LoadStory2(Transform transform)
        {
            // StoryManager.Instance.LoadStory(storyIndex);
            // StoryManager.Instance.UpdateGameData();
        }

        public void LoadStory3(Vector3 vector3)
        {
            // StoryManager.Instance.LoadStory(storyIndex);
            // StoryManager.Instance.UpdateGameData();
        }

        public void LoadChapter(int chapterIndex)
        {
            StoryManager.Instance.LoadChapter(chapterIndex);
            StoryManager.Instance.UpdateGameData();
        }
    }
}
