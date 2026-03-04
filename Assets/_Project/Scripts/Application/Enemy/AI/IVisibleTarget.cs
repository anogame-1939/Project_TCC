using UnityEngine;
using System;

namespace AnoGame.Application.Enemy.AI
{
    public interface IVisibleTarget
    {
        Transform GetTransform();
        bool IsHidden { get; }
        IObservable<bool> IsHiddenObservable { get; }
    }
}
