// AnoGame.Application.Player.Perception
using UnityEngine;

namespace AnoGame.Application.Player.Perception
{
    /// <summary>プレイヤーが可視状態になった（隠れるのをやめた / 発見された 等）</summary>
    public struct PlayerRevealed
    {
        public Transform Actor;
        public Vector3 WorldPos;

        public PlayerRevealed(Transform actor, Vector3 worldPos)
        { Actor = actor; WorldPos = worldPos; }

        public static PlayerRevealed From(Transform actor)
            => new PlayerRevealed(actor, actor ? actor.position : Vector3.zero);
    }
}
