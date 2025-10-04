// Presentation/Inventory/ItemPickupPopupController.cs
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using AnoGame.Messages;

namespace AnoGame.Application.Inventory
{
    public class ItemPickupPopupController : MonoBehaviour
    {
        [SerializeField] private ItemPickupView view;

        private readonly Queue<ItemCollected> _queue = new();
        private CompositeDisposable _disposables;

        private void OnEnable()
        {
            _disposables = new CompositeDisposable();

            MessageBroker.Default.Receive<ItemCollected>()
                .Subscribe(en =>
                {
                    _queue.Enqueue(en);
                    TryPlayNext();
                })
                .AddTo(_disposables);
        }

        private void OnDisable() => _disposables?.Dispose();

        private void TryPlayNext()
        {
            if (view.IsBusy || _queue.Count == 0) return;
            var ev = _queue.Dequeue();
            view.MarkBusy();
            Play(ev).Forget();
        }

        private async UniTaskVoid Play(ItemCollected ev)
        {
            await view.ShowAsync(ev.ItemData, ev.Quantity);
            TryPlayNext(); // 次を再生
        }
    }
}
