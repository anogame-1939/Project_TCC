using System;
using System.Linq;
using System.Collections.Generic;
using AnoGame.Domain.Inventory.Services;

// Infrastructure層の実装
namespace AnoGame.Infrastructure.Services.Inventory
{
	public class EventService : IEventService
	{
		private readonly Dictionary<string, List<Action>> _keyItemHandlers = new();

		public void TriggerKeyItemEvent(string itemName)
		{
			if (_keyItemHandlers.TryGetValue(itemName, out var handlers))
			{
				foreach (var handler in handlers.ToList()) // ToListで実行中の登録解除に対応
				{
					handler?.Invoke();
				}
			}
		}

		public void RegisterKeyItemHandler(string itemName, Action handler)
		{
			if (!_keyItemHandlers.ContainsKey(itemName))
			{
				_keyItemHandlers[itemName] = new List<Action>();
			}
			_keyItemHandlers[itemName].Add(handler);
		}

		public void UnregisterKeyItemHandler(string itemName, Action handler)
		{
			if (_keyItemHandlers.ContainsKey(itemName))
			{
				_keyItemHandlers[itemName].Remove(handler);
			}
		}
	}
}

