using System;

namespace AnoGame.Application.Event
{
    public interface ITimelineTask
    {
        Action OnCompleted { get; set; }
        void Play();
    }
}
