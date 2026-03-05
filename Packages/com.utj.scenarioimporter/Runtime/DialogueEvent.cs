#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
namespace Unity.ScenarioImporter
{
    /// <summary>
    /// Struct representing a specific event that occurs during the scenario.
    /// This includes the event's name and associated arguments.
    /// This struct is used to add dynamic elements to dialogues and scenarios.
    /// </summary>
    [System.Serializable]
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("Scenario.MessageEvent")]
#endif
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("Utj.ScenarioImporter.DialogueEvent")]
#endif
    public struct DialogueEvent
    {
        /// <summary>
        /// Name of the event
        /// </summary>
        public string EventName;

        /// <summary>
        /// Arguments for the event
        /// </summary>
        public string[] EventArgs;
    }
}