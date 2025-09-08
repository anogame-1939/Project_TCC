
using UnityEngine;
using AnoGame.Application.Enemy.AI;

namespace AnoGame.Application.Direction
{
    // ===================================
    // 敵 AI の挙動切替をまとめて管理する Director
    //  - 各 IntentProvider の Activate/Deactivate を呼ぶだけ
    //  - Timeline からの呼び出しも想定
    // ===================================
    [RequireComponent(typeof(MovementIntentRouter))]
    public sealed class EncounterDirector : MonoBehaviour
    {
        [SerializeField] EventLockIntentProvider eventLock;
        [SerializeField] ChaseIntentProvider chase;
        [SerializeField] InvestigateIntentProvider investigate;
        [SerializeField] ReturnToAnchorIntentProvider ret;
        [SerializeField] PatrolSplineIntentProvider patrol;

        [Header("Defaults")]
        [SerializeField] float defaultEncounterTTL = 2.0f;
        [SerializeField] float defaultInvestigateTTL = 3.0f;

    void Awake()
    {
        // Investigate 失敗→帰投開始
        investigate.OnExpired += lastSeen =>
        {
            Debug.Log("見失った！");
            ret.Deactivate();
            patrol.IsActive();
        };

        
    }

        // === Timeline から呼ぶ（SignalReceiver の UnityEvent 1本でOK） ===
        public void BeginEncounter(Transform focus) =>
            eventLock.Activate(focus, defaultEncounterTTL);

        public void EndEncounterToChase()
        {
            eventLock.Deactivate();       // 内部で NavMesh 再開＋ELock解除
            chase.SetActive(true);
        }

        // === ゲーム側（Perception等）から呼ぶ ===
        public void NotifyLost(Vector3 lastSeen) =>
            investigate.Activate(lastSeen, defaultInvestigateTTL);  // 失敗時は Provider 内で Return を起動

        public void NotifyFound()
        {
            investigate.Deactivate();
            ret.Deactivate();
            chase.SetActive(true);
        }

        // 任意：強制中断
        public void AbortAll()
        {
            eventLock.Deactivate();
            investigate.Deactivate();
            ret.Deactivate();
            chase.SetActive(false);
        }
    }
}
