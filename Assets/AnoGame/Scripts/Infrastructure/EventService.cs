using System;
using System.Linq;
using System.Collections.Generic;
using AnoGame.Domain.Event.Services;

namespace AnoGame.Infrastructure.Services.Inventory
{
    /// <summary>
    /// イベントIDに対応するアクションを登録するサービス
    /// イベントの開始時、終了時のイベントを発火するだけ
    /// </summary>
    public class EventService : IEventService
    {
        private readonly Dictionary<string, List<Action>> _startEventHandlers = new();
        private readonly Dictionary<string, List<Action>> _cpmpleteEventHandlers = new();
        private readonly Dictionary<string, List<Action>> _failedEventHandlers = new();
        
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

        public void RegisterFailedEventHandler(string eventID, Action handler)
        {
            if (!_failedEventHandlers.ContainsKey(eventID))
            {
                _failedEventHandlers[eventID] = new List<Action>();
            }
            _failedEventHandlers[eventID].Add(handler);
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

        public void UnregisterFailedEventHandler(string eventID, Action handler)
        {
            if (_failedEventHandlers.ContainsKey(eventID))
            {
                _failedEventHandlers[eventID].Remove(handler);
            }
        }

        public void TriggerEventStart(string eventID)
        {
            if (_startEventHandlers.TryGetValue(eventID, out var handlers))
            {
                foreach (var handler in handlers.ToList()) // ToList()で反復中の変更を防ぐ
                {
                    try
                    {
                        handler.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // TODO: 適切なログ処理やエラーハンドリングを実装
                        // 一つのハンドラーの失敗が他のハンドラーの実行を妨げないようにする
                        Console.WriteLine($"Error executing start event handler for eventID: {eventID}. Error: {ex.Message}");
                    }
                }
            }
        }

        public void TriggerEventComplete(string eventID)
        {
            if (_cpmpleteEventHandlers.TryGetValue(eventID, out var handlers))
            {
                foreach (var handler in handlers.ToList()) // ToList()で反復中の変更を防ぐ
                {
                    try
                    {
                        handler.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // TODO: 適切なログ処理やエラーハンドリングを実装
                        // 一つのハンドラーの失敗が他のハンドラーの実行を妨げないようにする
                        Console.WriteLine($"Error executing complete event handler for eventID: {eventID}. Error: {ex.Message}");
                    }
                }
            }
        }

        public void TriggerEventFailed(string eventID)
        {
            if (_failedEventHandlers.TryGetValue(eventID, out var handlers))
            {
                foreach (var handler in handlers.ToList()) // ToList()で反復中の変更を防ぐ
                {
                    try
                    {
                        handler.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // TODO: 適切なログ処理やエラーハンドリングを実装
                        // 一つのハンドラーの失敗が他のハンドラーの実行を妨げないようにする
                        Console.WriteLine($"Error executing complete event handler for eventID: {eventID}. Error: {ex.Message}");
                    }
                }
            }
        }



    }
}

