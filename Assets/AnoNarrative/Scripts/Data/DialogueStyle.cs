using UnityEngine;

namespace AnoGame.AnoNarrative.Data
{
    [CreateAssetMenu(fileName = "NewDialogueStyle", menuName = "AnoGame/AnoNarrative/Dialogue Style")]
    public class DialogueStyle : ScriptableObject
    {
        [TextArea]
        public string description;
    }
}
