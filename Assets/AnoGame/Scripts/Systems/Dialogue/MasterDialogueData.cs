using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Systems.Dialogue
{
    [CreateAssetMenu(fileName = "MasterDialogueData", menuName = "AnoGame/Dialogue/Master Dialogue Data")]
    public class MasterDialogueData : ScriptableObject
    {
        [Tooltip("List of all conversation units in the game.")]
        public List<ConversationUnit> Conversations = new List<ConversationUnit>();

        /// <summary>
        /// Retrieves a conversation unit by its ID.
        /// Note: For runtime performance, the manager should cache these in a Dictionary.
        /// </summary>
        public ConversationUnit GetConversationByID(string id)
        {
            foreach (var unit in Conversations)
            {
                if (unit.ID == id)
                {
                    return unit;
                }
            }
            return null;
        }
    }
}
