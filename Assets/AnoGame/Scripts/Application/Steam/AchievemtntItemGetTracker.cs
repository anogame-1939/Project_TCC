using UnityEngine;
using UniRx;
using static AnoGame.Application.Steam.SteamStatsAndAchievements;

namespace AnoGame.Application.Steam
{
    /// <summary>
    /// すべての古井手記を手に入れた
    /// </summary>
    [DisallowMultipleComponent]
    public class AchievemtntItemGetTracker : MonoBehaviour
    {
        void OnEnable()
        {
            MessageBroker.Default
                .Receive<GetAllLore>()
                .Subscribe(_ => EntryLore())
                .AddTo(this);
        }

        private void EntryLore()
        {
            Debug.Log("EntryLore");
            SteamStatsAndAchievements.Instance.ForceUnlockAchievement(Achievement.LORE);
        }
    }


}
