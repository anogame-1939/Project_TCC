using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnoGame.Story
{
    [Serializable]
    public class EvnetData
    {
        public int EvnentID = 0;
        public string EvnentName = "default";
        public EventType EvnentType = EventType.Normal;
        [SerializeField]
        private List<GameObject> _spwanObjects;
        public List<GameObject> SpwanObjects => _spwanObjects;

        public Scene scene;
        public List<Scene> Scenes; 

        public SceneReference sceneReference;
    }

    public enum EventType
    {
        Normal,

    }

}