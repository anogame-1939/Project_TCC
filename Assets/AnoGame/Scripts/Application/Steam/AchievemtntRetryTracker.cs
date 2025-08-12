using UnityEngine;
using UniRx;
using static AnoGame.Application.Steam.SteamStatsAndAchievements;

namespace AnoGame.Application.Steam
{
    /// <summary>
    /// プレイヤーのリトライ（あきらめず再挑戦）回数を集計し、
    /// しきい値に達したら“不屈の精神”実績を解除します。
    /// </summary>
    [DisallowMultipleComponent]
    public class AchievemtntRetryTracker : MonoBehaviour
    {
        [Tooltip("何回あきらめずにリトライしたら実績解除するか")]
        [SerializeField] private int _threshold = 3;

        private int _retryCount = 0;

        void OnEnable()
        {
            // PlayerRetriedEvent が Publish されるたびに HandleRetry が呼ばれる
            MessageBroker.Default
                .Receive<PlayerRetriedEvent>()
                .Subscribe(_ => HandleRetry())
                .AddTo(this);
        }

        private void HandleRetry()
        {
            _retryCount++;
            if (_retryCount < _threshold)
            {
                SteamStatsAndAchievements.Instance.ForceUnlockAchievement(Achievement.INDOMITABLE_SPIRIT);
            }
        }
    }


}
