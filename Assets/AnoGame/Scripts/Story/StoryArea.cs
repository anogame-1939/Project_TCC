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
    public class StoryArea : MonoBehaviour
    {
        [SerializeField]
        int _eventId = 0;

        void Start()
        {
#if UNITY_EDITOR
            if (!TestDataManager.EventAreaIdList.Contains(_eventId))
            {
                TestDataManager.EventAreaIdList.Add(_eventId);
            }
            else
            {
                Debug.LogWarning($"EventAreaIdが重複しています。{_eventId}, {name}", this);
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
                StoryManager.Instance.LoadChapter(1);
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