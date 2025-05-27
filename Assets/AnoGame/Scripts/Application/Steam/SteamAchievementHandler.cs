using UnityEngine;

namespace AnoGame.Application.Steam
{
    [AddComponentMenu("Steam/SteamAchievementHandler")]
    public class SteamAchievementHandler : MonoBehaviour
    {
        public void UnlockStartGame()            => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.START_GAME);
        public void UnlockBeginningMorning()     => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.BEGINNING_MORNING);
        public void UnlockIndomitableSpirit()    => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.INDOMITABLE_SPIRIT);
        public void UnlockEncounterWithUnknown() => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.ENCOUNTER_WITH_UNKNOWN);
        public void UnlockMemoriesOfThatDay()    => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.MEMORIES_OF_THAT_DAY);
        public void UnlockLittleSun()            => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.LITTLE_SUN);
        public void UnlockPureHeart()            => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.PURE_HEART);
        public void UnlockUnforeseenAccident()   => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.UNFORESEEN_ACCIDENT);
        public void UnlockLore()                 => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.LORE);
        public void UnlockSevenTalismans()       => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.SEVEN_TALISMANS);
        public void UnlockGedatsu()              => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.GEDATSU);
        public void UnlockTaikan()               => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.TAIKAN);
        public void UnlockShashin()              => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.SHASHIN);
        public void UnlockJoju()                 => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.JOJU);
        public void UnlockNirvana()              => SteamStatsAndAchievements.Instance.ForceUnlockAchievement(SteamStatsAndAchievements.Achievement.NIRVANA);
    }
}