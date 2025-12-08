using UnityEngine;
using UniRx;
using AnoGame.Domain.Event;
using AnoGame.Application.Event;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// プレイヤーのダメージイベントを受け取り、恐怖演出などを制御するPresenter
    /// </summary>
    public class PlayerDamageFeedbackPresenter : MonoBehaviour
    {
        // 恐怖演出などで使うAudioSourceなどをここに追加できます
        [SerializeField] TimelineController timelineController;

        private void Start()
        {
            // PlayerLifeLostEventを購読
            MessageBroker.Default.Receive<PlayerLifeLostEvent>().Subscribe(e =>
            {
                OnLifeLost(e);
            }).AddTo(this);
        }

        private void OnLifeLost(PlayerLifeLostEvent e)
        {
            Debug.Log($"[PlayerDamageFeedbackPresenter] Life Lost: {e.CurrentLives}/{e.MaxLives}");

            if (e.IsGameOver)
            {
                // ゲームオーバー時の処理（必要であれば）
                // GameOverManagerが別途処理しているので、ここでは演出のみ
            }
            else
            {
                timelineController.Enqueue();
            }
        }
    }
}
