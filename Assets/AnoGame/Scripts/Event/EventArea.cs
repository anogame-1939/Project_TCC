using System;
using System.Collections.Generic;
using AnoGame.Utility;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Event
{
    [RequireComponent(typeof(Collider))]
    public class EventArea : MonoBehaviour
    {
        [SerializeField]
        int _eventAreaId = 0;

        void Start()
        {
#if UNITY_EDITOR
            if (!TestDataManager.EventAreaIdList.Contains(_eventAreaId))
            {
                TestDataManager.EventAreaIdList.Add(_eventAreaId);
            }
            else
            {
                Debug.LogWarning($"EventAreaIdが重複しています。{_eventAreaId}, {name}", this);
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