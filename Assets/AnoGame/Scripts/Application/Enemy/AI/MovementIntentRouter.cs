// ================================
// Router: 優先度で Intent を選ぶ
// ================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.TinyCharacterController.Control;

namespace AnoGame.Application.Enemy.AI
{
    public class MovementIntentRouter : MonoBehaviour
    {
        [Header("実行層")]
        [SerializeField] private MoveNavmeshControl moveNavmeshControl;

        [Header("候補（IIntentProvider 実装を持つ MonoBehaviour を並べる）")]
        [SerializeField] private List<MonoBehaviour> providerBehaviours = new();

        private readonly List<IIntentProvider> _providers = new();

        private void Awake()
        {
            if (moveNavmeshControl == null)
                moveNavmeshControl = GetComponent<MoveNavmeshControl>();

            _providers.Clear();
            foreach (var mb in providerBehaviours)
            {
                if (mb is IIntentProvider p) _providers.Add(p);
            }
        }

        private void FixedUpdate()
        {
            if (moveNavmeshControl == null || _providers.Count == 0) return;

            // 優先度降順で走査
            foreach (var p in _providers.OrderByDescending(pv => pv.Priority))
            {
                if (!p.IsActive()) continue;

                // Debug.Log($"[MovementIntentRouter] Active Provider found: {p.GetType().Name}, Priority: {p.Priority}");

                if (p.TryGetGoal(out var goal) && goal.IsValid)
                {
                    Debug.Log($"[MovementIntentRouter] Selected Provider: {p.GetType().Name}, Priority: {p.Priority}, Goal: {goal.Position}");
                    moveNavmeshControl.SetTargetPosition(goal.Position);
                    // Facing を使って回頭を行いたい場合は MoveNavmeshControl 側で拡張
                    break;
                }
            }
        }

        // 追加でプロバイダを差し込みたい場合用（EventLock の一時登録など）
        public void AddProvider(MonoBehaviour provider)
        {
            if (provider is IIntentProvider ip && !providerBehaviours.Contains(provider))
            {
                providerBehaviours.Add(provider);
                _providers.Add(ip);
            }
        }

        public void RemoveProvider(MonoBehaviour provider)
        {
            if (provider is IIntentProvider ip)
            {
                providerBehaviours.Remove(provider);
                _providers.Remove(ip);
            }
        }
    }
}
