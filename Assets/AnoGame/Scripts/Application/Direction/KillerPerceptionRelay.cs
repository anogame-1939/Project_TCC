// AnoGame.Application.Direction
using System;
using AnoGame.Application.Player.Interaction;
using AnoGame.Application.Player.Perception;
using UniRx;
using UnityEngine;

namespace AnoGame.Application.Direction
{
    public sealed class KillerPerceptionRelay : MonoBehaviour
    {
        [SerializeField] EncounterDirector director;
        [SerializeField, Min(0f)] float reacquireWindow = 0.6f;

        private Vector3 _lastSeen;
        private bool _maybeLost;
        private CompositeDisposable _cd;

        void OnEnable()
        {
            _cd = new CompositeDisposable();

            // 1) ハイド開始要求 -> 見失い猶予タイマ開始
            MessageBroker.Default
                .Receive<HideRequested>()
                .Subscribe(msg =>
                {
                    var actor = msg.Actor;
                    // 最後に見た座標：Actor優先、無ければSpot
                    _lastSeen = actor ? actor.position
                                      : (msg.Spot ? msg.Spot.transform.position : _lastSeen);

                    _maybeLost = true;

                    // 同一Actorの露見が来たらキャンセル
                    MessageBroker.Default
                        .Receive<PlayerRevealed>()
                        .Where(ev => ev.Actor == actor)
                        .Take(1)
                        .Subscribe(_ => _maybeLost = false)
                        .AddTo(_cd);

                    // 猶予タイマ
                    Observable.Timer(TimeSpan.FromSeconds(reacquireWindow))
                        .TakeUntil(MessageBroker.Default
                            .Receive<PlayerRevealed>()
                            .Where(ev => ev.Actor == actor))
                        .Subscribe(_ =>
                        {
                            if (_maybeLost)
                            {
                                _maybeLost = false;
                                director.NotifyLost(_lastSeen);
                            }
                        })
                        .AddTo(_cd);
                })
                .AddTo(_cd);

            // 2) ハイド終了/キャンセル -> 露見イベントにブリッジ
            MessageBroker.Default
                .Receive<HideExited>()
                .Subscribe(msg =>
                {
                    var actor = msg.Actor;
                    MessageBroker.Default.Publish(PlayerRevealed.From(actor));
                    director.NotifyFound(); // すぐ追跡に戻したい場合
                })
                .AddTo(_cd);

            MessageBroker.Default
                .Receive<HideCanceled>()
                .Subscribe(msg =>
                {
                    var actor = msg.Actor;
                    MessageBroker.Default.Publish(PlayerRevealed.From(actor));
                    director.NotifyFound();
                })
                .AddTo(_cd);

            // 3) （任意）視覚システムが別途 PlayerRevealed を発行してもOK
            // MessageBroker.Default.Receive<PlayerSighted>() で NotifyFound しても良い
        }

        void OnDisable()
        {
            _cd?.Dispose();
            _cd = null;
        }
    }
}
