// AnoGame.Application.Player.Interaction
using System.Threading;
using AnoGame.Application.Player.Control;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace AnoGame.Application.Player.Interaction
{
    public struct HideRequested
    {
        public Transform Actor;
        public HideSpotZone Spot;
        public HideRequested(Transform actor, HideSpotZone spot) { Actor = actor; Spot = spot; }
    }
    
    public struct HideBegan { public Transform Actor; public HideSpotZone Spot; public HideBegan(Transform a, HideSpotZone s) { Actor = a; Spot = s; } }
    public struct HideExited  { public Transform Actor; public HideSpotZone Spot; public HideExited(Transform a, HideSpotZone s){ Actor=a; Spot=s; } }
    public struct HideCanceled{ public Transform Actor; public HideSpotZone Spot; public HideCanceled(Transform a, HideSpotZone s){ Actor=a; Spot=s; } }

    public class HideSpotZone : InteractableZone
    {
        [Header("UI")]
        [SerializeField] private string prompt = "隠れる";
        [SerializeField] private int priority = 900;

        [Header("Path / Points")]
        [Tooltip("入口→隠れ位置までの経路。空なら hidePoint へ直行")]
        [SerializeField] private Transform[] approachPath;
        [Tooltip("隠れる最終位置（必須）")]
        [SerializeField] private Transform hidePoint;
        [Tooltip("退出位置（任意。未設定なら hidePoint 近傍に留める）")]
        [SerializeField] private Transform exitPoint;

        [Header("Move Params (EventLockControl 用)")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.0f;
        [SerializeField, Min(0.0f)] private float stopDistance = 0.05f;
        [SerializeField, Min(0.0f)] private float validDistance = 3.0f; // 継続許容距離（無効化判定）

        [Header("Hooks (任意)")]
        public System.Action<Transform> OnBeginApproach; // 入り始め（入力ロック/レタボ等）
        public System.Action<Transform> OnEnterHidden;   // 隠れ状態に入った
        public System.Action<Transform> OnExitHidden;    // 退出（演出に合わせて呼びたい）
        public System.Action<Transform> OnCanceled;      // 中断

        // 既存：任意の外部接続フック
        public System.Action<Transform> EnterHide;

        private Transform _occupant; // 占有者（1人用スポット）

        // ===== 入口：オプション提示 =====
        public override bool TryBuildOptions(Transform actor, System.Collections.Generic.List<InteractionOption> buffer)
        {
            if (!InDistance(actor) || !InAngle(actor) || !HasLoS(actor)) return false;

            buffer.Add(new InteractionOption
            {
                Kind = InteractionKind.Hide,
                Prompt = prompt,
                Priority = priority,
                RequiresHold = false,
                Execute = () =>
                {
                    EnterHide?.Invoke(actor); // 任意
                    MessageBroker.Default.Publish(new HideRequested(actor, this));
                }
            });
            return true;
        }

        // ===== セッションから呼ばれるAPI =====

        public bool TryReserve(Transform actor)
        {
            if (_occupant != null && _occupant != actor) return false;
            _occupant = actor;
            return true;
        }

        public void Release(Transform actor)
        {
            if (_occupant == actor) _occupant = null;

            // 念のためロック解除（他の演出で既にOFFにしていれば無害）
            var el = FindEventLock(actor);
            if (el != null) el.EndLock();
        }

        public bool StillValidFor(Transform actor)
        {
            if (actor == null || hidePoint == null) return false;
            if (_occupant != null && _occupant != actor) return false;

            // 隠れ位置から離れ過ぎたら無効（必要ならLoS/フラグも）
            var dist = Vector3.Distance(actor.position, hidePoint.position);
            return dist <= Mathf.Max(validDistance, stopDistance * 2f);
        }

        public async UniTask MoveIntoAsync(Transform actor, CancellationToken ct)
        {
            var el = RequireEventLock(actor);

            // ロック開始＆移動は FaceMove で
            el.BeginLock();
            el.LookFaceMove();

            OnBeginApproach?.Invoke(actor);

            // 1) 経路があれば順に移動
            if (approachPath != null && approachPath.Length > 0)
            {
                for (int i = 0; i < approachPath.Length; i++)
                {
                    var p = approachPath[i];
                    if (p == null) continue;
                    el.MoveToPoint(p.position, moveSpeed, stopDistance);
                    await WaitArriveAsync(actor, p.position, ct);
                }
            }

            // 2) 最終的に隠れ位置へ
            if (hidePoint != null)
            {
                el.MoveToPoint(hidePoint.position, moveSpeed, stopDistance);
                await WaitArriveAsync(actor, hidePoint.position, ct);

                if (exitPoint != null)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    el.LookAt(exitPoint);
                }
            }
        }

        public async UniTask EnterHideAsync(Transform actor, CancellationToken ct)
        {
            var el = RequireEventLock(actor);

            // その場で静止＆向き固定（必要に応じて LookAt も可）
            el.Freeze();
            // el.LookKeep();

            OnEnterHidden?.Invoke(actor);
            MessageBroker.Default.Publish(new HideBegan(actor, this));

            // 必要なら1フレ待ち
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        public async UniTask ExitHideAsync(Transform actor, CancellationToken ct)
        {
            var el = RequireEventLock(actor);

            OnExitHidden?.Invoke(actor);

            // 出口指定があればそこへ
            if (exitPoint != null)
            {
                el.LookFaceMove();
                el.MoveToPoint(exitPoint.position, moveSpeed, stopDistance);
                await WaitArriveAsync(actor, exitPoint.position, ct);
            }

            // ロック解除して完了
            el.EndLock();

            MessageBroker.Default.Publish(new HideExited(actor, this));
        }

        public async UniTask CancelHideAsync(Transform actor, CancellationToken ct)
        {
            var el = FindEventLock(actor);
            OnCanceled?.Invoke(actor);

            // 状況に応じて少しだけ戻す/微演出を入れる場合はここに
            // ここでは即解除のみ
            if (el != null) el.EndLock();

            MessageBroker.Default.Publish(new HideCanceled(actor, this));
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // ===== 内部：EventLockControl 検索と到着待ち =====

        private static EventLockControl RequireEventLock(Transform actor)
        {
            var el = FindEventLock(actor);
            if (el == null)
                throw new System.InvalidOperationException($"EventLockControl が {actor?.name} に見つかりません。プレイヤー側に付与してください。");
            return el;
        }

        private static EventLockControl FindEventLock(Transform actor)
        {
            if (actor == null) return null;
            // プレイヤー本体か親に付いている想定
            return actor.GetComponent<EventLockControl>() ?? actor.GetComponentInParent<EventLockControl>();
        }

        private async UniTask WaitArriveAsync(Transform actor, Vector3 dest, CancellationToken ct)
        {
            var sq = Mathf.Max(0.0001f, stopDistance * stopDistance);
            while (!ct.IsCancellationRequested)
            {
                var p = actor.position;
                p.y = 0f; dest.y = 0f;
                if ((p - dest).sqrMagnitude <= sq) break;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // approachPath の最初と最後の「null でない」要素を拾う
            Transform first = null, last = null;
            if (approachPath != null && approachPath.Length > 0)
            {
                for (int i = 0; i < approachPath.Length; i++)
                {
                    if (approachPath[i] == null) continue;
                    first ??= approachPath[i];
                    last = approachPath[i];
                }
            }

            if (first != null) exitPoint = first;
            if (last != null) hidePoint = last;
        }
#endif
    }
}
