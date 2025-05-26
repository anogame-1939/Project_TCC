using UnityEngine;

namespace AnoGame.Application.Steam
{
    [AddComponentMenu("Steam/SteamAchievementHandler")]
    public class SteamAchievementHandler : MonoBehaviour
    {
        public void UnlockStartGame()        => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.START_GAME);
        public void UnlockWin100Games()      => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.ACH_WIN_100_GAMES);
        public void UnlockHeavyFire()        => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.ACH_HEAVY_FIRE);
        public void UnlockTravelFarAccum()   => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.ACH_TRAVEL_FAR_ACCUM);
        public void UnlockTravelFarSingle()  => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.ACH_TRAVEL_FAR_SINGLE);
    }
}