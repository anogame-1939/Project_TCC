using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace AnoGame.Application.Player.Interaction
{
    /// <summary>
    /// クールタイム付きのInspectZone
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CooldownInspectZone : InteractableZone
    {
        [Header("Settings")]
        [SerializeField] private string prompt = "調べる（長押し）";
        [SerializeField] private int priority = 500;

        [Tooltip("再使用までのクールタイム（秒）")]
        [SerializeField] private float cooldownDuration = 5.0f;

        [Header("Events")]
        [Tooltip("インタラクト成功時に実行されるイベント")]
        public UnityEvent OnInspected;         // 成功時

        [Tooltip("クールタイム中のインタラクト時に実行されるイベント")]
        public UnityEvent OnCooldownInteract;  // クールタイム中のインタラクト時

        [Tooltip("クールタイムが終了した瞬間に実行されるイベント")]
        public UnityEvent OnCooldownComplete;  // クールタイム終了時

        private bool _isCooldown = false;
        private CancellationTokenSource _cts;

        private void Reset()
        {
            if (TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
            }
        }

        private void OnDisable()
        {
            CancelTimer();
        }

        public override bool TryBuildOptions(Transform actor, List<InteractionOption> buffer)
        {
            if (!InDistance(actor) || !InAngle(actor) || !HasLoS(actor)) return false;

            buffer.Add(new InteractionOption
            {
                Kind = InteractionKind.Inspect,
                Prompt = prompt,
                Priority = priority,
                RequiresHold = true,
                Execute = () =>
                {
                    ExecuteInteraction();
                }
            });
            return true;
        }

        private void ExecuteInteraction()
        {
            if (_isCooldown)
            {
                Debug.Log("[CooldownInspectZone] Interacted during cooldown.");
                OnCooldownInteract?.Invoke();
                return;
            }

            // Success
            Debug.Log("[CooldownInspectZone] Interacted successfully.");
            OnInspected?.Invoke();

            StartCooldown();
        }

        private void StartCooldown()
        {
            CancelTimer();
            _cts = new CancellationTokenSource();
            CooldownRoutine(_cts.Token).Forget();
        }

        private void CancelTimer()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async UniTaskVoid CooldownRoutine(CancellationToken ct)
        {
            _isCooldown = true;

            await UniTask.Delay(System.TimeSpan.FromSeconds(cooldownDuration), cancellationToken: ct);

            _isCooldown = false;
            Debug.Log("[CooldownInspectZone] Cooldown complete.");
            OnCooldownComplete?.Invoke();
            _cts = null;
        }
    }
}
