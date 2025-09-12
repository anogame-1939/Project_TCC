
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
            // まずChaseは止めておく（安全）
            chase.SetActive(false);
            // 帰投を開始（最近傍のKnotへ）
            ret.ActivateToNearest();   // ★ ここが Deactivate ではなく Activate
            // 巡回は常時ONでも良いが、明示的にONにしておくと安心
            patrol.SetActive(true);
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
        public void NotifyLost(Vector3 lastSeen)
        {
            chase.SetActive(false);                 // ★最重要：ChaseをOFF
            ret.Deactivate();
            investigate.Activate(lastSeen, defaultInvestigateTTL);
        }


        public void NotifyFound()
        {
            investigate.Deactivate();
            ret.Deactivate();
            chase.SetActive(true);                  // 再露見＝追跡へ
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
