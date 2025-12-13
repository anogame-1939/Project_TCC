namespace AnoGame.Domain.Event
{
    /// <summary>
    /// Event published when the player takes damage but does not die (Miss).
    /// </summary>
    public struct PlayerMissEvent
    {
        public int CurrentLives { get; }
        public int MaxLives { get; }

        public PlayerMissEvent(int currentLives, int maxLives)
        {
            CurrentLives = currentLives;
            MaxLives = maxLives;
        }
    }
}
