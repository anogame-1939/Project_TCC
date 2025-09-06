// AnoGame.Application.Player.Interaction
using System.Collections.Generic;
using System.Threading;
using AnoGame.Application.Player.Control; // EventLockControl
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace AnoGame.Application.Player.Interaction
{
    /// <summary>
    /// Player がトリガーに入ると、approachPath を順に辿り、到達時に onPlayerEnter を発火。
    /// - Collider は isTrigger を推奨
    /// - Player 判定は Tag で実施（既定 "Player"）
    /// - EventLockControl が Actor(=Player) にあれば移動制御を行う
    ///   無ければ path を飛ばして即時 onPlayerEnter を発火
    /// </summary>
    [AddComponentMenu("AnoGame/Event Zone (Path->Invoke)")]
    [RequireComponent(typeof(Collider))]
    public class EventZone : MonoBehaviour
    {
        [Header("判定")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool invokeOnce = false;

        [Header("Path / Move")]
        [Tooltip("入口→最終位置までの経路。null / 空なら即時 onPlayerEnter")]
        [SerializeField] private Transform[] approachPath;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.0f;
        [SerializeField, Min(0.0f)] private float stopDistance = 0.05f;

        [Header("イベント")]
        [SerializeField] private UnityEvent onPlayerEnter;
        [SerializeField] private UnityEvent onPlayerExit;

        private bool _invoked;
        private readonly HashSet<Transform> _running = new HashSet<Transform>();

        private void OnTriggerEnter(Collider other)
        {
            if (invokeOnce && _invoked) return;
            if (!other.CompareTag(playerTag)) return;

            var actor = other.transform;
            if (_running.Contains(actor)) return;

            _running.Add(actor);
            RunSequenceAsync(actor, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            onPlayerExit?.Invoke();
        }

        private async UniTask RunSequenceAsync(Transform actor, CancellationToken ct)
        {
            try
            {
                // path 無し or 全て null → ただちに発火
                if (!HasValidPath(approachPath))
                {
                    FireEnter();
                    return;
                }

                var el = FindEventLock(actor);
                if (el == null)
                {
                    // EventLockControl が無い場合は即時発火（安全策）
                    FireEnter();
                    return;
                }

                // --- EventLockControl を使って移動制御 ---
                el.BeginLock();
                el.LookFaceMove();

                foreach (var p in approachPath)
                {
                    if (p == null) continue;
                    el.MoveToPoint(p.position, moveSpeed, stopDistance);
                    await WaitArriveAsync(actor, p.position, ct);
                    if (ct.IsCancellationRequested) return;
                }

                // 到達後に発火
                FireEnter();

                // 必要ならここで el.Freeze() / el.EndLock() の順を変える
                el.EndLock();
            }
            finally
            {
                _running.Remove(actor);
            }
        }

        private void FireEnter()
        {
            if (invokeOnce) _invoked = true;
            onPlayerEnter?.Invoke();
        }

        private static bool HasValidPath(Transform[] path)
        {
            if (path == null || path.Length == 0) return false;
            for (int i = 0; i < path.Length; i++)
                if (path[i] != null) return true;
            return false;
        }

        private static EventLockControl FindEventLock(Transform actor)
        {
            if (actor == null) return null;
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
        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col) col.isTrigger = true;
        }

        private void OnValidate()
        {
            var col = GetComponent<Collider>();
            if (col && !col.isTrigger)
            {
                Debug.LogWarning($"[EventZone] {name} の Collider は isTrigger を推奨します。");
            }
        }
#endif
    }
}
