using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Data
{
    [Serializable]
    public class GameData
    {
        public int score;
        public string playerName;
        public StoryProgress storyProgress;
        public List<InventoryItem> inventory;
    }

    [Serializable]
    public class StoryProgress
    {
        public int currentStoryIndex;
        public int currentChapterIndex;
        public int currentSceneIndex;
    }

    [Serializable]
    public class InventoryItem
    {
        public string itemName;
        public int quantity;
        public string description;
        public Sprite itemImage;
    }
}
