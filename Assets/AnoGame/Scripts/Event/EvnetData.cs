using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Event
{
    [Serializable]
    public class EvnetData
    {
        public int EvnentID = 0;
        public string EvnentName = "default";
        public EventType EvnentType = EventType.Normal;
        [SerializeField]
        public List<GameObject> _spwanObjects;
    }

    public enum EventType
    {
        Normal,

    }

}