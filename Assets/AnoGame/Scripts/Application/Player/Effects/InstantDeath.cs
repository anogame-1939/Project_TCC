
// Assets/AnoGame/Scripts/Application/Player/Effects/InstantDeath.cs
using System;
using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    public class InstantDeath : MonoBehaviour
    {
        public event Action OnPlayerDeath;

        public void Kill()
        {
            OnPlayerDeath?.Invoke();
        }
    }
}
