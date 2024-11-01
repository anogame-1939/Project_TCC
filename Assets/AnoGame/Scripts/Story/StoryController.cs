using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Story
{
    public class StoryController : MonoBehaviour
    {
        [SerializeField]
        bool _runAtStart = false;
        [SerializeField]
        int _chapterIndex = 0;

        void Start()
        {
            if (_runAtStart)
            {
                LoadChapter();
            }
        }

        public void LoadChapter()
        {
            StoryManager.Instance.LoadChapter(_chapterIndex);
            StoryManager.Instance.UpdateGameData();
        }
    }
}
