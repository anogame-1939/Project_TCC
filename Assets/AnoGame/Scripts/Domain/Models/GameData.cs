using System;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.Domain.Data.Models
{
    public class GameData
    {
        public int Score { get; private set; }
        public string PlayerName { get; private set; }
        public StoryProgress StoryProgress { get; private set; }
        public Inventory Inventory { get; private set; }
        public PlayerPosition Position { get; private set; }
        public EventHistory ClearedEvents { get; private set; }

        public GameData()
        {
            
        }

        public GameData(
            int score,
            string playerName,
            StoryProgress storyProgress,
            Inventory inventory,
            PlayerPosition position,
            EventHistory clearedEvents)
        {
            Score = score;
            PlayerName = playerName;
            StoryProgress = storyProgress;
            Inventory = inventory;
            Position = position;
            ClearedEvents = clearedEvents;
        }

        public void UpdatePosition(Position3D position, Rotation3D rotation, string mapId, string areaId)
        {
            Position = Position.UpdatePosition(position, rotation, mapId, areaId);
        }

        public void AddClearedEvent(string eventId)
        {
            ClearedEvents.AddEvent(eventId);
        }
    }

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

    public class StoryProgress
    {
        public int CurrentStoryIndex { get; private set; }
        public int CurrentChapterIndex { get; private set; }
        public int CurrentSceneIndex { get; private set; }

        public StoryProgress(int storyIndex, int chapterIndex, int sceneIndex)
        {
            CurrentStoryIndex = storyIndex;
            CurrentChapterIndex = chapterIndex;
            CurrentSceneIndex = sceneIndex;
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

    public class Inventory
    {
        private readonly List<InventoryItem> _items = new();
        public IReadOnlyList<InventoryItem> Items => _items.AsReadOnly();

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

    public class InventoryItem
    {
        public string ItemName { get; }
        public int Quantity { get; private set; }
        public string Description { get; }
        public string UniqueId { get; }
        private readonly List<string> _uniqueIds = new();
        public IReadOnlyList<string> UniqueIds => _uniqueIds.AsReadOnly();

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

    public class EventHistory
    {
        private readonly HashSet<string> _clearedEvents = new();
        public IReadOnlyCollection<string> ClearedEvents => _clearedEvents;

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