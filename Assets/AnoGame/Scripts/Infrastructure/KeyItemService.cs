using System.Collections.Generic;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Domain.Item.Models;

namespace AnoGame.Infrastructure.Services
{
    public class KeyItemService : IKeyItemService
    {
		private readonly HashSet<string> _keyItems = new();
		private readonly IEventService _eventService;

		public KeyItemService(IItem[] keyItems, IEventService eventService)
		{
			_eventService = eventService;
			foreach (var item in keyItems)
			{
				_keyItems.Add(item.ItemName);
			}
		}

		public void RestoreKeyItemStates(IEnumerable<string> collectedKeyItems)
		{
			foreach (var itemName in collectedKeyItems)
			{
				if (IsKeyItem(itemName))
				{
					// 各キーアイテムに対してイベントを発火
					_eventService.TriggerKeyItemEvent(itemName);
				}
			}
		}

        public bool IsKeyItem(string itemName)
        {
            return _keyItems.Contains(itemName);
        }

        public bool IsKeyItem(IItem item)
        {
            return item != null && _keyItems.Contains(item.ItemName);
        }
		
    }
}

