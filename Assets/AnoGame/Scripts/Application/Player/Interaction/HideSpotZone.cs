// AnoGame.Application.Player.Interaction
using System.Threading;
using AnoGame.Application.Player.Control;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Events;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Application.Player.Interaction
{
    public struct HideRequested
    {
        public Transform Actor;
        public HideSpotZone Spot;
        public HideRequested(Transform actor, HideSpotZone spot) { Actor = actor; Spot = spot; }
    }

    public struct HideBegan { public Transform Actor; public HideSpotZone Spot; public HideBegan(Transform a, HideSpotZone s) { Actor = a; Spot = s; } }
    public struct HideExited { public Transform Actor; public HideSpotZone Spot; public HideExited(Transform a, HideSpotZone s) { Actor = a; Spot = s; } }
    public struct HideCanceled { public Transform Actor; public HideSpotZone Spot; public HideCanceled(Transform a, HideSpotZone s) { Actor = a; Spot = s; } }

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
        [SerializeField]
        private UnityEvent OnBeginApproach; // 入り始め（入力ロック/レタボ等）
        [SerializeField]
        private UnityEvent OnEnterHidden;   // 隠れ状態に入った
        [SerializeField]
        private UnityEvent OnExitHidden;    // 退出（演出に合わせて呼びたい）
        [SerializeField]
        private UnityEvent OnCanceled;      // 中断

        // 既存：任意の外部接続フック
        [SerializeField]
        private UnityEvent EnterHide;
        // [NEW] 退出時の分岐イベント
        [SerializeField]
        private UnityEvent OnExitFound;     // 見つかった状態で出た（あるいは出る直後に見つかった）
        [SerializeField]
        private UnityEvent OnExitSafe;      // 見つからずに出られた


        private Transform _occupant; // 占有者（1人用スポット）
        private bool _isBusy;        // 処理中フラグ（二重実行防止）

        [Header("Gizmos")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showLabels = true;
        [SerializeField] private float pointRadius = 0.08f;
        [SerializeField] private float arrowSize = 0.25f;
        [SerializeField] private Color pathColor = new(0f, 0.8f, 1f, 0.9f);   // 水色
        [SerializeField] private Color hideColor = new(0.2f, 1f, 0.2f, 1f);   // 緑
        [SerializeField] private Color exitColor = new(1f, 0.9f, 0.2f, 1f);   // 黄
        [SerializeField] private Color invalidColor = new(1f, 0.3f, 0.3f, 0.8f); // 赤

        [Header("Visual")]
        [SerializeField] private Color playerHideColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private float fadeDuration = 0.25f;

        private Color? _originalColor; // 元の色を保持

        // ===== 入口：オプション提示 =====
        public override bool TryBuildOptions(Transform actor, System.Collections.Generic.List<InteractionOption> buffer)
        {
            // 処理中は一切のインタラクションを受け付けない
            if (_isBusy) return false;

            // [NEW] UsageLimiterによってロックされている場合
            if (_forceLocked) return false;

            // 既に自分が隠れているなら「出る」を提示
            if (_occupant == actor)
            {
                buffer.Add(new InteractionOption
                {
                    Kind = InteractionKind.Hide,
                    Prompt = "出る",
                    Priority = 1000, // 隠れるより優先
                    RequiresHold = false,
                    Execute = () => ExecuteExit(actor).Forget(),
                    IsContinuous = false
                });
                return true;
            }

            if (!InDistance(actor) || !InAngle(actor) || !HasLoS(actor)) return false;

            buffer.Add(new InteractionOption
            {
                Kind = InteractionKind.Hide,
                Prompt = prompt,
                Priority = priority,
                RequiresHold = false,
                Execute = () => ExecuteHide(actor).Forget()
            });
            return true;
        }

        private async UniTaskVoid ExecuteHide(Transform actor)
        {
            if (_isBusy) return;
            if (!TryReserve(actor)) return;

            _isBusy = true;
            try
            {
                EnterHide?.Invoke(); // 任意
                MessageBroker.Default.Publish(new HideRequested(actor, this));

                var ct = this.GetCancellationTokenOnDestroy();

                // 入口→隠れる
                await MoveIntoAsync(actor, ct);
                await EnterHideAsync(actor, ct);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async UniTaskVoid ExecuteExit(Transform actor)
        {
            if (_isBusy) return;
            _isBusy = true;
            try
            {
                var ct = this.GetCancellationTokenOnDestroy();

                // 退出
                await ExitHideAsync(actor, ct);
                Release(actor);
            }
            finally
            {
                _isBusy = false;
            }
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

            // 色を戻す（念のため）
            RestoreColorInstant(actor);
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

            OnBeginApproach?.Invoke();

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

            // 色変更フェードアウト
            await FadeColorAsync(actor, playerHideColor, fadeDuration, ct);

            OnEnterHidden?.Invoke();
            MessageBroker.Default.Publish(new HideBegan(actor, this));

            // 必要なら1フレ待ち
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        public async UniTask ExitHideAsync(Transform actor, CancellationToken ct)
        {
            var el = RequireEventLock(actor);

            OnExitHidden?.Invoke();

            // 色復帰フェードイン（移動開始と同時に行うか、完了してからか…ここでは並列で良さそうだが、移動中に戻るのが自然）
            // 移動と並列にフェードしたいので、Forgetせずにawaitしない...いや、
            // MoveToPointしてる間にフェードしたい。
            // 簡易的に、移動開始前にフェード開始して、移動メソッドを呼ぶ。
            var fadeTask = RestoreColorAsync(actor, fadeDuration, ct);

            Debug.Log("退出開始");
            // 出口指定があればそこへ
            if (exitPoint != null)
            {
                Debug.Log("出口へ移動");

                el.LookFaceMove();
                el.MoveToPoint(exitPoint.position, moveSpeed, stopDistance);
                await WaitArriveAsync(actor, exitPoint.position, ct);
            }
            Debug.Log("退出完了");

            // フェード完了待ち（もし移動より長ければ）
            await fadeTask;

            // ロック解除して完了
            el.EndLock();

            Debug.Log("退出完了2");

            MessageBroker.Default.Publish(new HideExited(actor, this));

            // [NEW] 退出後の状況判定
            // 1フレーム待って、敵のリアクション（Vision -> Coordinator）が回るのを待つ
            await UniTask.Yield(PlayerLoopTiming.Update, ct);

            if (CheckIfFound())
            {
                Debug.Log("見つかった状態で退出！");
                OnExitFound?.Invoke();
            }
            else
            {
                Debug.Log("安全に退出");
                OnExitSafe?.Invoke();
            }
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

        public async UniTask CancelHideAsync(Transform actor, CancellationToken ct)
        {
            if (_isBusy) return; // 既に何か実行中なら... いや、強制キャンセルは通すべきか？いったん通す
            // _isBusy = true; // キャンセルは特殊なのでBusyチェックは緩めるか、逆にセットするか

            // 強制中断なのでフラグを折る
            _isBusy = false;

            var el = FindEventLock(actor);
            OnCanceled?.Invoke();

            // 状況に応じて少しだけ戻す/微演出を入れる場合はここに
            // ここでは即解除のみ
            if (el != null) el.EndLock();

            // 色を即戻す
            RestoreColorInstant(actor);

            MessageBroker.Default.Publish(new HideCanceled(actor, this));
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // [NEW] 強制退出（イベント発火なし -> ありに変更）
        public async UniTask ForceExitHideAsync()
        {
            if (_occupant == null) return;
            var actor = _occupant;
            var ct = this.GetCancellationTokenOnDestroy();

            Debug.Log("[HideSpotZone] ForceExitHideAsync Started.");

            // 1. 退出移動（MoveOutAsync）
            await MoveOutAsync(actor, ct);

            // 2. イベント発行
            MessageBroker.Default.Publish(new HideExited(actor, this));

            // 3. ロック解除
            Release(actor);

            Debug.Log("[HideSpotZone] ForceExitHideAsync Completed.");
        }

        private async UniTask MoveOutAsync(Transform actor, CancellationToken ct)
        {
            if (approachPath == null || approachPath.Length == 0) return;

            var el = FindEventLock(actor);
            if (el == null) return;

            el.LookFaceMove();

            // MoveIntoAsync は path[0] -> path[end] -> hidePoint の順
            // なので MoveOutAsync は hidePoint(現在地) -> path[end] -> ... -> path[0] の順で戻る

            for (int i = approachPath.Length - 1; i >= 0; i--)
            {
                var p = approachPath[i];
                if (p == null) continue;

                el.MoveToPoint(p.position, moveSpeed, stopDistance);
                await WaitArriveAsync(actor, p.position, ct);
            }
        }

        public void SetInteractable(bool active)
        {
            // InteractableZone の仕様上、gameObject.SetActive(false) するとZone自体が消えるので、
            // 内部フラグで制御したいが、InteractableZoneには IsInteractable プロパティがない場合が多い。
            // ここでは簡易的に Collider を切る、あるいは _isBusy を使い続ける（ロック用途）
            // _isBusy を true に固定し続けるとハイドもできなくなる。
            // しかし UsageLimiter は "LockSpot" として呼んでいるので、ハイドできないようにしたい。
            // よって _isBusy = !active ではなく、専用の _isLocked フラグを設けるのが適切だが、
            // InteractableZone の TryBuildOptions で弾くためのフラグが必要。
            // ここでは _forceLocked フラグを追加して TryBuildOptions で見るように修正する。
            if (_forceLocked != !active)
            {
                _forceLocked = !active;
                if (active)
                {
                    OnSpotEnabled?.Invoke();
                }
                else
                {
                    OnCanceled?.Invoke();
                }
            }
        }
        private bool _forceLocked;

        [Header("Detection")]
        [Tooltip("退出時、この距離内に敵がいれば強制的に発見扱いにする")]
        [SerializeField] private float forceDetectionRadius = 5.0f;

        [Header("Events (Additional)")]
        [SerializeField]
        private UnityEvent OnSpotEnabled;   // [NEW] 再有効化時

        private bool CheckIfFound()
        {
            var coordinator = ForceFindCoordinator();
            if (coordinator == null) return false;

            // 1. 既にChase状態ならOut
            if (coordinator.IsChasing) return true;

            // 2. 距離チェック (指定範囲内に敵がいるなら強制発見)
            // 隠れポイントがあればそこ基準、なければ自身の位置
            var center = hidePoint != null ? hidePoint.position : transform.position;
            float dist = Vector3.Distance(center, coordinator.transform.position);

            if (dist <= forceDetectionRadius)
            {
                Debug.Log($"[HideSpot] 敵が近すぎるため強制発見！ Dist: {dist:F2} / Radius: {forceDetectionRadius}");
                coordinator.NotifyFound(); // 敵側もChaseに遷移させる
                return true;
            }

            return false;
        }

        public bool IsEnemyNear(Vector3 targetPos)
        {
            var center = hidePoint != null ? hidePoint.position : transform.position;
            float dist = Vector3.Distance(center, targetPos);
            return dist <= forceDetectionRadius;
        }

        private AnoGame.Application.Direction.EnemyBehaviorCoordinator _cachedCoordinator;
        private AnoGame.Application.Direction.EnemyBehaviorCoordinator ForceFindCoordinator()
        {
            if (_cachedCoordinator == null)
                _cachedCoordinator = UnityEngine.Object.FindFirstObjectByType<AnoGame.Application.Direction.EnemyBehaviorCoordinator>();
            return _cachedCoordinator;
        }

        private async UniTask WaitArriveAsync(Transform actor, Vector3 dest, CancellationToken ct)
        {
            // 到達判定を少し緩める（stopDistance + 0.1f）
            var threshold = stopDistance + 0.1f;
            var sq = threshold * threshold;
            while (!ct.IsCancellationRequested)
            {
                var p = actor.position;
                p.y = 0f; dest.y = 0f;
                if ((p - dest).sqrMagnitude <= sq) break;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        // ===== 内部：SpriteRenderer Helper & Fading =====

        private SpriteRenderer RequireSpriteRenderer(Transform actor)
        {
            if (actor == null) return null;

            // 1. 自身
            var sr = actor.GetComponent<SpriteRenderer>();
            if (sr != null) return sr;

            // 2. 自身以下の子供 (Inactive含む)
            sr = actor.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) return sr;

            // 3. 親方向 (EventLockControlがあるルート) からの全体検索
            var el = FindEventLock(actor);
            if (el != null)
            {
                // ルートから検索 (Inactive含む)
                sr = el.transform.GetComponentInChildren<SpriteRenderer>(true);
            }

            return sr;
        }

        private async UniTask FadeColorAsync(Transform actor, Color targetColor, float duration, CancellationToken ct)
        {
            var sr = RequireSpriteRenderer(actor);
            if (sr == null) return;

            // 初回なら元の色を保存
            if (!_originalColor.HasValue)
            {
                // Materialの色を取得 (_Colorプロパティを想定)
                if (sr.material.HasProperty("_Color"))
                {
                    _originalColor = sr.material.color;
                }
                else
                {
                    _originalColor = Color.white;
                }
            }

            var startColor = sr.material.HasProperty("_Color") ? sr.material.color : Color.white;
            float elapsed = 0f;

            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (sr.material.HasProperty("_Color"))
                {
                    sr.material.color = Color.Lerp(startColor, targetColor, t);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            if (!ct.IsCancellationRequested)
            {
                if (sr.material.HasProperty("_Color"))
                {
                    sr.material.color = targetColor;
                }
            }
        }

        private async UniTask RestoreColorAsync(Transform actor, float duration, CancellationToken ct)
        {
            var sr = RequireSpriteRenderer(actor);
            if (sr == null) return;

            var target = _originalColor ?? Color.white;
            var startColor = sr.material.HasProperty("_Color") ? sr.material.color : Color.white;

            float elapsed = 0f;

            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (sr.material.HasProperty("_Color"))
                {
                    sr.material.color = Color.Lerp(startColor, target, t);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            if (!ct.IsCancellationRequested)
            {
                if (sr.material.HasProperty("_Color"))
                {
                    sr.material.color = target;
                }
            }
        }

        private void RestoreColorInstant(Transform actor)
        {
            var sr = RequireSpriteRenderer(actor);
            if (sr == null) return;

            if (_originalColor.HasValue)
            {
                if (sr.material.HasProperty("_Color"))
                {
                    sr.material.color = _originalColor.Value;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            // Detection Radius (Red Wire)
            var center = hidePoint != null ? hidePoint.position : transform.position;

            // 範囲内なら赤（危険）、外なら通常...といってもEditorでは判定できないので常に赤枠で表示
            Handles.color = new Color(1f, 0f, 0f, 0.4f);
            Handles.DrawWireDisc(center, Vector3.up, forceDetectionRadius);
            if (showLabels) Handles.Label(center + Vector3.right * forceDetectionRadius, $"DetectionArea ({forceDetectionRadius}m)");

            // 経路点（approachPath）を「null を除いて」集約
            System.Span<Transform> span = approachPath is { Length: > 0 }
                ? new System.Span<Transform>(approachPath)
                : System.Span<Transform>.Empty;

            // 1) approachPath: 点と線・矢印
            Vector3? prev = null;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            foreach (var t in span)
            {
                if (t == null) continue;

                // 点
                Handles.color = pathColor;
                Handles.SphereHandleCap(0, t.position, Quaternion.identity, pointRadius, EventType.Repaint);

                // ラベル
                if (showLabels)
                {
                    Handles.Label(t.position + Vector3.up * (pointRadius * 2f), $"Path({t.name})");
                }

                // 線と矢印
                if (prev.HasValue)
                {
                    Handles.DrawLine(prev.Value, t.position, 2f);
                    DrawArrow(prev.Value, t.position, arrowSize, pathColor);
                }
                prev = t.position;
            }

            // 2) hidePoint: 最終隠れ位置（緑）
            if (hidePoint != null)
            {
                Handles.color = hideColor;
                Handles.SphereHandleCap(0, hidePoint.position, Quaternion.identity, pointRadius * 1.2f, EventType.Repaint);
                if (showLabels) Handles.Label(hidePoint.position + Vector3.up * (pointRadius * 2f), $"Hide({hidePoint.name})");

                // 有効距離の可視化（validDistance）
                if (validDistance > 0f)
                {
                    Handles.DrawWireDisc(hidePoint.position, Vector3.up, validDistance);
                }

                // 経路がある場合は終端→hidePoint も描画
                if (prev.HasValue)
                {
                    Handles.color = pathColor;
                    Handles.DrawLine(prev.Value, hidePoint.position, 2f);
                    DrawArrow(prev.Value, hidePoint.position, arrowSize, pathColor);
                }
            }
            else
            {
                // hidePoint 未設定時は警告表示
                Handles.color = invalidColor;
                var p = transform.position + Vector3.up * 0.05f;
                Handles.CubeHandleCap(0, p, Quaternion.identity, pointRadius * 1.2f, EventType.Repaint);
                if (showLabels) Handles.Label(p + Vector3.up * (pointRadius * 2f), "HidePoint = null");
            }

            // 3) exitPoint: 退出位置（黄）＆ hidePoint から矢印
            if (exitPoint != null)
            {
                Handles.color = exitColor;
                Handles.SphereHandleCap(0, exitPoint.position, Quaternion.identity, pointRadius * 1.2f, EventType.Repaint);
                if (showLabels) Handles.Label(exitPoint.position + Vector3.up * (pointRadius * 2f), $"Exit({exitPoint.name})");

                if (hidePoint != null)
                {
                    Handles.color = exitColor;
                    Handles.DrawDottedLine(hidePoint.position, exitPoint.position, 3f);
                    DrawArrow(hidePoint.position, exitPoint.position, arrowSize, exitColor);
                }
            }
        }

        private static void DrawArrow(Vector3 from, Vector3 to, float size, Color c)
        {
            var dir = to - from;
            if (dir.sqrMagnitude < 0.0001f) return;
            var rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            var mid = Vector3.Lerp(from, to, 0.8f); // 先端寄りに矢印ヘッド
            Handles.color = c;
            Handles.ArrowHandleCap(0, mid, rot, size, EventType.Repaint);
        }

        private void OnValidate()
        {
            // 既存ロジック：approachPath の最初/最後の非 null を拾って exitPoint/hidePoint を補完
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

            // エディタ上の再描画
            SceneView.RepaintAll();
        }
#endif
    }
}
