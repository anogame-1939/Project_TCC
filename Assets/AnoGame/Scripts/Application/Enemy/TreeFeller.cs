using System;
using UnityEngine;
using MoreMountains.Feedbacks;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace AnoGame.Application.Enemy
{
    public class TreeFeller : MonoBehaviour
    {
        [Header("実行間隔 (秒)")]
        public float duration = 10f;

        [Header("周囲の木を検出するための半径")]
        public float detectionRadius = 10f;

        [Header("倒すアニメーション設定")]
        [Tooltip("倒れるのにかかる時間(秒)")]
        public float fallDuration = 2f;

        [Header("木が倒れる時間をランダム化する範囲")]
        [Tooltip("fallDurationに乗算する乱数係数の最小値")]
        public float minDurationMultiplier = 0.8f;
        [Tooltip("fallDurationに乗算する乱数係数の最大値")]
        public float maxDurationMultiplier = 2.0f;

        [Tooltip("何度倒すか(90度で横倒しイメージ)")]
        public float fallAngle = 90f;
        [Tooltip("回転軸のオフセット。根元を軸にしたい場合などに調整")]
        public Vector3 pivotOffset = Vector3.zero;
        [Tooltip("0→1の区間で角度をどれだけ倒すかを制御するカーブ")]
        public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public MMF_Player mMF_Player_Skill1;
        public MMF_Player mMF_Player_Skill2;

        private const string TreeTag = "Tree";
        [SerializeField]
        private LayerMask detectionMask;
        private const string FallenTreeTag = "FallenTree";

        // ループ制御用のCTS
        private CancellationTokenSource _cts;

        /// <summary>
        /// 外部から呼び出してスキルループを開始
        /// </summary>
        public void PlaySkillLoop()
        {
            // 既存タスクがあればキャンセル
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            FellingLoopAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// 外部から呼び出してスキルループを停止
        /// </summary>
        public void StopSkillLoop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        /// <summary>
        /// UniTaskでの繰り返し処理
        /// </summary>
        private async UniTaskVoid FellingLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    FellTrees();
                    await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセル例外は無視
            }
        }

        [ContextMenu("Fell Trees")]
        public void FellTrees()
        {
            Debug.Log("FellTrees: 処理開始");

            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, detectionMask);
            foreach (Collider col in colliders)
            {
                if (!col.CompareTag(TreeTag))
                    continue;

                PreTreeFallen();

                var fallScript = col.GetComponent<CustomTreeRigidbody>()
                                 ?? col.gameObject.AddComponent<CustomTreeRigidbody>();

                fallScript.OnTreeFallen += OnTreeFallenHandler;

                float randomDuration = fallDuration *
                                       UnityEngine.Random.Range(minDurationMultiplier, maxDurationMultiplier);

                fallScript.Fall(
                    fellerPosition: transform.position,
                    fallDuration:   randomDuration,
                    fallAngle:      fallAngle,
                    pivotOffset:    pivotOffset,
                    fallCurve:      fallCurve
                );
            }
        }

        private void PreTreeFallen()
        {
            if (!mMF_Player_Skill1.IsPlaying)
            {
                mMF_Player_Skill1.PlayFeedbacks();
            }
        }

        private void OnTreeFallenHandler(CustomTreeRigidbody fallenTree)
        {
            fallenTree.gameObject.layer = 0;
            fallenTree.gameObject.tag = FallenTreeTag;
            fallenTree.OnTreeFallen -= OnTreeFallenHandler;

            Debug.Log("木が倒れ終わりました！ カメラシェイクを実行します。");
            if (!mMF_Player_Skill2.IsPlaying)
            {
                mMF_Player_Skill2.PlayFeedbacks();
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
