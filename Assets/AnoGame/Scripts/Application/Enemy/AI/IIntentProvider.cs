// ================================
// 共有: MoveGoal / IIntentProvider
// ================================
using UnityEngine;

namespace AnoGame.Application.Enemy.AI
{
    public struct MoveGoal
    {
        public Vector3 Position;
        public Vector3? Facing; // 任意。回頭させたい場合のみ設定
        public static MoveGoal FromPosition(Vector3 p) => new MoveGoal { Position = p, Facing = null };
        public bool IsValid => !float.IsNaN(Position.x);
    }

    public interface IIntentProvider
    {
        int Priority { get; }       // 優先度（大きい方が強い）
        bool IsActive();            // 今フレーム有効か
        bool TryGetGoal(out MoveGoal goal); // 目標を生成（失敗なら false）
    }
}
