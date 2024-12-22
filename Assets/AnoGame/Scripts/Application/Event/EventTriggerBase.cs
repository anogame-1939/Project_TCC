using UnityEngine;
using AnoGame.Data;
using VContainer;
using AnoGame.Domain.Event.Services;


// キーアイテムによってトリガーされるイベント
public class KeyItemEventTrigger : EventTriggerBase
{
    [SerializeField] private ItemData requiredKeyItem;
    
    [Inject] private IEventService _eventService;

    [Inject]
    public override void Construct(IEventStateService eventStateService)
    {
        base.Construct(eventStateService);
        
        if (gameObject.activeSelf)
        {
            _eventService.RegisterKeyItemHandler(requiredKeyItem.ItemName, OnKeyItemObtainedHandler);
        }
    }

    private void OnDestroy()
    {
        _eventService?.UnregisterKeyItemHandler(requiredKeyItem.ItemName, OnKeyItemObtainedHandler);
    }

    private void OnKeyItemObtainedHandler()
    {
        TriggerEvent();
    }
}

// 単純な実行可能イベント（トリガーコライダーなどで発火）
public class SimpleEventTrigger : EventTriggerBase
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerEvent();
        }
    }
}

// 敵の出現などに使用する一回限りのイベント
public class OneTimeEventTrigger : EventTriggerBase
{
    public void ExecuteEvent()
    {
        TriggerEvent();
        CompleteEvent(); // イベント実行後に自動的にクリア状態にする
    }
}
