using UnityEngine;
using AnoGame.Data;
using AnoGame.AnoFlow;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// イベントオブジェクトのルートを示すコンポーネント。
    /// ダッシュボード等からのアクセスを容易にするためのハブとして機能する。
    /// </summary>
    [SelectionBase]
    public class AnoEventRoot : MonoBehaviour
    {
        [Tooltip("このイベントの参照データ (EventData)")]
        [SerializeField] private EventData _eventData;

        [Header("References")]
        [Tooltip("このイベントに属する Receptor オブジェクト")]
        [SerializeField] private GameObject _receptor;

        [Tooltip("このイベントに属する Trigger オブジェクト (Timelineを持つ等)")]
        [SerializeField] private GameObject _trigger;

        public EventData EventData => _eventData;
        public GameObject Receptor => _receptor;
        public GameObject Trigger => _trigger;

        /// <summary>
        /// 参照を設定する（主にエディタツールからの自動セットアップ用）
        /// </summary>
        public void Setup(EventData eventData, GameObject receptor, GameObject trigger)
        {
            _eventData = eventData;
            _receptor = receptor;
            _trigger = trigger;
        }

        /// <summary>
        /// キャッシュされた Receptor から ReceptorBase などのコンポーネントを取得する
        /// </summary>
        public T GetReceptorComponent<T>() where T : Component
        {
            if (_receptor != null)
            {
                return _receptor.GetComponent<T>();
            }
            return null;
        }

        /// <summary>
        /// キャッシュされた Trigger からコンポーネントを取得する
        /// </summary>
        public T GetTriggerComponent<T>() where T : Component
        {
            if (_trigger != null)
            {
                return _trigger.GetComponent<T>();
            }
            return null;
        }
    }
}
