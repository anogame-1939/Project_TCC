using Steamworks;
using System;
using AnoGame.Application.Core;
using UniRx;

namespace AnoGame.Application.Steam
{
    /// <summary>
    /// Handles Steam stats and achievements for the game.
    /// Tracks stats (games played, wins, losses, distance traveled, etc.)
    /// and provides methods to unlock achievements.
    /// </summary>
    public class SteamStatsAndAchievements : SingletonMonoBehaviour<SteamStatsAndAchievements>
    {
        /// <summary>
        /// Achievements defined for the game.
        /// </summary>
        public enum Achievement : int
        {
            START_GAME,
            BEGINNING_MORNING,
            INDOMITABLE_SPIRIT,
            ENCOUNTER_WITH_UNKNOWN,
            MEMORIES_OF_THAT_DAY,
            LITTLE_SUN,
            PURE_HEART,
            UNFORESEEN_ACCIDENT,
            LORE,
            SEVEN_TALISMANS,
            GEDATSU,
            TAIKAN,
            SHASHIN,
            JOJU,
            NIRVANA
        }

        /// <summary>
        /// Internal struct representing an achievement entry.
        /// </summary>
        private class Achievement_t
        {
            public Achievement m_eAchievementID;
            public string m_strName;
            public string m_strDescription;
            public bool m_bAchieved;

            public Achievement_t(Achievement id, string name, string desc)
            {
                m_eAchievementID = id;
                m_strName = name;
                m_strDescription = desc;
                m_bAchieved = false;
            }
        }

        // Achievement list
        private Achievement_t[] m_Achievements = new Achievement_t[]
        {
            new Achievement_t(Achievement.START_GAME,    "000_First",      "Win your first game"),
            // new Achievement_t(Achievement.ACH_WIN_100_GAMES,   "Champion",    "Win 100 games"),
            // new Achievement_t(Achievement.ACH_TRAVEL_FAR_ACCUM, "Interstellar","Accumulate 5280 feet traveled"),
            // new Achievement_t(Achievement.ACH_TRAVEL_FAR_SINGLE,"Orbiter",     "Travel 500 feet in a single game"),
        };

        private CGameID m_GameID;
        private bool m_bRequestedStats;
        private bool m_bStatsValid;
        private bool m_bStoreStats;

        private float m_flGameFeetTraveled;
        private double m_flGameDurationSeconds;

        private int m_nTotalGamesPlayed;
        private int m_nTotalNumWins;
        private int m_nTotalNumLosses;
        private float m_flTotalFeetTraveled;
        private float m_flMaxFeetTraveled;
        private float m_flAverageSpeed;

        protected Callback<UserStatsReceived_t> m_UserStatsReceived;
        protected Callback<UserStatsStored_t> m_UserStatsStored;
        protected Callback<UserAchievementStored_t> m_UserAchievementStored;

        void OnEnable()
        {
            if (!SteamManager.Initialized)
                return;

            m_GameID = new CGameID(SteamUtils.GetAppID());
            m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
            m_UserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
            m_UserAchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

            m_bRequestedStats = false;
            m_bStatsValid = false;
        }

        void Update()
        {
            if (!SteamManager.Initialized)
                return;

            if (!m_bRequestedStats)
            {
                m_bRequestedStats = SteamUserStats.RequestCurrentStats();
                return;
            }

            if (!m_bStatsValid)
                return;

            EvaluateAchievements();

            if (m_bStoreStats)
                StoreStats();
        }

        private void EvaluateAchievements()
        {
            foreach (var ach in m_Achievements)
            {
                if (ach.m_bAchieved)
                    continue;

                switch (ach.m_eAchievementID)
                {
                    case Achievement.START_GAME:
                        if (m_nTotalNumWins > 0)
                            UnlockAchievement(ach);
                        break;
                    /*
                    case Achievement.ACH_WIN_100_GAMES:
                        if (m_nTotalNumWins >= 100)
                            UnlockAchievement(ach);
                        break;
                    case Achievement.ACH_TRAVEL_FAR_ACCUM:
                        if (m_flTotalFeetTraveled >= 5280f)
                            UnlockAchievement(ach);
                        break;
                    case Achievement.ACH_TRAVEL_FAR_SINGLE:
                        if (m_flGameFeetTraveled >= 500f)
                            UnlockAchievement(ach);
                        break;
                        }
                    */
                }
            }
        }

