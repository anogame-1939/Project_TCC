using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.AnoNarrative
{
    [System.Serializable]
    public class ActorProfile
    {
        public string ActorName;
        public Sprite Portrait;
    }

    [CreateAssetMenu(fileName = "DialogueActorDatabase", menuName = "AnoGame/Dialogue/Actor Database")]
    public class DialogueActorDatabase : ScriptableObject
    {
        [SerializeField] private List<ActorProfile> profiles = new List<ActorProfile>();

        public Sprite GetPortrait(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            foreach (var profile in profiles)
            {
                if (profile.ActorName == name)
                {
                    return profile.Portrait;
                }
            }
            return null;
        }
    }
}
