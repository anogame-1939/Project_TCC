using UnityEngine;
using AnoGame.Data;
using AnoGame.AnoFlow;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// イベントの視覚的分類用カラータグ（7色）
    /// </summary>
    public enum EventColorTag
    {
        None = 0,
        Red = 1,
        Orange = 2,
        Yellow = 3,
        Green = 4,
        Blue = 5,
        Purple = 6,
        White = 7
    }

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

        [Header("Visual")]
        [Tooltip("シーンギズモのカラータグ")]
        [SerializeField] private EventColorTag _colorTag = EventColorTag.None;

        public EventData EventData => _eventData;
        public GameObject Receptor => _receptor;
        public GameObject Trigger => _trigger;
        public EventColorTag ColorTag
        {
            get => _colorTag;
            set => _colorTag = value;
        }

        /// <summary>
        /// カラータグに対応する色を取得する
        /// </summary>
        public static Color GetTagColor(EventColorTag tag)
        {
            switch (tag)
            {
                case EventColorTag.Red: return new Color(0.9f, 0.2f, 0.2f, 1f);
                case EventColorTag.Orange: return new Color(0.9f, 0.55f, 0.1f, 1f);
                case EventColorTag.Yellow: return new Color(0.95f, 0.85f, 0.1f, 1f);
                case EventColorTag.Green: return new Color(0.2f, 0.8f, 0.3f, 1f);
                case EventColorTag.Blue: return new Color(0.2f, 0.5f, 0.9f, 1f);
                case EventColorTag.Purple: return new Color(0.6f, 0.3f, 0.9f, 1f);
                case EventColorTag.White: return new Color(0.9f, 0.9f, 0.9f, 1f);
                default: return new Color(0.2f, 0.8f, 0.4f, 1f); // デフォルト緑
            }
        }

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
