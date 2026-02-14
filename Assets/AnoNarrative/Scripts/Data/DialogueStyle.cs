using UnityEngine;
using System.Collections.Generic;

namespace AnoGame.AnoDialogue.Data
{
    [CreateAssetMenu(fileName = "DialogueStyleValues", menuName = "AnoGame/AnoNarrative/Dialogue Style Values")]
    public class DialogueStyle : ScriptableObject
    {
        [Tooltip("Define the available style names here (e.g., 'Standard', 'Narration', 'Flashback')")]
        public List<string> styleNames = new List<string>() { "Standard" };
    }
}
