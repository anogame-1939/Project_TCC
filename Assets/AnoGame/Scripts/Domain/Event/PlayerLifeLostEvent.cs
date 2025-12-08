namespace AnoGame.Domain.Event
{
    public struct PlayerLifeLostEvent
    {
        public int CurrentLives { get; }
        public int MaxLives { get; }
        public bool IsGameOver => CurrentLives <= 0;

        public PlayerLifeLostEvent(int currentLives, int maxLives)
        {
            CurrentLives = currentLives;
            MaxLives = maxLives;
        }
    }
}
