// Presentation/Inventory/ItemPickupPopupController.cs
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using AnoGame.Messages;
using AnoGame.AnoFlow;

namespace AnoGame.Application.Inventory
{
    public class ItemPickupPopupController : MonoBehaviour
    {
        [SerializeField] private ItemPickupView view;

        private CompositeDisposable _disposables;

        private void OnEnable()
        {
            _disposables = new CompositeDisposable();

            MessageBroker.Default.Receive<ItemCollected>()
                .Subscribe(en =>
                {
                    var task = new ItemPickupTask(view, en.ItemData, en.Quantity);
                    TimelineQueueManager.Instance.Enqueue(task);
                })
                .AddTo(_disposables);
        }

        private void OnDisable() => _disposables?.Dispose();
    }
}
