using System;
using AnoGame.Application.Event;
using AnoGame.Data;
using Cysharp.Threading.Tasks;

namespace AnoGame.Application.Inventory
{
    public class ItemPickupTask : ITimelineTask
    {
        private readonly ItemPickupView _view;
        private readonly ItemData _itemData;
        private readonly int _quantity;

        public Action OnCompleted { get; set; }

        public ItemPickupTask(ItemPickupView view, ItemData itemData, int quantity)
        {
            _view = view;
            _itemData = itemData;
            _quantity = quantity;
        }

        public void Play()
        {
            PlayAsync().Forget();
        }

        private async UniTaskVoid PlayAsync()
        {
            await _view.ShowAsync(_itemData, _quantity);
            OnCompleted?.Invoke();
        }
    }
}
