namespace AnoGame.Domain.Event
{
    /// <summary>
    /// Event published when the player dies (Lives <= 0).
    /// </summary>
    public struct PlayerDeathEvent
    {
        public int CurrentLives { get; }
        public int MaxLives { get; }

        public PlayerDeathEvent(int currentLives, int maxLives)
        {
            CurrentLives = currentLives;
            MaxLives = maxLives;
        }
    }
}
