using System;
using System.Collections.Generic;
using AnoGame.Utility;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Story
{
    [RequireComponent(typeof(Collider))]
    public class ChapterArea : MonoBehaviour
    {
        [SerializeField]
        int _chapterId = 0;

        void Start()
        {
#if UNITY_EDITOR
            if (!TestDataManager.EventAreaIdList.Contains(_chapterId))
            {
                TestDataManager.EventAreaIdList.Add(_chapterId);
            }
            else
            {
                Debug.LogWarning($"EventAreaIdが重複しています。{_chapterId}, {name}", this);
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
                StoryManager.Instance.LoadChapter(_chapterId);
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