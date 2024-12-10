using System.Collections.Generic;
using AnoGame.Domain.Inventory.Services;

// Infrastructure層の実装
namespace AnoGame.Infrastructure.Services.Inventory
{
	public class KeyItemService : IKeyItemService
	{
		private readonly HashSet<string> _keyItems = new();

		public KeyItemService()
		{
			// キーアイテムの登録
			_keyItems.Add("RedKey");
			_keyItems.Add("BlueKey");
			// 他のキーアイテムを追加
		}

		public bool IsKeyItem(string itemName)
		{
			return _keyItems.Contains(itemName);
		}
	}
}

