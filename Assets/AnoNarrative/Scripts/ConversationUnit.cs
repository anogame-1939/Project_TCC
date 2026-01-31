using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.AnoNarrative
{
    [Serializable]
    public class ConversationUnit
    {
        [Tooltip("Unique ID for this conversation node (e.g. S1_Intro_01)")]
        public string ID;

        [Tooltip("Episode Context (e.g. Ep1)")]
        public string EpisodeID;

        [Tooltip("Story/Chapter Context (e.g. Story1)")]
        public string ChapterID;

        [Tooltip("Section/Situation Context (e.g. Intro, BossBattle)")]
        public string SectionID;

        [Tooltip("Human readable name for the section")]
        public string SectionName;

        [Tooltip("Name of the speaker")]
        public string SpeakerName;

        [TextArea(3, 10)]
        [Tooltip("The dialogue text")]
        public string BodyText;

        [Tooltip("ID of the next conversation node. Leave empty if this is the end or if using choices.")]
        public string NextID;

        [Tooltip("Optional choices for branching dialogue")]
        public List<Choice> Choices = new List<Choice>();

        [HideInInspector]
        public Vector2 Position; // For Node Editor
    }

    [Serializable]
    public class Choice
    {
        [Tooltip("Text displayed on the choice button")]
        public string ChoiceText;

        [Tooltip("ID of the conversation node to jump to if this choice is selected")]
        public string TargetID;
    }
}
