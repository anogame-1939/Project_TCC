#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace AnoGame.Application.Steam.Editor
{
    /// <summary>
    /// Unity Editor extension to reset Steam stats and achievements.
    /// Adds a menu item under Tools/Steam to perform the reset.
    /// </summary>
    public static class ResetSteamStatsEditor
    {
        // TODO: Set your own App ID here
        private static readonly AppId_t AppId = (AppId_t)3332140;

        [MenuItem("Tools/Steam/Reset All Steam Stats and Achievements")]
        public static void ResetAllSteamStats()
        {
#if DISABLESTEAMWORKS
            Debug.LogWarning("Steamworks is disabled in this build. Cannot reset stats.");
#else
            // Restart through Steam to ensure correct AppID
            if (SteamAPI.RestartAppIfNecessary(AppId))
            {
                Debug.Log("Steam client restarted for AppID " + AppId);
                return;
            }

            // Initialize Steam API
            if (!SteamAPI.Init())
            {
                Debug.LogError("SteamAPI_Init() failed. Ensure Steam client is running and AppID is correct.");
                return;
            }

            // Reset stats and achievements
            bool resetOk = SteamUserStats.ResetAllStats(true);
            bool storeOk = SteamUserStats.StoreStats();
            Debug.Log($"Steam stats reset: {resetOk}, stats stored: {storeOk}");

            // Shutdown Steam API
            SteamAPI.Shutdown();
#endif
        }
    }
}
#endif
