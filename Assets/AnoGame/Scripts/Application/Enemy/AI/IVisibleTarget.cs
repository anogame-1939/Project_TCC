using UnityEngine;

namespace AnoGame.Application.Enemy.AI
{
    public interface IVisibleTarget
    {
        Transform GetTransform();
        bool IsHidden { get; }
    }
}
