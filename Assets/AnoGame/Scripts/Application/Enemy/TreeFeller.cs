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
        

        void Start()
        {
            // duration 秒ごとに木を倒す処理をコルーチンで実行
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

        /// <summary>
        /// 周囲の木を検出して倒す処理
        /// インスペクターから右クリックメニューでも呼び出せる
        /// </summary>
        [ContextMenu("Fell Trees")]
        public void FellTrees()
        {
            Debug.Log("FellTrees: 処理開始");

            // 自身の周囲(detectionRadius内)のColliderを取得
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
            foreach (Collider col in colliders)
            {
                // "Tree" タグのオブジェクトを対象にする
                if (col.CompareTag("Tree"))
                {
                    // 木が倒れる前の処理を実行
                    PreTreeFallen();

                    // まだ CustomTreeRigidbody が付いていなければアタッチ
                    CustomTreeRigidbody fallScript = col.GetComponent<CustomTreeRigidbody>();
                    if (fallScript == null)
                    {
                        fallScript = col.gameObject.AddComponent<CustomTreeRigidbody>();
                    }

                    // イベント購読
                    fallScript.OnTreeFallen += OnTreeFallenHandler;

                    float randomDuration = fallDuration * Random.Range(minDurationMultiplier, maxDurationMultiplier);

                    // 「倒す」処理を実行 (本スクリプト側のパラメータを渡す)
                    fallScript.Fall(
                        fellerPosition : transform.position,
                        fallDuration   : randomDuration,
                        fallAngle      : fallAngle,
                        pivotOffset    : pivotOffset,
                        fallCurve      : fallCurve
                    );
                }
            }
        }

        private void PreTreeFallen()
        {
            if (!mMF_Player_Skill1.IsPlaying)
            {
                mMF_Player_Skill1.PlayFeedbacks();
            }
        }

        // 木が倒れ終わった時に呼ばれるハンドラ
        private void OnTreeFallenHandler(CustomTreeRigidbody fallenTree)
        {
            // 必要に応じて購読解除
            fallenTree.OnTreeFallen -= OnTreeFallenHandler;

            // Feelでカメラシェイクなどを実行
            Debug.Log("木が倒れ終わりました！ カメラシェイクを実行します。");
            // ここでFeelのFeedbackを呼ぶ etc...

            if (!mMF_Player_Skill2.IsPlaying)
            {
                mMF_Player_Skill2.PlayFeedbacks();
            }
        }

        // デバッグ用に detectionRadius を描画する
        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
