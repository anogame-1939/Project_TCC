using System.Collections.Generic;
#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.ScenarioImporter
{
    /// <summary>
    /// ScriptableObject class for managing scenario conversations and events.
    /// This class stores different sections and messages within a scenario
    /// and provides functionality to retrieve specific scenario sections based on tags.
    /// </summary>
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("Scenario.ScenarioData")]
#endif
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("Utj.ScenarioImporter.DialogueScript")]
#endif
    public class DialogueScript : ScriptableObject
    {
        /// <summary>
        /// List of DialogueEntry
        /// </summary>
        [FormerlySerializedAs("Messages")] 
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("Messages")]
#endif
        public List<DialogueEntry> DialogueEntries = new();

        /// <summary>
        /// Get the range of a scenario
        /// </summary>
        /// <param name="tag">Tag</param>
        /// <param name="start">Start point</param>
        /// <param name="end">End point</param>
        public void GetSliceIndex(string tag, out int start, out int end)
        {
            var size = DialogueEntries.Count;
            start = DialogueEntries.FindIndex(0, c => c.Tag == tag);
            end = DialogueEntries.FindIndex(start + 1, c => string.IsNullOrEmpty(c.Tag) == false) ;
            if (end < 0)
                end = DialogueEntries.Count;
        }

        /// <summary>
        /// Get dialogue entries with a specified tag
        /// </summary>
        /// <param name="tag">Tag</param>
        /// <returns>List of messages within the scenario</returns>
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("GetSlicedScenario")]
#endif
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("GetMessages")]
#endif
        public List<DialogueEntry> GetDialogueEntriesWithTag(string tag)
        {
            GetSliceIndex(tag, out var start, out var end);
            return DialogueEntries.GetRange(start, end - start);
        }
    }
}
