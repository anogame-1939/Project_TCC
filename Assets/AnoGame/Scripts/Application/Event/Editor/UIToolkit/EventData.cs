using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Event.Editor.UIToolkit
{
    [Serializable]
    public class EventList
    {
        public List<EventJsonItem> events;
    }

    [Serializable]
    public class EventJsonItem
    {
        public string eventId;
        public string name;
        public string category;
        public string description;
        public List<string> conditions;
        public List<string> results;
    }
}
