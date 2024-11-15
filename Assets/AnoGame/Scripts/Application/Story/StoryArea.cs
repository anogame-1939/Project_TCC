using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Application.Story
{
    [RequireComponent(typeof(BoxCollider))]
    public class StoryArea : MonoBehaviour
    {
        [SerializeField]
        int _chapterIndex = 0;

        void Start()
        {
#if UNITY_EDITOR
            if (!TestDataManager.EventAreaIdList.Contains(_chapterIndex))
            {
                TestDataManager.EventAreaIdList.Add(_chapterIndex);
            }
            else
            {
                Debug.LogWarning($"EventAreaIdが重複しています。{_chapterIndex}, {name}", this);
                PingObject();
            }

            if (!GetComponent<Collider>().isTrigger)
            {
                Debug.LogWarning($"EventAreaにIsTriggerが設定されていません。{name}", this);
                PingObject();
            }
#endif
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("プレイヤーがトリガー内に入りました");
                // ここにプレイヤーが入った時の処理を書きます
                StoryManager.Instance.LoadChapter(_chapterIndex);
                StoryManager.Instance.UpdateGameData();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("プレイヤーがトリガーから出ました");
                // ここにプレイヤーが出た時の処理を書きます
            }
        }

#if UNITY_EDITOR
        private void PingObject()
        {
            EditorGUIUtility.PingObject(this.gameObject);
        }
#endif
    }
}