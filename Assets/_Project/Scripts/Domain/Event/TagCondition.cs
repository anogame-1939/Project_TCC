using AnoGame.Domain.Event.Services;

namespace AnoGame.Domain.Event.Conditions
{
    /// <summary>
    /// タグベースの条件チェック（conditionTags用）
    /// EventTriggerBase の条件リストで使用する
    /// </summary>
    public class TagCondition : IEventCondition
    {
        private readonly IEventService _eventService;
        private readonly string _tag;

        public TagCondition(IEventService eventService, string tag)
        {
            _eventService = eventService;
            _tag = tag;
        }

        public bool IsSatisfied()
        {
            if (_tag.StartsWith("!"))
            {
                return !_eventService.HasTag(_tag.Substring(1));
            }
            return _eventService.HasTag(_tag);
        }
    }
}
