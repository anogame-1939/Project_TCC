using UnityEngine;
using VContainer;
using AnoGame.Application.Player.Control; // EventLockControl

[DisallowMultipleComponent]
public sealed class TimelineEventLockProxy : MonoBehaviour
{
    [Inject] public EventLockControl _playerCtrl { get; private set; }


    [Inject] public void Construct(EventLockControl playerCtrl /* GameScope で登録済み */)
    {
        _playerCtrl = playerCtrl;
    }

    // タイムラインから叩きたい薄いAPI（必要に応じて拡張）
    public void BeginLock() => _playerCtrl?.BeginLock();
    public void EndLock()   => _playerCtrl?.EndLock();

    public void MoveTo(Transform target, float speed, float stopDistance)
    {
        if (_playerCtrl == null || target == null) return;
        _playerCtrl.MoveToPoint(target.position, speed, stopDistance);
    }


}
