// ===================================
// Provider: プレイヤー追跡（最小）
// ===================================
using UnityEngine;

namespace AnoGame.Application.Enemy.AI
{
    public class ChaseIntentProvider : MonoBehaviour, IIntentProvider
    {
        [SerializeField] private int priority = 70;
        [SerializeField] private bool active = true;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Transform player; // 未指定なら tag 検索

        public int Priority => priority;

        public bool IsActive()
        {
            if (!active) return false;
            if (player == null)
            {
                var go = GameObject.FindWithTag(playerTag);
                if (go != null) player = go.transform;
            }
            return player != null;
        }

        public bool TryGetGoal(out MoveGoal goal)
        {
            if (player == null) { goal = default; return false; }
            goal = MoveGoal.FromPosition(player.position);
            return true;
        }

        // ランタイム切替用（任意）
        public void SetActive(bool v) => active = v;
    }
}
