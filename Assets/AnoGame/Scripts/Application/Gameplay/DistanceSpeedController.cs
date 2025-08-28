using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.TinyCharacterController.Control;

namespace AnoGame.Application.Gameplay
{

    public class DistanceSpeedController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private MoveNavmeshControl move;      // 同じ敵ルートにある MoveNavmeshControl
        [SerializeField] private string playerTag = "Player";  // プレイヤータグ

        [Header("速度設定")]
        [SerializeField] private float boostedSpeed = 7f;      // 加速時の速度
        [SerializeField] private float revertSmoothing = 0f;   // 0=即時、>0で補間復帰（m/s^2 相当）

        [Header("距離しきい値（m）")]
        [Tooltip("この距離以上に離れたらブースト開始")]
        [SerializeField] private float farBoostDistance = 12f;
        [Tooltip("この距離以下まで近づいたら元速に復帰")]
        [SerializeField] private float nearReturnDistance = 6f;
        [Header("ターゲット検出")]
        [SerializeField] private bool autoRefreshTargets = true;
        [SerializeField, Min(0.1f)] private float refreshInterval = 1.0f;

        private readonly List<Transform> targets = new();
        private float baseSpeed;
        private bool isBoosted;
        private Coroutine refreshLoop;

        private float farBoostSqr, nearReturnSqr;

        void Reset()
        {
            move = GetComponent<MoveNavmeshControl>();
        }

        void Awake()
        {
            if (move == null) move = GetComponent<MoveNavmeshControl>();
            baseSpeed = move.Speed;
            RecalcSqr();
        }

        private void RecalcSqr()
        {
            farBoostSqr = farBoostDistance * farBoostDistance;
            nearReturnSqr = nearReturnDistance * nearReturnDistance;
        }

        void OnEnable()
        {
            RefreshTargets();
            if (autoRefreshTargets)
                refreshLoop = StartCoroutine(RefreshTargetsLoop());
        }

        void OnDisable()
        {
            if (refreshLoop != null) StopCoroutine(refreshLoop);
            // 念のため速度を元に戻す
            move.Speed = baseSpeed;
            isBoosted = false;
        }

        void Update()
        {
            if (targets.Count == 0) { RevertToBaseImmediate(); return; }

            // 最も近いプレイヤーまでの水平距離
            float minSqr = float.PositiveInfinity;
            Vector3 p = transform.position;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var t = targets[i];
                if (t == null) { targets.RemoveAt(i); continue; }
                Vector3 a = p; a.y = 0f; Vector3 b = t.position; b.y = 0f;
                float sqr = (a - b).sqrMagnitude;
                if (sqr < minSqr) minSqr = sqr;
            }
            if (minSqr == float.PositiveInfinity) { RevertToBaseImmediate(); return; }

            // 離れたらブースト / 近づいたら復帰（ヒステリシス）
            if (isBoosted)
            {
                // ブースト中は、十分近づくまで維持
                if (minSqr <= nearReturnSqr)
                    RevertToBaseSmooth();
                else
                    move.Speed = boostedSpeed; // 維持
            }
            else
            {
                // 非ブースト時、十分離れたらブースト開始
                if (minSqr >= farBoostSqr)
                {
                    isBoosted = true;
                    move.Speed = boostedSpeed; // 必要ならここを補間に変える
                }
                else
                {
                    RevertToBaseSmooth(); // 中間帯は元速キープ
                }
            }
        }

        private void RevertToBaseImmediate()
        {
            if (isBoosted || !Mathf.Approximately(move.Speed, baseSpeed))
            {
                isBoosted = false;
                move.Speed = baseSpeed;
            }
        }

        private void RevertToBaseSmooth()
        {
            if (revertSmoothing <= 0f)
            {
                RevertToBaseImmediate();
            }
            else
            {
                // 単純な時間あたりの減速（補間）。必要なら MoveTowards で十分。
                move.Speed = Mathf.MoveTowards(move.Speed, baseSpeed, revertSmoothing * Time.deltaTime);
                if (Mathf.Approximately(move.Speed, baseSpeed)) isBoosted = false;
            }
        }

        private IEnumerator RefreshTargetsLoop()
        {
            var wfs = new WaitForSeconds(refreshInterval);
            while (true)
            {
                RefreshTargets();
                yield return wfs;
            }
        }

        [ContextMenu("Refresh Targets (Manual)")]
        public void RefreshTargets()
        {
            targets.Clear();
            var found = GameObject.FindGameObjectsWithTag(playerTag);
            foreach (var go in found)
            {
                // 自分自身は除外
                if (go == gameObject) continue;
                targets.Add(go.transform);
            }
        }

    }
}