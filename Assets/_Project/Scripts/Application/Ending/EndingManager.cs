using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Ending
{
    public class EndingManager : MonoBehaviour
    {
        private static int currentEndingIndex = 0; // 現在のエンディングインデックス

        [SerializeField]
        public int CurrentEndingIndex => currentEndingIndex; // プロパティで取得

        [SerializeField]
        private PlayableDirector playableDirector1;

        [SerializeField]
        private PlayableDirector playableDirector2;

        [SerializeField]
        private PlayableDirector playableDirector3;

        public void SetEndingIndex(int index)
        {
            currentEndingIndex = index;
        }

        public void PlayDirection()
        {
            Debug.Log("エンディング index: " + currentEndingIndex);
            if (currentEndingIndex == 0)
            {
                playableDirector1.Play();
            }
            else if (currentEndingIndex == 1)
            {
                playableDirector2.Play();
            }
            else if (currentEndingIndex == 2)
            {
                playableDirector3.Play();
            }
        }

    }
}