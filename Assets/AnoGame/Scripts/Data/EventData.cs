using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Data
{
    [CreateAssetMenu(fileName = "EventData", menuName = "Game/EventData")]
    public class EventData : ScriptableObject
    {
        [SerializeField] private string eventId;
        [SerializeField] private string eventName;
        [SerializeField] private string description;
        [SerializeField] private bool isOneTime; // 一回限りのイベントかどうか

        public string EventId => eventId;
        public string EventName => eventName;
        public string Description => description;
        public bool IsOneTime => isOneTime;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(eventId))
            {
                eventId = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}