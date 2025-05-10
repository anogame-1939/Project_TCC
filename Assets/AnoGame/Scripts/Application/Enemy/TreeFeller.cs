using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;

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
        private const string FallenTreeTag = "FallenTree";

        void Start()
        {
            StartCoroutine(TreeFellingRoutine());
        }

        IEnumerator TreeFellingRoutine()
        {
            while (true)
            {
                FellTrees();
                yield return new WaitForSeconds(duration);
            }
        }

        [ContextMenu("Fell Trees")]
        public void FellTrees()
        {
            Debug.Log("FellTrees: 処理開始");

            // 必要ならLayerMaskを使ってさらに絞り込んでもOK
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
            foreach (Collider col in colliders)
            {
                // 「倒す対象の木」は元のタグ "Tree" のまま
                // 倒したら FallenTree に変わるので、既に倒れた木はここでスキップされる
                if (!col.CompareTag(TreeTag))
                    continue;

                PreTreeFallen();

                var fallScript = col.GetComponent<CustomTreeRigidbody>();
                if (fallScript == null)
                {
                    fallScript = col.gameObject.AddComponent<CustomTreeRigidbody>();
                }

                fallScript.OnTreeFallen += OnTreeFallenHandler;

                float randomDuration = fallDuration * Random.Range(minDurationMultiplier, maxDurationMultiplier);

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

        // 倒れ終わった木を FallenTree にタグ変更
        private void OnTreeFallenHandler(CustomTreeRigidbody fallenTree)
        {
            // タグを変更して次回以降の検出から外す
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