        private void StoreStats()
        {
            SteamUserStats.SetStat("NumGames", m_nTotalGamesPlayed);
            SteamUserStats.SetStat("NumWins", m_nTotalNumWins);
            SteamUserStats.SetStat("NumLosses", m_nTotalNumLosses);
            SteamUserStats.SetStat("FeetTraveled", m_flTotalFeetTraveled);
            SteamUserStats.SetStat("MaxFeetTraveled", m_flMaxFeetTraveled);
            SteamUserStats.UpdateAvgRateStat("AverageSpeed", m_flGameFeetTraveled, m_flGameDurationSeconds);
            SteamUserStats.GetStat("AverageSpeed", out m_flAverageSpeed);

            m_bStoreStats = !SteamUserStats.StoreStats();
        }

        /// <summary>
        /// Adds distance traveled for the current game session.
        /// </summary>
        public void AddDistanceTraveled(float distance)
        {
            m_flGameFeetTraveled += distance;
        }

        /// <summary>
        /// Force-unlocks a specific achievement via code or UnityEvent.
        /// </summary>
        public void ForceUnlockAchievement(Achievement achievementID)
        {
            var entry = Array.Find(m_Achievements, a => a.m_eAchievementID == achievementID);
            if (entry != null && !entry.m_bAchieved)
            {
                UnlockAchievement(entry);
            }
        }

        private void UnlockAchievement(Achievement_t achievement)
        {
            achievement.m_bAchieved = true;
            SteamUserStats.SetAchievement(achievement.m_eAchievementID.ToString());
            m_bStoreStats = true;
        }

        private void OnUserStatsReceived(UserStatsReceived_t callback)
        {
            if ((ulong)m_GameID != callback.m_nGameID || callback.m_eResult != EResult.k_EResultOK)
                return;

            m_bStatsValid = true;
            foreach (var ach in m_Achievements)
            {
                SteamUserStats.GetAchievement(ach.m_eAchievementID.ToString(), out ach.m_bAchieved);
                ach.m_strName = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "name");
                ach.m_strDescription = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "desc");
            }

            SteamUserStats.GetStat("NumGames", out m_nTotalGamesPlayed);
            SteamUserStats.GetStat("NumWins", out m_nTotalNumWins);
            SteamUserStats.GetStat("NumLosses", out m_nTotalNumLosses);
            SteamUserStats.GetStat("FeetTraveled", out m_flTotalFeetTraveled);
            SteamUserStats.GetStat("MaxFeetTraveled", out m_flMaxFeetTraveled);
        }

        private void OnUserStatsStored(UserStatsStored_t callback)
        {
            if ((ulong)m_GameID != callback.m_nGameID)
                return;

            if (callback.m_eResult == EResult.k_EResultInvalidParam)
                SteamUserStats.RequestCurrentStats();
        }

        private void OnAchievementStored(UserAchievementStored_t callback)
        {
            // Optional: handle progress/logging
        }

        /// <summary>
        /// すべての
        /// </summary>
        /// <returns></returns>
        bool AreAllAchievementsCleared()
        {
            // ユーザーデータをリクエスト（必要に応じて）
            SteamUserStats.RequestUserStats(SteamUser.GetSteamID());

            uint total = SteamUserStats.GetNumAchievements();
            for (uint i = 0; i < total; i++)
            {
                string achName = SteamUserStats.GetAchievementName(i);
                bool achieved;
                SteamUserStats.GetAchievement(achName, out achieved);
                if (!achieved)
                    return false;
            }
            return true;
        }

    }
    public class PlayerRetriedEvent { } // Placeholder for actual event class
}
