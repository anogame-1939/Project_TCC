using AnoGame.Application.Damage;
using UnityEngine;

namespace AnoGame.Application.Gameplay
{
    /// <summary>
    /// 指定したオブジェクトが当たり判定（Trigger）から出た瞬間に
    /// Debug.Log を出力するシンプルなスクリプト。
    /// このスクリプトは “判定エリア” 側（コライダーを持つ GameObject）に
    /// アタッチしてください。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class OutOfBoundsGameOverHandler : MonoBehaviour
    {
        [Header("判定対象を絞り込みたい場合は Tag を指定")]
        [Tooltip("空欄ならすべてのオブジェクトが対象")]
        [SerializeField] private string targetTag = "Player";

        // Collider を Trigger にしておくこと
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerExit(Collider other)
        {
            // Tag 指定がない場合は無条件でログを出す
            // Tag が指定されているなら一致するものだけログを出す
            if (string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag))
            {
                Debug.Log($"{other.name} が当たり判定の外に出ました ({Time.time:F2} 秒)");
                var damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(999);
                }
            }
        }
    }
}