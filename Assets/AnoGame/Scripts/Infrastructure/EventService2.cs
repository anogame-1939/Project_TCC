using System;
using System.Collections.Generic;
using AnoGame.Domain.Event.Services;

namespace AnoGame.Infrastructure.Services.Inventory
{
    /// <summary>
    /// イベントIDに対応するアクションを登録するサービス
    /// イベントの開始時、終了時のイベントを発火するだけ
    /// </summary>
    public class EventService2 : IEventService2
    {
        private readonly Dictionary<string, List<Action>> _startEventHandlers = new();
        private readonly Dictionary<string, List<Action>> _cpmpleteEventHandlers = new();
        
        public void RegisterStartEventHandler(string eventID, Action handler)
        {
            if (!_startEventHandlers.ContainsKey(eventID))
            {
                _startEventHandlers[eventID] = new List<Action>();
            }
            _startEventHandlers[eventID].Add(handler);
        }

        public void RegisterCompleteEventHandler(string eventID, Action handler)
        {
            if (!_cpmpleteEventHandlers.ContainsKey(eventID))
            {
                _cpmpleteEventHandlers[eventID] = new List<Action>();
            }
            _cpmpleteEventHandlers[eventID].Add(handler);
        }

        public void UnregisterStartEventHandler(string eventID, Action handler)
        {
            if (_startEventHandlers.ContainsKey(eventID))
            {
                _startEventHandlers[eventID].Remove(handler);
            }
        }

        public void UnregisterCompleteEventHandler(string eventID, Action handler)
        {
            if (_cpmpleteEventHandlers.ContainsKey(eventID))
            {
                _cpmpleteEventHandlers[eventID].Remove(handler);
            }
        }

    }
}

