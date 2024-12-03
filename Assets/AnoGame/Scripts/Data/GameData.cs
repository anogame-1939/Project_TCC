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
        public PlayerPositionData playerPosition; // 位置情報を追加
    }

    [Serializable]
    public class PlayerPositionData
    {
        // 基本的な位置情報
        public Vector3SerializableData position;
        public QuaternionSerializableData rotation;
        
        // 現在のマップ/エリア情報
        public string currentMapId;
        public string currentAreaId;
        
        // 最後にセーブされたセーフポイント情報
        public string lastCheckpointId;
        public Vector3SerializableData lastCheckpointPosition;
        
        // リスポーン位置（必要な場合）
        public Vector3SerializableData respawnPosition;

        // 位置情報が設定されているかを確認するプロパティ
        public bool HasPosition => position != null;
        
        // 回転情報が設定されているかを確認するプロパティ
        public bool HasRotation => rotation != null;
        
        // 位置と回転の両方が設定されているかを確認するプロパティ
        public bool IsPositionValid => HasPosition && HasRotation;

        // デフォルト値を設定するメソッド
        public void SetDefaultPosition(Vector3 defaultPosition, Quaternion defaultRotation)
        {
            position = new Vector3SerializableData(defaultPosition);
            rotation = new QuaternionSerializableData(defaultRotation);
        }
    }

    // Vector3をシリアル化可能な形式に変換するためのクラス
    [Serializable]
    public class Vector3SerializableData
    {
        public float x;
        public float y;
        public float z;

        public Vector3SerializableData() { }

        public Vector3SerializableData(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public static Vector3SerializableData FromVector3(Vector3 vector)
        {
            return new Vector3SerializableData(vector);
        }
    }

    [Serializable]
    public class QuaternionSerializableData
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public QuaternionSerializableData() { }

        public QuaternionSerializableData(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }

        public static QuaternionSerializableData FromQuaternion(Quaternion quaternion)
        {
            return new QuaternionSerializableData(quaternion);
        }
    }

    // 既存のクラスはそのまま
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
        // public Sprite itemImage;
    }
}