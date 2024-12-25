	
using System;
using UnityEngine;

namespace AnoGame.Application.Core
{
    [Serializable]
    public class ApplicationConfigs : ScriptableObject
    {
        public bool AutoReady = false;
        public int MaxPlayers = 4;
        public int MinPlayers = 2;
        public int ConnetRetryCount = 3;
        public string LobbyName = "Lobby";

        public OtherConfig OtherConfig;
    }

    [Serializable]
    public class OtherConfig
    {
        public string GameVersion = "";
    }
}
