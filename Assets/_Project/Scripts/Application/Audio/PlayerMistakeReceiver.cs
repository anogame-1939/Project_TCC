using UnityEngine;
using UnityEngine.Events;
using UniRx;
using AnoGame.Domain.Event;

namespace AnoGame.Application.Audio
{
    /// <summary>
    /// プレイヤーのミス（Miss, Death）イベントを受信し、UnityEventを発火するコンポーネント。
    /// BGMの停止などを想定。
    /// </summary>
    public class PlayerMistakeReceiver : MonoBehaviour
    {
        [Tooltip("ミスまたは死亡時に発火するイベント")]
        [SerializeField] private UnityEvent onMistake;

        private void Start()
        {
            // PlayerMissEventを受信
            MessageBroker.Default.Receive<PlayerMissEvent>()
                .Subscribe(_ => OnMistake("Miss"))
                .AddTo(this);

            // PlayerDeathEventを受信
            MessageBroker.Default.Receive<PlayerDeathEvent>()
                .Subscribe(_ => OnMistake("Death"))
                .AddTo(this);
        }

        private void OnMistake(string reason)
        {
            Debug.Log($"[PlayerMistakeReceiver] Mistake received: {reason}");
            onMistake?.Invoke();
        }
    }
}
