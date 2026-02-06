// AnoGame.Application.Player.Interaction
using System.Collections.Generic;
using System.Threading;
using AnoGame.Application.Player.Control; // EventLockControl
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Unity.TinyCharacterController.Interfaces.Components;

namespace AnoGame.Application.Player.Interaction
{
    [AddComponentMenu("AnoGame/Event Zone (Path->Invoke)")]
    [RequireComponent(typeof(BoxCollider))]
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

        [Header("イベント（移動前）")]
        [SerializeField] private UnityEvent onPrepareBegin;
        [SerializeField] private UnityEvent onPrepare;           // 毎フレーム
        [SerializeField] private UnityEvent onPrepareComplete;

        [Header("イベント（到達後）")]
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
                Debug.Log($"[EventZone] {name} triggered by {actor.name}");
                // path 無し or 全て null → ただちに到達イベントのみ
                if (!HasValidPath(approachPath))
                {
                    FireEnter();
                    return;
                }

                Debug.Log($"[EventZone] {name} preparing move for {actor.name}");

                var el = FindEventLock(actor);
                if (el == null)
                {
                    // 安全策：ロック不可なら即時到達扱い
                    FireEnter();
                    return;
                }
                Debug.Log($"[EventZone] {name} preparing move for {actor.name}");

                // プリペア開始
                onPrepareBegin?.Invoke();

                // ---- 移動制御開始 ----
                el.BeginLock();
                el.LookFaceMove();
                Debug.Log($"[EventZone] {name} preparing move for {actor.name}");

                foreach (var p in approachPath)
                {
                    if (p == null) continue;

                    el.MoveToPoint(p.position, moveSpeed, stopDistance);
                    await WaitArriveAsync(actor, p.position, ct, onPrepare);
                    if (ct.IsCancellationRequested) return;
                }
                Debug.Log($"[EventZone] {name} preparing move for {actor.name}");

                // プリペア完了
                onPrepareComplete?.Invoke();
                Debug.Log($"[EventZone] {name} preparing move for {actor.name}");

                // 到達後イベント
                FireEnter();

                Debug.Log($"[EventZone] {name} preparing move for {actor.name}");

                // 例：向きの確定など必要ならここで
                var brain = actor.GetComponent<IBrain>();
                if (brain != null)
                {
                    var yaw = actor.transform.eulerAngles.y;
                    // brain.SetYawAngle(yaw); // 実装に合わせて
                }

                // el.EndLock();
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

        private async UniTask WaitArriveAsync(Transform actor, Vector3 dest, CancellationToken ct, UnityEvent onPrepareTick)
        {
            var sq = Mathf.Max(0.0001f, stopDistance * stopDistance);
            var destFlat = dest; destFlat.y = 0f;

            while (!ct.IsCancellationRequested)
            {
                // プリペア（毎フレーム）
                onPrepareTick?.Invoke();

                var p = actor.position;
                p.y = 0f;
                if ((p - destFlat).sqrMagnitude <= sq) break;

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
