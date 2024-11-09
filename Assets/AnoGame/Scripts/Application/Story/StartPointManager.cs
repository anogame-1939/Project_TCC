using UnityEngine;
using Unity.TinyCharacterController.Brain;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Application.Story
{
    public class StartPointManager : MonoBehaviour
    {   
        [SerializeField]
        private const string TAG_START_POINT = "StartPoint";

        [SerializeField]
        private const string TAG_PLAYER = "Player";

        private Transform GetStartPoint()
        {
            return GameObject.FindWithTag(TAG_START_POINT).transform;
        }

        private GameObject GetPlayeyr()
        {
            return GameObject.FindWithTag(TAG_PLAYER);
        }

        private void Awake()
        {
            StoryManager.Instance.ChapterLoaded += OnChapterLoaded;
        }

        private void OnDestroy()
        {
            // イベント購読の解除
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.ChapterLoaded -= OnChapterLoaded;
            }
        }

        // インスペクターにボタンを追加
        [ContextMenu("Execute OnChapterLoaded")]
        public void ExecuteOnChapterLoaded()
        {
            OnChapterLoaded();
        }

#if UNITY_EDITOR
        // カスタムインスペクターボタンを追加
        [CustomEditor(typeof(StartPointManager))]
        public class StartPointManagerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                StartPointManager manager = (StartPointManager)target;

                // ボタンを追加
                if (GUILayout.Button("Execute OnChapterLoaded"))
                {
                    manager.ExecuteOnChapterLoaded();
                }
            }
        }
#endif

        private void OnChapterLoaded()
        {
            Debug.Log("スタートポイントを設定します。");

            // スタートポイントを取得
            var startPoint = GetStartPoint();
            if (startPoint == null)
            {
                Debug.LogError("スタートポイントの取得に失敗しました。");
                return;
            }

            var player = GetPlayeyr();
            if (player == null)
            {
                Debug.LogError("プレイヤーの取得に失敗しました。");
                return;
            }

            player.GetComponent<CharacterBrain>().Warp(startPoint.transform.position, startPoint.transform.rotation);
        }
    }
}