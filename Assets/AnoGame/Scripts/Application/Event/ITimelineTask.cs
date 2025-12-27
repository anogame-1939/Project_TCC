using System;

namespace AnoGame.Application.Event
{
    public interface ITimelineTask
    {
        bool IsSkipAvailable { get; }
        Action OnCompleted { get; set; }
        void Play();
    }
}
