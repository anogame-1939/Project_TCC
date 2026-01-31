using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

namespace AnoGame.Systems.Dialogue.Editor
{
    public class PixelCrushersConverter : EditorWindow
    {
        private DialogueDatabase sourceDatabase;
        private MasterDialogueData targetData;

        [MenuItem("AnoGame/Dialogue System/Open Converter")]
        public static void ShowWindow()
        {
            GetWindow<PixelCrushersConverter>("Pixel Crushers Converter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Pixel Crushers to AnoGame Converter", EditorStyles.boldLabel);

            sourceDatabase = (DialogueDatabase)EditorGUILayout.ObjectField("Source Database", sourceDatabase, typeof(DialogueDatabase), false);
            targetData = (MasterDialogueData)EditorGUILayout.ObjectField("Target Data", targetData, typeof(MasterDialogueData), false);

            if (GUILayout.Button("Convert"))
            {
                if (sourceDatabase == null || targetData == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign both Source Database and Target Data.", "OK");
                    return;
                }

                ConvertDatabase();
            }
        }

        private void ConvertDatabase()
        {
            targetData.Conversations.Clear();
            int count = 0;

            foreach (var conversation in sourceDatabase.conversations)
            {
                string conversationTitle = conversation.Title;

                // Parse Title to guess Chapter/Section (Format: Chapter/Section/...)
                // We assume many users use folder-like structures in Pixel Crushers
                string[] parts = conversationTitle.Split(new char[] { '/', '_' }, System.StringSplitOptions.RemoveEmptyEntries);
                string chapter = "Default";
                string section = conversationTitle;

                if (parts.Length >= 2)
                {
                    chapter = parts[0];
                    section = parts[1];
                }
                else if (parts.Length == 1)
                {
                    section = parts[0];
                }

                // Pixel Crushers often has a "START" node (ID 0) which is empty. 
                // We typically want to skip it unless it has text, but usually links flow FROM it.
                // We simply map all nodes and let the ID linking resolve the flow.

                // First pass: Create units for all entries
                foreach (var entry in conversation.dialogueEntries)
                {
                    // Skip 'Group' nodes if they are just organizational, but keep them if they route logic on.
                    // For simplicity, we convert everything to preserve structure.

                    ConversationUnit unit = new ConversationUnit();
                    unit.ID = GenerateID(conversationTitle, entry.id);
                    unit.ChapterID = chapter;
                    unit.SectionID = section;

                    // Get Speaker Name
                    var actor = sourceDatabase.GetActor(entry.ActorID);
                    unit.SpeakerName = actor != null ? actor.Name : "Unknown";

                    unit.BodyText = entry.DialogueText;

                    // Handle Links (NextID or Choices)
                    if (entry.outgoingLinks != null && entry.outgoingLinks.Count > 0)
                    {
                        // Pixel Crushers assumes links go to entries in the SAME conversation usually.
                        // Cross-conversation links exist but are rarer.

                        if (entry.outgoingLinks.Count == 1)
                        {
                            // Single link -> NextID
                            var link = entry.outgoingLinks[0];
                            string targetTitle = conversationTitle; // Assume same conversation

                            if (link.destinationConversationID != conversation.id)
                            {
                                // Link to another conversation
                                var targetConv = sourceDatabase.GetConversation(link.destinationConversationID);
                                if (targetConv != null) targetTitle = targetConv.Title;
                            }

                            unit.NextID = GenerateID(targetTitle, link.destinationDialogueID);
                        }
                        else
                        {
                            // Multiple links -> Choices
                            foreach (var link in entry.outgoingLinks)
                            {
                                Choice choice = new Choice();

                                // Need to find the target entry to get its text (often menu text)
                                string targetTitle = conversationTitle;
                                var targetConv = conversation;

                                if (link.destinationConversationID != conversation.id)
                                {
                                    targetConv = sourceDatabase.GetConversation(link.destinationConversationID);
                                    targetTitle = targetConv?.Title;
                                }

                                var targetEntry = targetConv?.GetDialogueEntry(link.destinationDialogueID);

                                if (targetEntry != null)
                                {
                                    // Use MenuText if available, otherwise DialogueText
                                    choice.ChoiceText = !string.IsNullOrEmpty(targetEntry.MenuText) ? targetEntry.MenuText : targetEntry.DialogueText;

                                    // If even that is empty, it might be a continue? Use "Next" or similar fallback?
                                    if (string.IsNullOrEmpty(choice.ChoiceText)) choice.ChoiceText = "...";
                                }

                                choice.TargetID = GenerateID(targetTitle, link.destinationDialogueID);
                                unit.Choices.Add(choice);
                            }
                        }
                    }

                    targetData.Conversations.Add(unit);
                    count++;
                }
            }

            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"Converted {count} dialogue entries successfully!", "OK");
        }

        private string GenerateID(string conversationTitle, int entryID)
        {
            // Sanitize title to be ID friendly
            string safeTitle = conversationTitle.Replace(" ", "_").Replace("/", "_").Replace("-", "_");
            return $"{safeTitle}_{entryID}";
        }
    }
}
