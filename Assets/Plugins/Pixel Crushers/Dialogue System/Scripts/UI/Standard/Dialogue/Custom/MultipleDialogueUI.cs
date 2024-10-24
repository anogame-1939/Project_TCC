using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{
    /// <summary>
    /// This subclass of StandardDialogueUI checks conversations for a custom
    /// Boolean field named "Overlay". If it's true, it tells participating
    /// Dialogue Actors to use the default subtitle panel instead of their
    /// custom overhead bubble subtitle panels.
    /// </summary>
    public class MultipleDialogueUI : StandardDialogueUI
    {
        private Dictionary<DialogueActor, SubtitlePanelNumber> dialogueActors = new Dictionary<DialogueActor, SubtitlePanelNumber>();

        public override void Open()
        {
            // Check the conversation. If it's set to use overlay, set actors to Default panel:
            dialogueActors.Clear();
            var conversation = DialogueManager.MasterDatabase.GetConversation(DialogueManager.lastConversationStarted);
            if (conversation != null)
            {
                if (conversation.LookupBool("Overlay"))
                {
                    SetActorToDefaultPanel(DialogueActor.GetDialogueActorComponent(DialogueManager.currentActor));
                    SetActorToDefaultPanel(DialogueActor.GetDialogueActorComponent(DialogueManager.currentConversant));
                    for (int i = 0; i < conversation.dialogueEntries.Count; i++)
                    {
                        SetActorToDefaultPanel(conversation.dialogueEntries[i].ActorID);
                    }
                }
            }

            base.Open();
        }

        public override void Close()
        {
            base.Close();

            // Restore DialogueActors' subtitle panel settings:
            foreach (KeyValuePair<DialogueActor, SubtitlePanelNumber> kvp in dialogueActors)
            {
                kvp.Key.standardDialogueUISettings.subtitlePanelNumber = kvp.Value;
            }
        }

        protected void SetActorToDefaultPanel(DialogueActor dialogueActor)
        {
            if (dialogueActor != null && !dialogueActors.ContainsKey(dialogueActor))
            {
                dialogueActors[dialogueActor] = dialogueActor.standardDialogueUISettings.subtitlePanelNumber;
                dialogueActor.standardDialogueUISettings.subtitlePanelNumber = SubtitlePanelNumber.Default;
            }
        }

        protected void SetActorToDefaultPanel(int actorID)
        {
            var actor = DialogueManager.masterDatabase.GetActor(actorID);
            if (actor != null)
            {
                SetActorToDefaultPanel(DialogueActor.GetDialogueActorComponent(CharacterInfo.GetRegisteredActorTransform(actor.Name)));
            }
        }
    }
}