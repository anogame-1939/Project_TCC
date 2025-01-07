using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AnoGame.Domain.Data.Models
{
    [Serializable]
    public class GameData
    {
        [JsonProperty]
        public int Score { get; private set; }
        [JsonProperty]
        public string PlayerName { get; private set; }
        [JsonProperty]
        public StoryProgress StoryProgress { get; private set; }
        [JsonProperty]
        public Inventory Inventory { get; private set; }
        [JsonProperty]
        public PlayerPosition PlayerPosition { get; private set; }
        [JsonProperty]
        public EventHistory EventHistory { get; private set; }

        [JsonConstructor]
        public GameData()
        {
        }
        public GameData(
            int score,
            string playerName,
            StoryProgress storyProgress,
            Inventory inventory,
            PlayerPosition position,
            EventHistory eventHistory)
        {
            Score = score;
            PlayerName = playerName;
            StoryProgress = storyProgress;
            Inventory = inventory;
            PlayerPosition = position;
            EventHistory = eventHistory;
        }

        public void UpdateStoryProgress(StoryProgress storyProgress)
        {
            StoryProgress = storyProgress;
        }

        public void UpdatePosition(Position3D position, Rotation3D rotation, string mapId, string areaId)
        {
            PlayerPosition = PlayerPosition.UpdatePosition(position, rotation, mapId, areaId);
        }

        public void AddClearedEvent(string eventId)
        {
            EventHistory.AddEvent(eventId);
        }
    }

    [Serializable]
    public class PlayerPosition
    {
        public Position3D Position { get; }
        public Rotation3D Rotation { get; }
        public string CurrentMapId { get; }
        public string CurrentAreaId { get; }
        public string LastCheckpointId { get; }
        public Position3D? LastCheckpointPosition { get; }
        public Position3D? RespawnPosition { get; }


        public PlayerPosition(
            Position3D position,
            Rotation3D rotation,
            string currentMapId,
            string currentAreaId,
            string lastCheckpointId = null,
            Position3D? lastCheckpointPosition = null,
            Position3D? respawnPosition = null)
        {
            Position = position;
            Rotation = rotation;
            CurrentMapId = currentMapId;
            CurrentAreaId = currentAreaId;
            LastCheckpointId = lastCheckpointId;
            LastCheckpointPosition = lastCheckpointPosition;
            RespawnPosition = respawnPosition;
        }

        public PlayerPosition UpdatePosition(Position3D newPosition, Rotation3D newRotation, string mapId, string areaId)
        {
            return new PlayerPosition(
                newPosition,
                newRotation,
                mapId,
                areaId,
                LastCheckpointId,
                LastCheckpointPosition,
                RespawnPosition
            );
        }

        public PlayerPosition UpdateCheckpoint(string checkpointId, Position3D checkpointPosition)
        {
            return new PlayerPosition(
                Position,
                Rotation,
                CurrentMapId,
                CurrentAreaId,
                checkpointId,
                checkpointPosition,
                RespawnPosition
            );
        }
    }

    [Serializable]
    public readonly struct Position3D
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Position3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    [Serializable]
    public readonly struct Rotation3D
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }

        public Rotation3D(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }

    [Serializable]
    public class StoryProgress
    {
        public int CurrentStoryIndex { get; private set; }
        public int CurrentChapterIndex { get; private set; }
        public int CurrentSceneIndex { get; private set; }

        public StoryProgress(int storyIndex, int chapterIndex)
        {
            CurrentStoryIndex = storyIndex;
            CurrentChapterIndex = chapterIndex;
        }

        public void AdvanceScene()
        {
            CurrentSceneIndex++;
        }

        public void AdvanceChapter()
        {
            CurrentChapterIndex++;
            CurrentSceneIndex = 0;
        }
    }

    [Serializable]
    public class Inventory
    {
        [JsonProperty("Items")]
        private readonly List<InventoryItem> _items = new();
        
        [JsonIgnore]
        public IReadOnlyList<InventoryItem> Items => _items.AsReadOnly();

        [JsonConstructor]
        public Inventory()
        {
        }

        public void AddItem(InventoryItem item)
        {
            _items.Add(item);
        }

        public void RemoveItem(string itemId)
        {
            var item = _items.FirstOrDefault(x => x.UniqueId == itemId);
            if (item != null)
            {
                _items.Remove(item);
            }
        }
    }

    [Serializable]
    public class InventoryItem
    {
        [JsonProperty]
        public string ItemName { get; private set; }
        [JsonProperty]
        public int Quantity { get; private set; }
        [JsonProperty]
        public string Description { get; private set; }
        [JsonProperty]
        public string UniqueId { get; private set; }
        
        [JsonProperty("UniqueIds")]
        private readonly List<string> _uniqueIds = new();
        
        [JsonIgnore]
        public IReadOnlyList<string> UniqueIds => _uniqueIds.AsReadOnly();

        [JsonConstructor]
        public InventoryItem()
        {
        }

        public InventoryItem(string itemName, int quantity, string description, string uniqueId)
        {
            ItemName = itemName;
            Quantity = quantity;
            Description = description;
            UniqueId = uniqueId;
        }

        public void AddQuantity(int amount)
        {
            if (amount < 0) throw new ArgumentException("Amount must be positive");
            Quantity += amount;
        }
    }

    [Serializable]
    public class EventHistory
    {
        [JsonProperty("ClearedEvents")]
        private readonly HashSet<string> _clearedEvents = new();
        
        [JsonIgnore]
        public IReadOnlyCollection<string> ClearedEvents => _clearedEvents;

        [JsonConstructor]
        public EventHistory()
        {
        }

        public void AddEvent(string eventId)
        {
            _clearedEvents.Add(eventId);
        }

        public bool HasCompleted(string eventId)
        {
            return _clearedEvents.Contains(eventId);
        }
    }
}