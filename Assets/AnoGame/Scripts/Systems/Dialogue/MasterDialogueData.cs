using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Systems.Dialogue
{
    [System.Serializable]
    public class ActorDefinition
    {
        public string Name;
        public Color Color = Color.white;
    }

    public enum ColorTheme
    {
        Pastel,
        Cool,
        Vibrant
    }

    [CreateAssetMenu(fileName = "MasterDialogueData", menuName = "AnoGame/Dialogue/Master Dialogue Data")]
    public class MasterDialogueData : ScriptableObject
    {
        [Tooltip("List of all conversation units in the game.")]
        public List<ConversationUnit> Conversations = new List<ConversationUnit>();

        [Tooltip("Legacy list of actors.")]
        [HideInInspector]
        public List<string> ActorList = new List<string>();

        [Tooltip("List of available actors with colors.")]
        public List<ActorDefinition> ActorDefinitions = new List<ActorDefinition>();

        [Tooltip("Color Theme for Actors")]
        public ColorTheme Theme = ColorTheme.Pastel;

        public static readonly Color[] PastelPalette = new Color[]
        {
            new Color(1f, 0.8f, 0.8f), // Pastel Red
            new Color(1f, 0.9f, 0.8f), // Pastel Orange
            new Color(1f, 1f, 0.8f),   // Pastel Yellow
            new Color(0.8f, 1f, 0.8f), // Pastel Green
            new Color(0.8f, 1f, 0.9f), // Pastel Mint
            new Color(0.8f, 1f, 1f),   // Pastel Cyan
            new Color(0.8f, 0.9f, 1f), // Pastel Blue
            new Color(0.8f, 0.8f, 1f), // Pastel Indigo
            new Color(0.9f, 0.8f, 1f), // Pastel Purple
            new Color(1f, 0.8f, 1f),   // Pastel Magenta
            new Color(1f, 0.8f, 0.9f), // Pastel Pink
            new Color(0.9f, 0.9f, 0.9f)// Pastel Gray
        };

        public static readonly Color[] CoolPalette = new Color[]
        {
            new Color(0f, 1f, 1f),     // Cyan
            new Color(0f, 0.8f, 1f),   // Deep Sky Blue
            new Color(0f, 0.5f, 1f),   // Dodger Blue
            new Color(0f, 0.2f, 1f),   // Blue
            new Color(0.3f, 0f, 1f),   // Electric Indigo
            new Color(0.5f, 0f, 1f),   // Violet
            new Color(0.7f, 0f, 1f),   // Purple
            new Color(0f, 1f, 0.8f),   // Turquoise
            new Color(0f, 0.8f, 0.8f), // Teal
            new Color(0.4f, 0.5f, 0.7f), // Cool Grey
            new Color(0.2f, 0.2f, 0.4f), // Midnight
            new Color(0.8f, 0.8f, 1f)  // Periwinkle
        };

        public static readonly Color[] VibrantPalette = new Color[]
        {
            Color.red,
            new Color(1f, 0.5f, 0f), // Orange
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            Color.magenta,
            new Color(1f, 0f, 0.5f), // Hot Pink
            new Color(0.5f, 0f, 1f), // Purple
            new Color(0f, 1f, 0.5f), // Spring Green
            Color.white,
            Color.gray
        };

        public static Color[] GetPalette(ColorTheme theme)
        {
            switch (theme)
            {
                case ColorTheme.Cool: return CoolPalette;
                case ColorTheme.Vibrant: return VibrantPalette;
                default: return PastelPalette;
            }
        }

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

        public Color GetActorColor(string name)
        {
            if (string.IsNullOrEmpty(name)) return Color.white;
            var actor = ActorDefinitions.Find(a => a.Name == name);
            return actor != null ? actor.Color : Color.white;
        }
    }
}
