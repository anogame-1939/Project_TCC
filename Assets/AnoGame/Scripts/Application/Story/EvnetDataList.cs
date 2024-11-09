using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Story
{
    public class EvnetDataList : ScriptableObject
    {
        [SerializeField]
        private List<EvnetData> _eventDataList = new List<EvnetData>();
        public List<EvnetData> EventDataList => _eventDataList;
    }
}