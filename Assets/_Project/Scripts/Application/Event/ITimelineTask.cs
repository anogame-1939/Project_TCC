using System;

namespace AnoGame.AnoFlow
{
    public interface ITimelineTask
    {
        bool IsSkipAvailable { get; }
        Action OnCompleted { get; set; }
        void Play();
    }
}
