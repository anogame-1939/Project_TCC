using System.Collections.Generic;
#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
using UnityEngine;

namespace Unity.ScenarioImporter
{
    /// <summary>
    /// Class representing a single dialogue entry in the game.
    /// This includes the text of the dialogue, related events, and the tag it belongs to.
    /// This class is used to represent the flow of scenarios and dialogues.
    /// </summary>
    [System.Serializable]
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("Scenario.Message")]
#endif
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("Utj.ScenarioImporter.DialogueEntry")]
#endif
    public class DialogueEntry
    {
        /// <summary>
        /// Text content of the message
        /// </summary>
        [Multiline]
        public string Text;

        /// <summary>
        /// Events stored within the message
        /// </summary>
        public List<DialogueEvent> Events = new();

        /// <summary>
        /// Tag to which the message belongs
        /// </summary>
        public string Tag;
    }
}