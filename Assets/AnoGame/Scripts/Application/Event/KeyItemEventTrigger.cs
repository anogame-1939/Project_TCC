using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // Add this line
using AnoGame.Data;
using VContainer;
using AnoGame.Domain.Event.Services;

// イベントの基底クラス
public abstract class EventTriggerBase : MonoBehaviour
{
    [SerializeField] protected EventData eventData;
    [SerializeField] protected UnityEvent onEventTriggered;

    [Inject] protected IEventStateService _eventStateService;

    [Inject]
    public virtual void Construct(IEventStateService eventStateService)
    {
        _eventStateService = eventStateService;
        
        // イベントがクリア済みの場合は非アクティブにする
        if (_eventStateService.IsEventCleared(eventData.EventId))
        {
            gameObject.SetActive(false);
        }
    }

    public void CompleteEvent()
    {
        _eventStateService.SetEventCleared(eventData.EventId);
        gameObject.SetActive(false);
    }

    protected virtual void TriggerEvent()
    {
        if (!_eventStateService.IsEventCleared(eventData.EventId))
        {
            onEventTriggered?.Invoke();
        }
    }
}
